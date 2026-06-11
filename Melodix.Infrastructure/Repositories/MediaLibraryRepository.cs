using Melodix.Application.Abstractions;
using Melodix.Domain.Entities;
using Melodix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Melodix.Infrastructure.Repositories;

public sealed class MediaLibraryRepository : IMediaLibraryRepository
{
    private readonly IDbContextFactory<MelodixDbContext> _dbContextFactory;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;

    // Guarda la fabrica de contexto para el acceso a datos.
    public MediaLibraryRepository(IDbContextFactory<MelodixDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    // Obtiene la carpeta activa almacenada.
    public async Task<MusicFolder?> GetActiveFolderAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.MusicFolders
            .AsNoTracking()
            .FirstOrDefaultAsync(folder => folder.IsActive, cancellationToken);
    }

    // Marca una carpeta como activa y desactiva las demas.
    public async Task<MusicFolder> UpsertActiveFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedPath = Path.GetFullPath(path);

        var activeFolders = await dbContext.MusicFolders
            .Where(folder => folder.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var folder in activeFolders)
        {
            folder.IsActive = false;
        }

        var folderEntity = await dbContext.MusicFolders
            .FirstOrDefaultAsync(folder => folder.Path == normalizedPath, cancellationToken);

        if (folderEntity is null)
        {
            folderEntity = new MusicFolder
            {
                Path = normalizedPath,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await dbContext.MusicFolders.AddAsync(folderEntity, cancellationToken);
        }
        else
        {
            folderEntity.IsActive = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return folderEntity;
    }

    // Reemplaza las pistas de una carpeta por las nuevas detectadas.
    public async Task ReplaceTracksForFolderAsync(
        Guid folderId,
        IReadOnlyCollection<MediaTrack> tracks,
        DateTimeOffset scannedAt,
        CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var folderEntity = await dbContext.MusicFolders
            .Include(folder => folder.Tracks)
            .FirstAsync(folder => folder.Id == folderId, cancellationToken);

        dbContext.MediaTracks.RemoveRange(folderEntity.Tracks);

        foreach (var track in tracks)
        {
            track.Id = track.Id == Guid.Empty ? Guid.NewGuid() : track.Id;
            track.FolderId = folderId;
        }

        await dbContext.MediaTracks.AddRangeAsync(tracks, cancellationToken);
        folderEntity.LastScanAt = scannedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Lista las pistas guardadas para una carpeta concreta.
    public async Task<IReadOnlyList<MediaTrack>> GetTracksForFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.MediaTracks
            .AsNoTracking()
            .Where(track => track.FolderId == folderId)
            .OrderBy(track => track.FileName)
            .ToListAsync(cancellationToken);
    }

    // Crea la base de datos solo una vez.
    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
