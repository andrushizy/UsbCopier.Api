using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using UsbCopier.Api.Data;
using UsbCopier.Api.Middleware;
using UsbCopier.Api.Models;
using UsbCopier.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UsbCopier API",
        Version = "v1",
        Description = "REST API для UsbCopier: профили, флешки, история, авторизация."
    });

    // Bearer-схема в Swagger: «Authorize» → токен → запросы летят с заголовком.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "Token",
        In = ParameterLocation.Header,
        Description = "Введите: Bearer <ваш-токен>"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme, Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var conn = builder.Configuration.GetConnectionString("Mysql")
    ?? throw new InvalidOperationException("ConnectionStrings:Mysql не задан в appsettings.json");

builder.Services.AddDbContext<UsbCopierDbContext>(opts =>
{
    opts.UseMySql(conn, new MySqlServerVersion(new Version(8, 0, 0)));
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ── Seed: создаём admin'а если БД пустая ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsbCopierDbContext>();
    try
    {
        // Безопасно: если соединение с БД нет, продолжаем без падения —
        // ошибка проявится позднее при первом запросе.
        if (await db.Database.CanConnectAsync() && !await db.Users.AnyAsync())
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = PasswordHasher.Hash("Admin123!"),
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            Console.WriteLine("[seed] Создан admin / Admin123!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("[seed] Не удалось проверить/создать admin: " + ex.Message);
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UsbCopier API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();

// Middleware читает Authorization: Bearer и кладёт UserId/IsAdmin в HttpContext.Items.
app.UseMiddleware<BearerAuthMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
