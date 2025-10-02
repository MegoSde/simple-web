using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cms.Models;

public sealed class Template
{
    public Guid Id { get; set; }
    [RegularExpression("^[a-z0-9-]{3,64}$")]
    [MaxLength(120)]
    public string Name { get; set; } = default!;
    public int Version { get; set; } = 1;
    [Column(TypeName = "jsonb")]
    public string Root { get; set; } = """{"type":"page","v":1,"props":{},"children":[]}"""; // json
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}