using cms.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace cms.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaPreset> MediaPresets => Set<MediaPreset>();
    public DbSet<MediaAssetCrop> MediaAssetCrops => Set<MediaAssetCrop>();
    public DbSet<Template> Templates => Set<Template>();
    
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
            e.Property(x => x.Types).HasColumnName("types").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.RatioKey);
            e.Property(x => x.RatioW).HasColumnName("ratio_w");
            e.Property(x => x.RatioH).HasColumnName("ratio_h");
            e.Property(x => x.RatioKey).HasColumnName("ratio_key");
        });
        
        b.Entity<MediaAssetCrop>(e =>
        {
            e.ToTable("media_asset_crops");
            e.HasIndex(x => new { x.AssetHash, x.PresetName }).IsUnique();
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AssetHash).HasColumnName("asset_hash");
            e.Property(x => x.PresetName).HasColumnName("preset_name");
            e.Property(x => x.X).HasColumnName("x");
            e.Property(x => x.Y).HasColumnName("y");
            e.Property(x => x.W).HasColumnName("w");
            e.Property(x => x.H).HasColumnName("h");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        b.Entity<Template>(e =>
        {
            e.ToTable("templates");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.Root).HasColumnName("root").HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        });

    }
}