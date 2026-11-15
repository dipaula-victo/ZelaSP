using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace ZelaSP.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ZelaSpDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
}
