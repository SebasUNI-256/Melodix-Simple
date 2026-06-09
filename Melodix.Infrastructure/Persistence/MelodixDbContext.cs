using Melodix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Melodix.Infrastructure.Persistence;

public sealed class MelodixDbContext : DbContext
{
    public MelodixDbContext(DbContextOptions<MelodixDbContext> options)
        : base(options)
    {
    }

    public DbSet<MusicFolder> MusicFolders => Set<MusicFolder>();

    public DbSet<MediaTrack> MediaTracks => Set<MediaTrack>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MusicFolder>(builder =>
        {
            builder.ToTable("MusicFolders");
            builder.HasKey(folder => folder.Id);
            builder.Property(folder => folder.Path).IsRequired();
            builder.Property(folder => folder.CreatedAt).IsRequired();
            builder.HasIndex(folder => folder.Path).IsUnique();
            builder.HasMany(folder => folder.Tracks)
                .WithOne(track => track.Folder)
                .HasForeignKey(track => track.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaTrack>(builder =>
        {
            builder.ToTable("MediaTracks");
            builder.HasKey(track => track.Id);
            builder.Property(track => track.FilePath).IsRequired();
            builder.Property(track => track.FileName).IsRequired();
            builder.Property(track => track.Extension).IsRequired();
            builder.Property(track => track.DiscoveredAt).IsRequired();
            builder.HasIndex(track => new { track.FolderId, track.FilePath }).IsUnique();
        });
    }
}
