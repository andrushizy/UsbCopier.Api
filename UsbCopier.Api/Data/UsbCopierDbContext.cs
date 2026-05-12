using Microsoft.EntityFrameworkCore;
using UsbCopier.Api.Models;

namespace UsbCopier.Api.Data;

public class UsbCopierDbContext : DbContext
{
    public UsbCopierDbContext(DbContextOptions<UsbCopierDbContext> options) : base(options) { }

    public DbSet<User>                Users               => Set<User>();
    public DbSet<Session>             Sessions            => Set<Session>();
    public DbSet<Profile>             Profiles            => Set<Profile>();
    public DbSet<ProfileCategory>     ProfileCategories   => Set<ProfileCategory>();
    public DbSet<ProfileExtension>    ProfileExtensions   => Set<ProfileExtension>();
    public DbSet<ProfileScheduleTime> ProfileScheduleTimes => Set<ProfileScheduleTime>();
    public DbSet<KnownDevice>         KnownDevices        => Set<KnownDevice>();
    public DbSet<BackupHistory>       BackupHistory       => Set<BackupHistory>();
    public DbSet<BackupError>         BackupErrors        => Set<BackupError>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ProfileCategory>()
            .HasOne(c => c.Profile)
            .WithMany(p => p.Categories)
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProfileExtension>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Extensions)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProfileScheduleTime>()
            .HasOne(t => t.Profile)
            .WithMany(p => p.ScheduleTimes)
            .HasForeignKey(t => t.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BackupHistory>()
            .HasOne(h => h.Profile)
            .WithMany()
            .HasForeignKey(h => h.ProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<BackupHistory>()
            .HasOne(h => h.Device)
            .WithMany()
            .HasForeignKey(h => h.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<BackupError>()
            .HasOne(e => e.History)
            .WithMany(h => h.Errors)
            .HasForeignKey(e => e.HistoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Уникальность теперь в рамках пользователя — два разных юзера
        // могут иметь профиль с одинаковым именем.
        b.Entity<Profile>()
            .HasIndex(p => new { p.UserId, p.Name })
            .IsUnique();

        b.Entity<KnownDevice>()
            .HasIndex(d => new { d.UserId, d.VolumeSerial, d.VolumeLabel })
            .IsUnique();

        b.Entity<User>().HasIndex(u => u.Username).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
