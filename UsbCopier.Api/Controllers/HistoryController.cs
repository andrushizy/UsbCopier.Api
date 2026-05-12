using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsbCopier.Api.Data;
using UsbCopier.Api.Dto;
using UsbCopier.Api.Middleware;
using UsbCopier.Api.Models;

namespace UsbCopier.Api.Controllers;

[ApiController]
[Route("api/history")]
public class HistoryController : ControllerBase
{
    private readonly UsbCopierDbContext _db;
    public HistoryController(UsbCopierDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<BackupHistoryDto>>> GetHistory(
        [FromQuery] int? profileId = null,
        [FromQuery] int take = 100)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var isAdmin = HttpContext.IsAdmin();

        take = Math.Clamp(take, 1, 1000);

        var q = _db.BackupHistory
            .Include(h => h.Profile)
            .Include(h => h.Errors)
            .AsNoTracking()
            .AsQueryable();

        if (!isAdmin) q = q.Where(h => h.UserId == userId);
        if (profileId is not null) q = q.Where(h => h.ProfileId == profileId);

        var rows = await q
            .OrderByDescending(h => h.StartedAt)
            .Take(take)
            .ToListAsync();

        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<BackupHistoryDto>> Create([FromBody] BackupHistoryDto dto)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();

        var h = new BackupHistory
        {
            UserId = userId.Value,
            ProfileId = dto.ProfileId,
            DeviceId = dto.DeviceId,
            Trigger = NormalizeTrigger(dto.Trigger),
            Status = NormalizeStatus(dto.Status),
            SourceLetter = dto.SourceLetter ?? "",
            SourceLabel = dto.SourceLabel ?? "",
            TargetFolder = dto.TargetFolder ?? "",
            FilesCopied = dto.FilesCopied,
            FilesFailed = dto.FilesFailed,
            ErrorMessage = string.IsNullOrEmpty(dto.ErrorMessage) ? null : dto.ErrorMessage,
            StartedAt = dto.StartedAt == default ? DateTime.UtcNow : dto.StartedAt,
            FinishedAt = dto.FinishedAt == default ? DateTime.UtcNow : dto.FinishedAt
        };

        foreach (var err in dto.Errors)
        {
            h.Errors.Add(new BackupError
            {
                FilePath = err.FilePath,
                ErrorMessage = err.ErrorMessage
            });
        }

        _db.BackupHistory.Add(h);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHistory), null, ToDto(h));
    }

    private static BackupHistoryDto ToDto(BackupHistory h) => new()
    {
        HistoryId = h.HistoryId,
        ProfileId = h.ProfileId,
        DeviceId = h.DeviceId,
        ProfileName = h.Profile?.Name,
        Trigger = h.Trigger,
        Status = h.Status,
        SourceLetter = h.SourceLetter,
        SourceLabel = h.SourceLabel,
        TargetFolder = h.TargetFolder,
        FilesCopied = h.FilesCopied,
        FilesFailed = h.FilesFailed,
        ErrorMessage = h.ErrorMessage,
        StartedAt = h.StartedAt,
        FinishedAt = h.FinishedAt,
        Errors = h.Errors.Select(e => new BackupErrorDto
        {
            FilePath = e.FilePath,
            ErrorMessage = e.ErrorMessage
        }).ToList()
    };

    private static string NormalizeTrigger(string? s) => s switch
    {
        "Manual" or "AutoOnConnect" or "Schedule" => s,
        _ => "Manual"
    };

    private static string NormalizeStatus(string? s) => s switch
    {
        "Success" or "PartialErrors" or "Failed" or "Cancelled" or "NoFilesMatched" => s,
        _ => "Success"
    };
}
