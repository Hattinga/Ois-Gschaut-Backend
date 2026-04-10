using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Models;

namespace OisGschaut.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserList> Lists => Set<UserList>();
    public DbSet<ListCollaborator> ListCollaborators => Set<ListCollaborator>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<UserSeasonWatched> UserSeasonWatched => Set<UserSeasonWatched>();
    public DbSet<MediaType> MediaTypes => Set<MediaType>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<RatingSource> RatingSources => Set<RatingSource>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<CollaboratorRole> CollaboratorRoles => Set<CollaboratorRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Composite primary keys ────────────────────────────────────────
        modelBuilder.Entity<UserSeasonWatched>()
            .HasKey(w => new { w.UserId, w.MediaId, w.Season });

        modelBuilder.Entity<ListCollaborator>()
            .HasKey(lc => new { lc.ListId, lc.UserId });

        modelBuilder.Entity<ListItem>()
            .HasKey(li => new { li.ListId, li.MediaId });

        // ── Unique constraints ────────────────────────────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.OAuthProvider, u.OAuthId })
            .IsUnique()
            .HasFilter("[OAuthId] IS NOT NULL");

        modelBuilder.Entity<UserList>()
            .HasIndex(l => new { l.UserId, l.Name }).IsUnique();

        modelBuilder.Entity<Media>()
            .HasIndex(m => m.TmdbId).IsUnique();
        modelBuilder.Entity<Media>()
            .HasIndex(m => m.TvMazeId).IsUnique();

        modelBuilder.Entity<Rating>()
            .HasIndex(r => new { r.MediaId, r.RatingSourceId }).IsUnique();

        modelBuilder.Entity<Episode>()
            .HasIndex(e => new { e.MediaId, e.Season, e.NumberInSeason }).IsUnique();

        modelBuilder.Entity<MediaAsset>()
            .HasIndex(a => new { a.MediaId, a.AssetTypeId, a.Url }).IsUnique();

        // ── Rating precision ──────────────────────────────────────────────
        modelBuilder.Entity<Rating>()
            .Property(r => r.Score)
            .HasPrecision(4, 2);

        // ── Check constraints ─────────────────────────────────────────────
        modelBuilder.Entity<Rating>()
            .ToTable(t => t.HasCheckConstraint("ck_ratings_score_range", "Score >= 0 AND Score <= 10"));

        modelBuilder.Entity<Episode>()
            .ToTable(t => t.HasCheckConstraint("ck_episodes_season_positive", "Season > 0"));
        modelBuilder.Entity<Episode>()
            .ToTable(t => t.HasCheckConstraint("ck_episodes_number_positive", "NumberInSeason > 0"));

        // ── Delete behavior ───────────────────────────────────────────────
        modelBuilder.Entity<ListCollaborator>()
            .HasOne(lc => lc.List)
            .WithMany(l => l.Collaborators)
            .HasForeignKey(lc => lc.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ListCollaborator>()
            .HasOne(lc => lc.User)
            .WithMany(u => u.Collaborations)
            .HasForeignKey(lc => lc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.List)
            .WithMany(l => l.Comments)
            .HasForeignKey(c => c.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ListItem>()
            .HasOne(li => li.List)
            .WithMany(l => l.Items)
            .HasForeignKey(li => li.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ListItem>()
            .HasOne(li => li.Media)
            .WithMany(m => m.ListItems)
            .HasForeignKey(li => li.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Episode>()
            .HasOne(e => e.Media)
            .WithMany(m => m.Episodes)
            .HasForeignKey(e => e.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Media)
            .WithMany(m => m.Ratings)
            .HasForeignKey(r => r.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MediaAsset>()
            .HasOne(a => a.Media)
            .WithMany(m => m.Assets)
            .HasForeignKey(a => a.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSeasonWatched>()
            .HasOne(w => w.User)
            .WithMany(u => u.WatchedSeasons)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSeasonWatched>()
            .HasOne(w => w.Media)
            .WithMany(m => m.WatchedSeasons)
            .HasForeignKey(w => w.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Seed data ─────────────────────────────────────────────────────
        modelBuilder.Entity<MediaType>().HasData(
            new MediaType { Id = 1, Name = "Movie" },
            new MediaType { Id = 2, Name = "TV Show" }
        );

        modelBuilder.Entity<CollaboratorRole>().HasData(
            new CollaboratorRole { Id = 1, Name = "Owner",  SortOrder = 1 },
            new CollaboratorRole { Id = 2, Name = "Admin",  SortOrder = 2 },
            new CollaboratorRole { Id = 3, Name = "Editor", SortOrder = 3 },
            new CollaboratorRole { Id = 4, Name = "Viewer", SortOrder = 4 }
        );

        modelBuilder.Entity<RatingSource>().HasData(
            new RatingSource { Id = 1, Name = "TMDB" },
            new RatingSource { Id = 2, Name = "IMDb" },
            new RatingSource { Id = 3, Name = "Rotten Tomatoes" }
        );

        modelBuilder.Entity<AssetType>().HasData(
            new AssetType { Id = 1, Name = "Poster" },
            new AssetType { Id = 2, Name = "Backdrop" },
            new AssetType { Id = 3, Name = "Still" },
            new AssetType { Id = 4, Name = "Logo" }
        );
    }
}
