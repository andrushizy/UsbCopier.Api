using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsbCopier.Api.Data;
using UsbCopier.Api.Dto;
using UsbCopier.Api.Middleware;
using UsbCopier.Api.Models;

namespace UsbCopier.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly UsbCopierDbContext _db;
    public DevicesController(UsbCopierDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<KnownDeviceDto>>> GetAll()
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var isAdmin = HttpContext.IsAdmin();

        var q = _db.KnownDevices.AsQueryable();
        if (!isAdmin) q = q.Where(d => d.UserId == userId);

        var rows = await q
            .OrderByDescending(d => d.LastSeenAt)
            .Select(d => new KnownDeviceDto
            {
                DeviceId = d.DeviceId,
                VolumeSerial = d.VolumeSerial,
                VolumeLabel = d.VolumeLabel,
                FileSystem = d.FileSystem,
                TotalBytes = d.TotalBytes,
                FirstSeenAt = d.FirstSeenAt,
                LastSeenAt = d.LastSeenAt
            })
            .ToListAsync();
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<KnownDeviceDto>> Upsert([FromBody] KnownDeviceDto dto)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.VolumeLabel))
            return BadRequest("VolumeLabel обязателен");

        var dtoSerial = dto.VolumeSerial ?? "";
        var existing = await _db.KnownDevices.FirstOrDefaultAsync(d =>
            d.UserId == userId &&
            (d.VolumeSerial ?? "") == dtoSerial &&
            d.VolumeLabel == dto.VolumeLabel);

        if (existing is null)
        {
            var d = new KnownDevice
            {
                UserId = userId.Value,
                VolumeSerial = dto.VolumeSerial,
                VolumeLabel = dto.VolumeLabel,
                FileSystem = dto.FileSystem,
                TotalBytes = dto.TotalBytes,
                FirstSeenAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };
            _db.KnownDevices.Add(d);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), null, ToDto(d));
        }
        else
        {
            existing.FileSystem = dto.FileSystem;
            existing.TotalBytes = dto.TotalBytes;
            existing.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(ToDto(existing));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var isAdmin = HttpContext.IsAdmin();

        var d = await _db.KnownDevices.FindAsync(id);
        if (d is null) return NotFound();
        if (!isAdmin && d.UserId != userId) return Forbid();

        _db.KnownDevices.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static KnownDeviceDto ToDto(KnownDevice d) => new()
    {
        DeviceId = d.DeviceId,
        VolumeSerial = d.VolumeSerial,
        VolumeLabel = d.VolumeLabel,
        FileSystem = d.FileSystem,
        TotalBytes = d.TotalBytes,
        FirstSeenAt = d.FirstSeenAt,
        LastSeenAt = d.LastSeenAt
    };
}
