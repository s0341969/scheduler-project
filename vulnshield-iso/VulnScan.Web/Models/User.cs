using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Account { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Email { get; set; }

    [Required]
    [StringLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? PasswordChangedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
