using Microsoft.Data.SqlClient;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Başqa kompüterdə SQL Server-in adı fərqli olur (.\SQLEXPRESS, LocalDB və s.).
    // appsettings-dəki server işləmirsə, tanınmış ünvanlar yoxlanılır və ilk işləyəni seçilir.
    public static class ConnectionStringResolver
    {
        private const string DefaultDatabase = "CaspianBankDataBase";

        public static string Resolve(IConfiguration configuration, ILogger logger)
        {
            var configured = configuration.GetConnectionString("DefaultConnection");
            var baseBuilder = string.IsNullOrWhiteSpace(configured)
                ? new SqlConnectionStringBuilder
                {
                    InitialCatalog = DefaultDatabase,
                    IntegratedSecurity = true,
                    TrustServerCertificate = true
                }
                : new SqlConnectionStringBuilder(configured);

            var servers = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseBuilder.DataSource)) servers.Add(baseBuilder.DataSource);
            foreach (var candidate in Candidates())
                if (!servers.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                    servers.Add(candidate);

            // 1-ci keçid: verilənlər bazasının özü (import olunmuş) mövcud olan server
            foreach (var server in servers)
            {
                var builder = With(baseBuilder, server, baseBuilder.InitialCatalog);
                if (CanConnect(builder))
                {
                    logger.LogInformation("SQL Server tapıldı: {Server} (baza: {Database})", server, baseBuilder.InitialCatalog);
                    return builder.ConnectionString;
                }
            }

            // 2-ci keçid: server işləyir, amma baza hələ yoxdur: migration onu yaradacaq
            foreach (var server in servers)
            {
                if (CanConnect(With(baseBuilder, server, "master")))
                {
                    logger.LogWarning("SQL Server tapıldı: {Server}, amma '{Database}' bazası yoxdur. Migration onu yaradacaq.", server, baseBuilder.InitialCatalog);
                    return With(baseBuilder, server, baseBuilder.InitialCatalog).ConnectionString;
                }
            }

            // Xəta atmırıq: bazaya ehtiyacı olmayan əmrlər (məs. dotnet ef migrations add) işləməyə davam etsin.
            // Real bağlantı xətası bazaya ilk müraciətdə (başlanğıcdakı migration) aydın mesajla çıxacaq.
            logger.LogError(
                "SQL Server tapılmadı. Yoxlanılan ünvanlar: {Servers}. SQL Server (Express/LocalDB) quraşdırılıb işləyirsə, " +
                "appsettings.json-dakı ConnectionStrings:DefaultConnection-u öz serverinizin adı ilə dəyişin.",
                string.Join(", ", servers));
            return baseBuilder.ConnectionString;
        }

        private static IEnumerable<string> Candidates()
        {
            yield return @".\SQLEXPRESS";
            yield return @"(localdb)\MSSQLLocalDB";
            yield return ".";
            yield return "localhost";
            yield return @"localhost\SQLEXPRESS";
            yield return $@"{Environment.MachineName}\SQLEXPRESS";
            yield return Environment.MachineName;
        }

        private static SqlConnectionStringBuilder With(SqlConnectionStringBuilder source, string server, string database)
        {
            var builder = new SqlConnectionStringBuilder(source.ConnectionString)
            {
                DataSource = server,
                InitialCatalog = database,
                ConnectTimeout = 5
            };
            // Başqa kompüterdə SQL Server sertifikatı etibarlı olmaya bilər (lokal sınaq)
            builder.TrustServerCertificate = true;
            return builder;
        }

        private static bool CanConnect(SqlConnectionStringBuilder builder)
        {
            try
            {
                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
