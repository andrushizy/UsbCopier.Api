namespace UsbCopier.Api.Dto;

public class KnownDeviceDto
{
    public int DeviceId { get; set; }
    public string? VolumeSerial { get; set; }
    public string VolumeLabel { get; set; } = "";
    public string? FileSystem { get; set; }
    public long TotalBytes { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}

public class BackupHistoryDto
{
    public int HistoryId { get; set; }
    public int? ProfileId { get; set; }
    public int? DeviceId { get; set; }
    public string? ProfileName { get; set; }
    public string Trigger { get; set; } = "";
    public string Status { get; set; } = "";
    public string SourceLetter { get; set; } = "";
    public string SourceLabel { get; set; } = "";
    public string TargetFolder { get; set; } = "";
    public int FilesCopied { get; set; }
    public int FilesFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public List<BackupErrorDto> Errors { get; set; } = new();
}

public class BackupErrorDto
{
    public string FilePath { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}
