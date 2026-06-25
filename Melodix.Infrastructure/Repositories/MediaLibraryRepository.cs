using Melodix.Application.Abstractions;
using Melodix.Domain.Entities;
using Melodix.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Melodix.Infrastructure.Repositories;

// Persiste carpetas activas, orden de pistas y rutas de letra en SQLite.
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

        var existingTracksByPath = folderEntity.Tracks
            .ToDictionary(track => track.FilePath, StringComparer.OrdinalIgnoreCase);
        dbContext.MediaTracks.RemoveRange(folderEntity.Tracks);

        var nextSortOrder = 0;
        foreach (var track in tracks)
        {
            if (existingTracksByPath.TryGetValue(track.FilePath, out var existingTrack))
            {
                track.Id = existingTrack.Id;
                track.SortOrder = existingTrack.SortOrder;
                track.LyricsFilePath = existingTrack.LyricsFilePath;
            }
            else
            {
                track.Id = track.Id == Guid.Empty ? Guid.NewGuid() : track.Id;
                track.SortOrder = nextSortOrder;
            }

            track.FolderId = folderId;
            nextSortOrder = Math.Max(nextSortOrder, track.SortOrder + 1);
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
            .OrderBy(track => track.SortOrder)
            .ThenBy(track => track.FileName)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateTrackOrderAsync(
        Guid folderId,
        IReadOnlyList<(Guid TrackId, int SortOrder)> trackOrders,
        CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        if (trackOrders.Count == 0)
        {
            return;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tracks = await dbContext.MediaTracks
            .Where(track => track.FolderId == folderId)
            .ToDictionaryAsync(track => track.Id, cancellationToken);

        foreach (var (trackId, sortOrder) in trackOrders)
        {
            if (tracks.TryGetValue(trackId, out var track))
            {
                track.SortOrder = sortOrder;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTrackLyricsFilePathAsync(
        Guid trackId,
        string? lyricsFilePath,
        CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var track = await dbContext.MediaTracks.FirstOrDefaultAsync(item => item.Id == trackId, cancellationToken);
        if (track is null)
        {
            return;
        }

        track.LyricsFilePath = string.IsNullOrWhiteSpace(lyricsFilePath) ? null : lyricsFilePath;
        await dbContext.SaveChangesAsync(cancellationToken);
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
            await EnsureMediaTrackColumnsAsync(dbContext, cancellationToken);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    //Modifica atraves del codigo de la bd para no hacer Migraciones manualmente
    private static async Task EnsureMediaTrackColumnsAsync(MelodixDbContext dbContext, CancellationToken cancellationToken)
    {
        await using var connection = (SqliteConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info('MediaTracks');";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existingColumns.Add(reader.GetString(1));
            }
        }

        if (!existingColumns.Contains("SortOrder"))
        {
            await using var addSortOrderCommand = connection.CreateCommand();
            addSortOrderCommand.CommandText = "ALTER TABLE MediaTracks ADD COLUMN SortOrder INTEGER NOT NULL DEFAULT 0;";
            await addSortOrderCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!existingColumns.Contains("LyricsFilePath"))
        {
            await using var addLyricsPathCommand = connection.CreateCommand();
            addLyricsPathCommand.CommandText = "ALTER TABLE MediaTracks ADD COLUMN LyricsFilePath TEXT NULL;";
            await addLyricsPathCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
