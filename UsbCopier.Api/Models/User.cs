using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsbCopier.Api.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("username"), Required, MaxLength(50)]
    public string Username { get; set; } = "";

    [Column("email"), Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [Column("password_hash"), Required, MaxLength(255)]
    public string PasswordHash { get; set; } = "";

    [Column("is_admin")]
    public bool IsAdmin { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
