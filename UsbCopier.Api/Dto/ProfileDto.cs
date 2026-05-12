namespace UsbCopier.Api.Dto;

public class ProfileDto
{
    public int ProfileId { get; set; }
    public string Name { get; set; } = "";
    public string DestinationPath { get; set; } = "";
    public bool IncludeSubfolders { get; set; } = true;
    public string Grouping { get; set; } = "Original";
    public string DateGranularity { get; set; } = "Month";
    public string TriggerMode { get; set; } = "OnUsbConnect";
    public string BackupMode { get; set; } = "NewVersion";
    public int EveryNHours { get; set; }
    public string CustomExtensions { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public List<ScheduleTimeDto> ScheduleTimes { get; set; } = new();
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public List<ExtensionDto> Extensions { get; set; } = new();
}

public class ExtensionDto
{
    public int ExtensionId { get; set; }
    public string Extension { get; set; } = "";
    public bool IsChecked { get; set; } = true;
}

public class ScheduleTimeDto
{
    public int TimeId { get; set; }
    public byte Hour { get; set; }
    public byte Minute { get; set; }
}

public class ProfileSummaryDto
{
    public int ProfileId { get; set; }
    public string Name { get; set; } = "";
    public string TriggerMode { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}
