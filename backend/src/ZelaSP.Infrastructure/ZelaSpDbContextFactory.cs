using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ZelaSP.Infrastructure;

// Geração offline de artefatos. A conexão real só é necessária para aplicar migrações.
public sealed class ZelaSpDbContextFactory : IDesignTimeDbContextFactory<ZelaSpDbContext>
{
    public ZelaSpDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__ZelaSP")
            ?? "Server=.\\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true";
        return new(new DbContextOptionsBuilder<ZelaSpDbContext>().UseSqlServer(connection).Options);
    }
}
