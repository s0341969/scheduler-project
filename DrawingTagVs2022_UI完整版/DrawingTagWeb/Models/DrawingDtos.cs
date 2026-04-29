namespace DrawingTagWeb.Models;

public sealed class DrawingSpecDto
{
    public int ItemNo { get; set; }
    public string? InspectionMethod { get; set; }
}

public sealed class DrawingSpecLookupResponse
{
    public List<Dictionary<string, object?>> SpecRows { get; set; } = new();
    public List<PdfOptionDto> PdfOptions { get; set; } = new();
}

public sealed class PdfOptionDto
{
    public string FilePath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class SaveDrawingProjectRequest
{
    public string DrawingNumber { get; set; } = string.Empty;
    public string? ImageSrc { get; set; }
    public string? Image { get; set; }
    public Dictionary<string, string>? SpecDataMap { get; set; }
    public string? CurrentSeq { get; set; }
    public decimal? CurrentZoom { get; set; }
    public List<DrawingTagRequest> Tags { get; set; } = new();
}

public sealed class DrawingTagRequest
{
    public string? Id { get; set; }
    public string? Method { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class DrawingProjectResponse
{
    public int ProjectId { get; set; }
    public string DrawingNumber { get; set; } = string.Empty;
    public int Version { get; set; }
    public int VersionNo { get; set; }
    public string? ImageSrc { get; set; }
    public string? Image { get; set; }
    public Dictionary<string, string>? SpecDataMap { get; set; }
    public string? CurrentSeq { get; set; }
    public decimal CurrentZoom { get; set; }
    public List<DrawingTagResponse> Tags { get; set; } = new();
}

public sealed class DrawingTagResponse
{
    public string Id { get; set; } = string.Empty;
    public string? Method { get; set; }
    public int ItemNo { get; set; }
    public string? InspectionMethod { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
