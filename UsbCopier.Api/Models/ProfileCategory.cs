using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsbCopier.Api.Models;

[Table("profile_categories")]
public class ProfileCategory
{
    [Key]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("profile_id")]
    public int ProfileId { get; set; }

    [Column("name"), Required, MaxLength(50)]
    public string Name { get; set; } = "";

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    public Profile? Profile { get; set; }
    public List<ProfileExtension> Extensions { get; set; } = new();
}

[Table("profile_extensions")]
public class ProfileExtension
{
    [Key]
    [Column("extension_id")]
    public int ExtensionId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("extension"), Required, MaxLength(20)]
    public string Extension { get; set; } = "";

    [Column("is_checked")]
    public bool IsChecked { get; set; } = true;

    public ProfileCategory? Category { get; set; }
}
