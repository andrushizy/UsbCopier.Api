using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsbCopier.Api.Models;

[Table("profiles")]
public class Profile
{
    // Имя свойства (ProfileId) совпадает с конвенцией <Class>Id, поэтому
    // [Key] технически не нужен — но я ставлю его явно во всех сущностях
    // для единообразия. Без [Key] на других сущностях (CategoryId, ErrorId
    // и т.д.) EF Core падал при валидации модели.
    [Key]
    [Column("profile_id")]
    public int ProfileId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("name"), Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("destination_path"), MaxLength(500)]
    public string DestinationPath { get; set; } = "";

    [Column("include_subfolders")]
    public bool IncludeSubfolders { get; set; } = true;

    [Column("grouping"), MaxLength(20)]
    public string Grouping { get; set; } = "Original";

    [Column("date_granularity"), MaxLength(10)]
    public string DateGranularity { get; set; } = "Month";

    [Column("trigger_mode"), MaxLength(20)]
    public string TriggerMode { get; set; } = "OnUsbConnect";

    /// <summary>"NewVersion" — каждый запуск создаёт новую папку с timestamp.
    /// "UpdateLatest" — копирование в одну и ту же папку с пропуском неизменённых файлов.</summary>
    [Column("backup_mode"), MaxLength(20)]
    public string BackupMode { get; set; } = "NewVersion";

    [Column("every_n_hours")]
    public int EveryNHours { get; set; }

    [Column("custom_extensions"), MaxLength(500)]
    public string CustomExtensions { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public List<ProfileCategory> Categories { get; set; } = new();
    public List<ProfileScheduleTime> ScheduleTimes { get; set; } = new();
}
