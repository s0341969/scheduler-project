using System.ComponentModel.DataAnnotations;

namespace RagApi.Models;

public sealed class RetrieveRequest
{
    [Required]
    [MinLength(2)]
    public string Query { get; set; } = string.Empty;

    [Range(1, 100)]
    public int? TopK { get; set; }

    public string? UserId { get; set; }
}
