using cms.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace cms.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaPreset> MediaPresets => Set<MediaPreset>();
    
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // vigtigt når du arver fra IdentityDbContext

        b.Entity<MediaAsset>(e =>
        {
            e.ToTable("media_assets");
            e.HasKey(p => p.Id);

            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Hash).HasColumnName("hash");
            e.HasIndex(p => p.Hash).IsUnique();

            e.Property(p => p.OriginalUrl).HasColumnName("original_url");
            e.Property(p => p.Mime).HasColumnName("mime");
            e.Property(p => p.Width).HasColumnName("width");
            e.Property(p => p.Height).HasColumnName("height");
            e.Property(p => p.Bytes).HasColumnName("bytes");
            e.Property(p => p.AltText).HasColumnName("alt_text");
            e.Property(p => p.UploadedBy).HasColumnName("uploaded_by");

            // Lad gerne DB sætte default-tidspunktet:
            e.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now() at time zone 'utc'");

            // Map til jsonb hvis du kører PostgreSQL:
            e.Property(p => p.Meta)
                .HasColumnName("meta")
                .HasColumnType("jsonb");
        });
        
        b.Entity<MediaPreset>(e =>
        {
            e.ToTable("media_presets");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Width).HasColumnName("width");
            e.Property(x => x.Height).HasColumnName("height");
            e.Property(x => x.Types).HasColumnName("types");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
public class MediaAsset {
    public Guid Id { get; set; }
    public string Hash { get; set; } = default!;
    public string OriginalUrl { get; set; } = default!;
    public string Mime { get; set; } = default!;
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bytes { get; set; }
    public string? AltText { get; set; }
    public string UploadedBy { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Meta { get; set; } = "{}"; // map til jsonb via EF: string <-> jsonb
    // Alternativt: brug JsonDocument / Dictionary<string,object> + EF converter
}