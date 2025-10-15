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
    public DbSet<PageNodeDto> SiteNodes => Set<PageNodeDto>();
    public DbSet<PageResult> AddPageResults => Set<PageResult>(); // raw-sql result
    public DbSet<cms.Models.DeletePageResult> DeletePageResults => Set<cms.Models.DeletePageResult>();
    
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
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(64).IsRequired();

            e.Property(x => x.Width).HasColumnName("width");
            e.Property(x => x.Height).HasColumnName("height");

            e.Property(x => x.Types).HasColumnName("types").HasMaxLength(128).IsRequired();

            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            // === Computed / GENERATED ALWAYS kolonner ===
            e.Property(x => x.RatioW)
                .HasColumnName("ratio_w")
                .HasComputedColumnSql(@"
        CASE
            WHEN width > 0 AND height > 0 THEN width / gcd_int(width, height)
            ELSE 0
        END", stored: true)
                .ValueGeneratedOnAddOrUpdate();

            e.Property(x => x.RatioH)
                .HasColumnName("ratio_h")
                .HasComputedColumnSql(@"
        CASE
            WHEN width > 0 AND height > 0 THEN height / gcd_int(width, height)
            ELSE 0
        END", stored: true)
                .ValueGeneratedOnAddOrUpdate();

            e.Property(x => x.RatioKey)
                .HasColumnName("ratio_key")
                .HasMaxLength(32)
                .HasComputedColumnSql(@"
        CASE
            WHEN width > 0 AND height > 0 THEN (width / gcd_int(width, height))::text || ':' || (height / gcd_int(width, height))::text
            ELSE 'free'
        END", stored: true)
                .ValueGeneratedOnAddOrUpdate();

            // (Valgfrit, men “belt & suspenders”) – tving EF til aldrig at sende værdier i INSERT/UPDATE:
            e.Property(x => x.RatioW).Metadata.SetBeforeSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
            e.Property(x => x.RatioW).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
            e.Property(x => x.RatioH).Metadata.SetBeforeSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
            e.Property(x => x.RatioH).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
            e.Property(x => x.RatioKey).Metadata.SetBeforeSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
            e.Property(x => x.RatioKey).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

            // Index på ratio_key – brug samme navn som i din SQL, så EF ikke forsøger at lave et ekstra:
            e.HasIndex(x => x.RatioKey).HasDatabaseName("idx_media_presets_ratio_key");
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
        
        // Map PageNodeDto til view cms.site_nodes
        b.Entity<PageNodeDto>(eb =>
        {
            eb.HasNoKey();
            eb.ToView("site_nodes", schema: "cms");
            eb.Property(x => x.Id).HasColumnName("id");
            eb.Property(x => x.ParentId).HasColumnName("parent_id");
            eb.Property(x => x.FullPath).HasColumnName("full_path");
            eb.Property(x => x.Slug).HasColumnName("slug");
            eb.Property(x => x.Title).HasColumnName("title");
            eb.Property(x => x.Depth).HasColumnName("depth");
            eb.Property(x => x.InMenu).HasColumnName("in_menu");
            eb.Property(x => x.InSitemap).HasColumnName("in_sitemap");
            eb.Property(x => x.LatestVersionNo).HasColumnName("latest_version_no");
            eb.Property(x => x.PublishedVersionNo).HasColumnName("published_version_no");
            eb.Property(x => x.HasPublished).HasColumnName("has_published");
            eb.Property(x => x.HasNewerDraft).HasColumnName("has_newer_draft");
            eb.Property(x=>x.LatestTemplateId).HasColumnName("latest_template_id");
        });
        
        b.Entity<PageResult>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null);
            eb.Property(p => p.New_Page_Id).HasColumnName("new_page_id");
            eb.Property(p => p.New_Version_No).HasColumnName("new_version_no");
        });
        
        b.Entity<cms.Models.DeletePageResult>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null);
            eb.Property(x => x.Deleted_Page_Id).HasColumnName("deleted_page_id");
            eb.Property(x => x.Deleted_Path).HasColumnName("deleted_path");
            eb.Property(x => x.Deleted_Count).HasColumnName("deleted_count");
        });

    }
}