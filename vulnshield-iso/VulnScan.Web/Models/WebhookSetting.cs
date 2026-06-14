using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class WebhookSetting
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Secret { get; set; }

    public bool IsActive { get; set; } = true;

    public string Events { get; set; } = "ScanCompleted";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
