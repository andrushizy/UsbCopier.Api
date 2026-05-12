using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsbCopier.Api.Models;

[Table("known_devices")]
public class KnownDevice
{
    [Key]
    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("volume_serial"), MaxLength(64)]
    public string? VolumeSerial { get; set; }

    [Column("volume_label"), Required, MaxLength(100)]
    public string VolumeLabel { get; set; } = "";

    [Column("file_system"), MaxLength(20)]
    public string? FileSystem { get; set; }

    [Column("total_bytes")]
    public long TotalBytes { get; set; }

    [Column("first_seen_at")]
    public DateTime FirstSeenAt { get; set; }

    [Column("last_seen_at")]
    public DateTime LastSeenAt { get; set; }
}
