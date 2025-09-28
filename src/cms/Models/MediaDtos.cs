namespace cms.Models;

public record MediaCreateResponse(
    Guid id, string hash, string original_url, int width, int height, string mime
);

public record MediaListItem(
    Guid id, string hash, string original_url, int width, int height, string mime, long bytes, string? alt_text, DateTimeOffset created_at
);

public record PagedMediaResponse(IEnumerable<MediaListItem> items, int page, int pageSize, long total);