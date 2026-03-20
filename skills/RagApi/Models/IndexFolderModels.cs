using System.ComponentModel.DataAnnotations;

namespace RagApi.Models;

public sealed class IndexFolderRequest
{
    [Required]
    public string? FolderPath { get; set; }

    [Range(1, 100)]
    public int? MaxFiles { get; set; }
}

public sealed class IndexFolderResponse
{
    public required int TotalFiles { get; init; }
    public required int IndexedFiles { get; init; }
    public required int SkippedFiles { get; init; }
    public required List<string> Errors { get; init; }
}
