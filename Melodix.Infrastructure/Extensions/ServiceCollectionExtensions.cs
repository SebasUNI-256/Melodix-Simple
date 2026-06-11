using Melodix.Application.Abstractions;
using Melodix.Infrastructure.Persistence;
using Melodix.Infrastructure.Repositories;
using Melodix.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Melodix.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    // Registra la infraestructura y la base SQLite.
    public static IServiceCollection AddMelodixInfrastructure(this IServiceCollection services, string databasePath)
    {
        services.AddDbContextFactory<MelodixDbContext>(options =>
        {
            options.UseSqlite($"Data Source={databasePath}");
        });

        services.AddSingleton<IMediaLibraryRepository, MediaLibraryRepository>();
        services.AddSingleton<IMediaScanner, FileSystemMediaScanner>();

        return services;
    }
}
