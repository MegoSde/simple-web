namespace cms.Models;

public record MediaCreateResponse(
    Guid Id, string Hash, string OriginalUrl, int Width, int Height, string Mime
);

public record MediaListItem(
    Guid Id, string Hash, string OriginalUrl, int Width, int Height, string Mime, long Bytes, string? AltText, DateTimeOffset CreatedAt
);

public record PagedMediaResponse(IEnumerable<MediaListItem> Items, int Page, int PageSize, long Total);