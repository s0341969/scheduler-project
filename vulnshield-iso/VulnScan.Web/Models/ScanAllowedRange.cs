using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class ScanAllowedRange
{
    [Key]
    public int RangeId { get; set; }

    [Required]
    [StringLength(100)]
    public string RangeName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Cidr { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
