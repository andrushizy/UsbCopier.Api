using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsbCopier.Api.Data;
using UsbCopier.Api.Dto;
using UsbCopier.Api.Middleware;
using UsbCopier.Api.Models;

namespace UsbCopier.Api.Controllers;

[ApiController]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly UsbCopierDbContext _db;
    public ProfilesController(UsbCopierDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ProfileSummaryDto>>> GetAll()
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var isAdmin = HttpContext.IsAdmin();

        var q = _db.Profiles.AsQueryable();
        if (!isAdmin) q = q.Where(p => p.UserId == userId);

        var rows = await q
            .OrderBy(p => p.Name)
            .Select(p => new ProfileSummaryDto
            {
                ProfileId = p.ProfileId,
                Name = p.Name,
                TriggerMode = p.TriggerMode,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfileDto>> GetById(int id)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var isAdmin = HttpContext.IsAdmin();

        var p = await _db.Profiles
            .Include(x => x.Categories.OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Extensions)
            .Include(x => x.ScheduleTimes)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProfileId == id);

        if (p is null) return NotFound();
        if (!isAdmin && p.UserId != userId) return Forbid();
        return Ok(ToDto(p));
    }

    [HttpPost]
    public async Task<ActionResult<ProfileDto>> Create([FromBody] ProfileDto dto)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Имя профиля не может быть пустым");

        // Уникальность имени теперь в рамках пользователя.
        if (await _db.Profiles.AnyAsync(p => p.UserId == userId && p.Name == dto.Name))
            return Conflict($"У вас уже есть профиль «{dto.Name}»");

        var p = new Profile { UserId = userId.Value };
        ApplyDto(p, dto);
        p.CreatedAt = DateTime.UtcNow;
        p.UpdatedAt = DateTime.UtcNow;

        _db.Profiles.Add(p);
        await _db.SaveChangesAsync();

        var saved = await _db.Profiles
            .Include(x => x.Categories.OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Extensions)
            .Include(x => x.ScheduleTimes)
            .AsNoTracking()
            .FirstAsync(x => x.ProfileId == p.ProfileId);

        return CreatedAtAction(nameof(GetById), new { id = saved.ProfileId }, ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProfileDto>> Update(int id, [FromBody] ProfileDto dto)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var isAdmin = HttpContext.IsAdmin();

        var p = await _db.Profiles
            .Include(x => x.Categories)
                .ThenInclude(c => c.Extensions)
            .Include(x => x.ScheduleTimes)
            .FirstOrDefaultAsync(x => x.ProfileId == id);

        if (p is null) return NotFound();
        if (!isAdmin && p.UserId != userId) return Forbid();

        if (!string.Equals(p.Name, dto.Name, StringComparison.Ordinal) &&
            await _db.Profiles.AnyAsync(x => x.UserId == p.UserId && x.Name == dto.Name && x.ProfileId != id))
            return Conflict($"У вас уже есть профиль «{dto.Name}»");

        _db.ProfileExtensions.RemoveRange(p.Categories.SelectMany(c => c.Extensions));
        _db.ProfileCategories.RemoveRange(p.Categories);
        _db.ProfileScheduleTimes.RemoveRange(p.ScheduleTimes);
        p.Categories.Clear();
        p.ScheduleTimes.Clear();

        ApplyDto(p, dto);
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var saved = await _db.Profiles
            .Include(x => x.Categories.OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Extensions)
            .Include(x => x.ScheduleTimes)
            .AsNoTracking()
            .FirstAsync(x => x.ProfileId == id);

        return Ok(ToDto(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var isAdmin = HttpContext.IsAdmin();

        var p = await _db.Profiles.FindAsync(id);
        if (p is null) return NotFound();
        if (!isAdmin && p.UserId != userId) return Forbid();

        _db.Profiles.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ProfileDto ToDto(Profile p) => new()
    {
        ProfileId = p.ProfileId,
        Name = p.Name,
        DestinationPath = p.DestinationPath,
        IncludeSubfolders = p.IncludeSubfolders,
        Grouping = p.Grouping,
        DateGranularity = p.DateGranularity,
        TriggerMode = p.TriggerMode,
        BackupMode = p.BackupMode,
        EveryNHours = p.EveryNHours,
        CustomExtensions = p.CustomExtensions,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Categories = p.Categories
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                IsEnabled = c.IsEnabled,
                SortOrder = c.SortOrder,
                Extensions = c.Extensions.Select(e => new ExtensionDto
                {
                    ExtensionId = e.ExtensionId,
                    Extension = e.Extension,
                    IsChecked = e.IsChecked
                }).ToList()
            }).ToList(),
        ScheduleTimes = p.ScheduleTimes.Select(t => new ScheduleTimeDto
        {
            TimeId = t.TimeId,
            Hour = t.Hour,
            Minute = t.Minute
        }).ToList()
    };

    private static void ApplyDto(Profile p, ProfileDto dto)
    {
        p.Name = (dto.Name ?? "").Trim();
        p.DestinationPath = dto.DestinationPath ?? "";
        p.IncludeSubfolders = dto.IncludeSubfolders;
        p.Grouping = NormalizeGrouping(dto.Grouping);
        p.DateGranularity = NormalizeDate(dto.DateGranularity);
        p.TriggerMode = NormalizeTrigger(dto.TriggerMode);
        p.BackupMode = NormalizeBackupMode(dto.BackupMode);
        p.EveryNHours = Math.Max(0, dto.EveryNHours);
        p.CustomExtensions = dto.CustomExtensions ?? "";

        foreach (var cd in dto.Categories.OrderBy(c => c.SortOrder))
        {
            var cat = new ProfileCategory
            {
                Name = cd.Name,
                IsEnabled = cd.IsEnabled,
                SortOrder = cd.SortOrder
            };
            foreach (var ed in cd.Extensions)
            {
                cat.Extensions.Add(new ProfileExtension
                {
                    Extension = NormalizeExt(ed.Extension),
                    IsChecked = ed.IsChecked
                });
            }
            p.Categories.Add(cat);
        }

        foreach (var st in dto.ScheduleTimes)
        {
            if (st.Hour > 23 || st.Minute > 59) continue;
            p.ScheduleTimes.Add(new ProfileScheduleTime { Hour = st.Hour, Minute = st.Minute });
        }
    }

    private static string NormalizeGrouping(string? s) => s switch
    {
        "Original" or "ByType" or "ByDate" or "BySize" => s,
        _ => "Original"
    };
    private static string NormalizeDate(string? s) => s == "Year" ? "Year" : "Month";
    private static string NormalizeTrigger(string? s) => s switch
    {
        "Manual" or "OnUsbConnect" or "Schedule" => s,
        _ => "OnUsbConnect"
    };
    private static string NormalizeBackupMode(string? s) => s switch
    {
        "NewVersion" or "UpdateLatest" => s,
        _ => "NewVersion"
    };
    private static string NormalizeExt(string raw)
    {
        var t = (raw ?? "").Trim().ToLowerInvariant();
        if (t.Length == 0) return "";
        if (!t.StartsWith('.')) t = "." + t;
        return t;
    }
}
