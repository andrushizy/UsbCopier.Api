using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsbCopier.Api.Models;

[Table("backup_history")]
public class BackupHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("profile_id")]
    public int? ProfileId { get; set; }

    [Column("device_id")]
    public int? DeviceId { get; set; }

    [Column("trigger"), MaxLength(20)]
    public string Trigger { get; set; } = "Manual";

    [Column("status"), MaxLength(20)]
    public string Status { get; set; } = "Success";

    [Column("source_letter"), MaxLength(10)]
    public string SourceLetter { get; set; } = "";

    [Column("source_label"), MaxLength(100)]
    public string SourceLabel { get; set; } = "";

    [Column("target_folder"), MaxLength(500)]
    public string TargetFolder { get; set; } = "";

    [Column("files_copied")]
    public int FilesCopied { get; set; }

    [Column("files_failed")]
    public int FilesFailed { get; set; }

    [Column("error_message"), MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime FinishedAt { get; set; }

    public Profile? Profile { get; set; }
    public KnownDevice? Device { get; set; }
    public List<BackupError> Errors { get; set; } = new();
}

[Table("backup_errors")]
public class BackupError
{
    // Это и было местом падения: имя ErrorId не совпадает с конвенцией
    // <Class>Id (BackupErrorId), поэтому без [Key] EF Core ругался
    // "requires a primary key to be defined".
    [Key]
    [Column("error_id")]
    public int ErrorId { get; set; }

    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("file_path"), Required, MaxLength(1000)]
    public string FilePath { get; set; } = "";

    [Column("error_message"), Required, MaxLength(1000)]
    public string ErrorMessage { get; set; } = "";

    public BackupHistory? History { get; set; }
}
