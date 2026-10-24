using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository;
using Repository.Data;
using Service;
using Service.Helpers.DTOs.Notifications;
using Service.Helpers.Settings;
using Service.Services.Interfaces;

namespace CaspianBank.Tests.Infrastructure
{
    // Testlər üçün real SQL Server bazası: pul əməliyyatları tranzaksiya, RowVersion və EF.Functions.Like istifadə edir,
    // yaddaş bazası bunları eyni cür işlətmir. Hər test işi üçün ayrıca baza yaradılır və sonda silinir
    public sealed class TestApp : IAsyncDisposable
    {
        private const string DatabasePrefix = "CaspianBankTests_";
        private static readonly object Gate = new object();
        private static Task<TestApp>? _shared;

        public ServiceProvider Provider { get; }
        public string DatabaseName { get; }
        public CapturingEmailSender Emails { get; }
        public CapturingNotificationPublisher Published { get; }
        public string WebRoot { get; }

        private readonly string _connectionString;

        private TestApp(string databaseName, string connectionString, string webRoot, CapturingEmailSender emails, CapturingNotificationPublisher published, ServiceProvider provider)
        {
            DatabaseName = databaseName;
            _connectionString = connectionString;
            WebRoot = webRoot;
            Emails = emails;
            Published = published;
            Provider = provider;
        }

        // Bütün testlər üçün ortaq baza. Testlər ÖZ məlumatını yaradır (təkrarsız email və s.), qlobal cəmlərə (xəzinə, dashboard) baxan testlər ayrıca baza istifadə edir
        public static Task<TestApp> SharedAsync()
        {
            lock (Gate)
            {
                if (_shared is null)
                {
                    _shared = CreateAsync();
                    AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                    {
                        try
                        {
                            _shared.GetAwaiter().GetResult().DisposeAsync().AsTask().GetAwaiter().GetResult();
                        }
                        catch
                        {
                            // proses bağlanır, silinməsə də zərəri yoxdur (növbəti işdə təmizlənir)
                        }
                    };
                }
                return _shared;
            }
        }

        public static async Task<TestApp> CreateAsync()
        {
            var server = LocateServer();
            var databaseName = DatabasePrefix + Guid.NewGuid().ToString("N");
            var builder = new SqlConnectionStringBuilder(server) { InitialCatalog = databaseName };
            var connectionString = builder.ConnectionString;

            var webRoot = Path.Combine(Path.GetTempPath(), databaseName);
            Directory.CreateDirectory(webRoot);

            var emails = new CapturingEmailSender();
            var published = new CapturingNotificationPublisher();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            services.AddSingleton<INotificationPublisher>(published);
            services.AddSingleton<IWebHostEnvironment>(new FakeEnvironment(webRoot));

            services.AddRepositoryLayer();
            services.AddServiceLayer();
            services.AddSingleton<IEmailSender>(emails);      // AddServiceLayer öz göndərəni qeydiyyatdan keçirir: bizimki ondan SONRA olmalıdır

            // Program.cs-dəki Identity qaydaları ilə eyni
            services.AddIdentityCore<AppUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 6;
                    options.Password.RequiredUniqueChars = 1;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<JwtSettings>(o =>
            {
                o.Key = "test-key-test-key-test-key-test-key-1234567890";
                o.Issuer = "CaspianBank.Test";
                o.Audience = "CaspianBank.Web";
                o.ExpiryMinutes = 60;
            });
            services.Configure<SmtpSettings>(o =>
            {
                o.ShowCodesWhenNotConfigured = true;       // kod cavabda qayıdır: testlər emaili oxumadan da davam edə bilər
            });
            services.Configure<FrontendSettings>(o => o.BaseUrl = "https://frontend.test");
            services.Configure<HomeStatsSettings>(_ => { });
            services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromMinutes(30));

            var provider = services.BuildServiceProvider();
            var app = new TestApp(databaseName, connectionString, webRoot, emails, published, provider);

            await app.CleanOldDatabasesAsync(server);

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();

                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                await RoleSeeder.SeedAsync(roles);
            }

            return app;
        }

        public IServiceScope NewScope()
        {
            return Provider.CreateScope();
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            try
            {
                SqlConnection.ClearAllPools();
                using var connection = new SqlConnection(new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master" }.ConnectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = $"IF DB_ID(N'{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END";
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // baza silinmədisə növbəti işdə CleanOldDatabasesAsync silir
            }

            try
            {
                Directory.Delete(WebRoot, true);
            }
            catch
            {
                // müvəqqəti qovluqdur
            }
        }

        // Əvvəlki işdən qalmış test bazalarını silir (proses yarımçıq dayandırılıbsa)
        private async Task CleanOldDatabasesAsync(string server)
        {
            try
            {
                using var connection = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = "master" }.ConnectionString);
                await connection.OpenAsync();

                var old = new List<string>();
                using (var list = connection.CreateCommand())
                {
                    list.CommandText = "SELECT name FROM sys.databases WHERE name LIKE 'CaspianBankTests[_]%' AND create_date < DATEADD(hour, -2, GETDATE())";
                    using var reader = await list.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        old.Add(reader.GetString(0));
                    }
                }

                foreach (var name in old)
                {
                    using var drop = connection.CreateCommand();
                    drop.CommandText = $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];";
                    await drop.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // təmizləmə vacib deyil
            }
        }

        // SQL Server: mühit dəyişəni CASPIAN_TEST_SQL (tam connection string), yoxsa tipik yerli serverlər
        private static string LocateServer()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("CASPIAN_TEST_SQL");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            var candidates = new[] { @".\SQLEXPRESS", @"(localdb)\MSSQLLocalDB", "localhost", @"DESKTOP-B24N2SA\SQLEXPRESS" };
            foreach (var candidate in candidates)
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = candidate,
                    InitialCatalog = "master",
                    IntegratedSecurity = true,
                    TrustServerCertificate = true,
                    ConnectTimeout = 3
                };
                try
                {
                    using var connection = new SqlConnection(builder.ConnectionString);
                    connection.Open();
                    return builder.ConnectionString;
                }
                catch
                {
                    // növbəti namizəd
                }
            }

            throw new InvalidOperationException("SQL Server tapılmadı. CASPIAN_TEST_SQL mühit dəyişəninə connection string yazın.");
        }
    }

    // Göndərilən emaillər burada yığılır
    public sealed class CapturingEmailSender : IEmailSender
    {
        private readonly List<SentEmail> _sent = new List<SentEmail>();

        public IReadOnlyList<SentEmail> Sent
        {
            get
            {
                lock (_sent)
                {
                    return _sent.ToList();
                }
            }
        }

        public bool ShouldFail { get; set; }

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                throw new InvalidOperationException("Email server is down.");
            }
            lock (_sent)
            {
                _sent.Add(new SentEmail(to, subject, htmlBody));
            }
            return Task.CompletedTask;
        }

        public SentEmail? LastTo(string email)
        {
            return Sent.LastOrDefault(e => string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed record SentEmail(string To, string Subject, string Body)
    {
        // Mətndəki 6 rəqəmli kod (OTP)
        public string? Code
        {
            get
            {
                var match = System.Text.RegularExpressions.Regex.Match(Body, @"<h2[^>]*>(\d{6})</h2>");
                return match.Success ? match.Groups[1].Value : null;
            }
        }

        // Mətndəki ilk link
        public string? Link
        {
            get
            {
                var match = System.Text.RegularExpressions.Regex.Match(Body, "href=\"([^\"]+)\"");
                return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
            }
        }
    }

    public sealed class CapturingNotificationPublisher : INotificationPublisher
    {
        private readonly List<(string UserId, NotificationDto Notification, int Unread)> _items = new List<(string, NotificationDto, int)>();

        public IReadOnlyList<(string UserId, NotificationDto Notification, int Unread)> Items
        {
            get
            {
                lock (_items)
                {
                    return _items.ToList();
                }
            }
        }

        public Task PublishAsync(string userId, NotificationDto notification, int unreadCount)
        {
            lock (_items)
            {
                _items.Add((userId, notification, unreadCount));
            }
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeEnvironment : IWebHostEnvironment
    {
        public FakeEnvironment(string webRoot)
        {
            WebRootPath = webRoot;
            ContentRootPath = webRoot;
            WebRootFileProvider = new NullFileProvider();
            ContentRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; } = "CaspianBank.Tests";
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
