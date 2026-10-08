using CaspianBank_API_FinalProject.Helpers;
using CaspianBank_API_FinalProject.Middlewares;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repository;
using Repository.Data;
using Service;
using Service.Services.Interfaces;
using Service.Helpers.Settings;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Yalnız bu kompüterdəki gizli məlumatlar (məs. işçi hesablarının parolları). Fayl .gitignore-dadır, yoxdursa heç nə olmur
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger-də "Authorize" düyməsi: login-dən alınan tokeni yapışdırıb qorunan endpoint-ləri sınamaq üçün
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Login-dən qayıdan tokeni yazın (Bearer sözü olmadan)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// SQL Server avtomatik tapılır (başqa kompüterdə server adı fərqli ola bilər)
var connectionString = ConnectionStringResolver.Resolve(
    builder.Configuration,
    LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("Database"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddServiceLayer();
builder.Services.AddRepositoryLayer();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true; // sənin layihəndə email unikal olmalıdır (login email ilədir)
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT: açar user-secrets-dən (Jwt:Key), qalan ayarlar appsettings.json-dan gəlir
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));

// Şifrə bərpası linki 30 dəqiqə etibarlıdır (Identity defoltu 1 gündür)
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(30));
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key tapılmadı və ya çox qısadır (min 32 simvol). dotnet user-secrets ilə təyin edin.");
}

// AddIdentity cookie-ni defolt edir; API üçün defolt sxem JWT olmalıdır (yoxsa 401 əvəzinə login-ə yönləndirir)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});


// MVC API-yə server tərəfdən müraciət edir, ona görə real istifadəçi IP-si X-Forwarded-For başlığı ilə gəlir.
// Defolt olaraq yalnız lokal (loopback) proksiyə etibar olunur; production-da proksi ünvanı əlavə edilməlidir.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
});

// Rate limiting: eyni IP-dən dəqiqədə məhdud sayda cəhd (check-email, login və s.)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Köçürmə: alıcı kartını tapmaq cəhdlərini (nömrə təxmini) və sürətli təkrar sorğuları məhdudlaşdırır
    options.AddPolicy("transfer", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Baza yoxdursa yaradılır, çatışmayan migration-lar tətbiq olunur (import olunmuş baza artıq tamdırsa heç nə dəyişmir)
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(roleManager);

    // İşçi hesabları (appsettings.json -> SeedAdmins): hesab artıq varsa toxunulmur
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var seedAdmins = app.Configuration.GetSection("SeedAdmins").Get<List<SeedAdminSettings>>() ?? new List<SeedAdminSettings>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
    await AdminSeeder.SeedAsync(userManager, seedAdmins, seedLogger);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ən başda dayansın ki, sonrakı hər şeydə atılan xəta tutulsun
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();


