using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsbCopier.Api.Models;

[Table("profile_schedule_times")]
public class ProfileScheduleTime
{
    [Key]
    [Column("time_id")]
    public int TimeId { get; set; }

    [Column("profile_id")]
    public int ProfileId { get; set; }

    [Column("hh")]
    public byte Hour { get; set; }

    [Column("mm")]
    public byte Minute { get; set; }

    public Profile? Profile { get; set; }
}
