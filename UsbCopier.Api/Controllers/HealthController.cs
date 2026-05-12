using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsbCopier.Api.Data;

namespace UsbCopier.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly UsbCopierDbContext _db;
    public HealthController(UsbCopierDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            return Ok(new
            {
                api = "ok",
                database = canConnect ? "ok" : "unreachable",
                serverTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                api = "ok",
                database = "error",
                error = ex.Message
            });
        }
    }
}
