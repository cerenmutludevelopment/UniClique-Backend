using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace UniCliqueBackend.Persistence.Contexts
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql")
                ?? "Host=localhost;Port=5432;Database=uniclique_db;Username=uniclique_user;Password=uniclique_pass";
            var t = connectionString.Trim();
            var l = t.ToLowerInvariant();
            if (l.StartsWith("postgres://") || l.StartsWith("postgresql://"))
            {
                var u = new Uri(t);
                var ui = u.UserInfo.Split(':', 2);
                var un = ui.Length > 0 ? ui[0] : "";
                var pw = ui.Length > 1 ? ui[1] : "";
                var h = u.Host;
                var pt = u.Port > 0 ? u.Port : 5432;
                var db = u.AbsolutePath.TrimStart('/');
                var kv = $"Host={h};Port={pt};Database={db};Username={un};Password={pw}";
                var q = u.Query;
                if (!string.IsNullOrEmpty(q))
                {
                    var parts = q.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var kvp = p.Split('=', 2);
                        var k = kvp[0].ToLowerInvariant();
                        var v = kvp.Length > 1 ? kvp[1] : "";
                        if (k == "sslmode" && !string.IsNullOrEmpty(v))
                        {
                            var vv = char.ToUpperInvariant(v[0]) + v.Substring(1);
                            kv += $";SslMode={vv}";
                        }
                        if (k == "trust_server_certificate" && !string.IsNullOrEmpty(v))
                        {
                            kv += $";Trust Server Certificate={v}";
                        }
                    }
                }
                connectionString = kv;
            }

            optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly("UniCliqueBackend.Persistence"));
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
