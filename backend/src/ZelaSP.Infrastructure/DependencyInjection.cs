using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZelaSP.Application;
namespace ZelaSP.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ZelaSpDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IOcorrenciasStore, OcorrenciasStore>();
        services.AddScoped<IImagensStore, ImagensStore>();
        return services;
    }
}
