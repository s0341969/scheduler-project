using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class ReportService(
    ApplicationDbContext dbContext,
    IAuditLogService auditLogService,
    IOptions<VulnScanOptions> options) : IReportService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<string> ExportVulnerabilityExcelAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var filePath = BuildReportPath("vulnerability-export", "xlsx");
        var items = await QueryVulnerabilitiesAsync(start, end, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Vulnerabilities");
        worksheet.Cell(1, 1).Value = "弱點編號";
        worksheet.Cell(1, 2).Value = "資產編號";
        worksheet.Cell(1, 3).Value = "IP 位址";
        worksheet.Cell(1, 4).Value = "弱點名稱";
        worksheet.Cell(1, 5).Value = "Severity";
        worksheet.Cell(1, 6).Value = "CVSS";
        worksheet.Cell(1, 7).Value = "軟體版本";
        worksheet.Cell(1, 8).Value = "特徵碼版本";
        worksheet.Cell(1, 9).Value = "狀態";

        for (var index = 0; index < items.Count; index += 1)
        {
            var row = index + 2;
            worksheet.Cell(row, 1).Value = items[index].VulnId;
            worksheet.Cell(row, 2).Value = items[index].AssetId;
            worksheet.Cell(row, 3).Value = items[index].IPAddress;
            worksheet.Cell(row, 4).Value = items[index].VulnName;
            worksheet.Cell(row, 5).Value = items[index].Severity;
            worksheet.Cell(row, 6).Value = items[index].CVSS;
            worksheet.Cell(row, 7).Value = items[index].DetectedVersion ?? items[index].ServiceName;
            worksheet.Cell(row, 8).Value = items[index].SignatureVersion;
            worksheet.Cell(row, 9).Value = items[index].Status;
        }

        workbook.SaveAs(filePath);
        await SaveExportRecordAsync("弱點掃描總表", "Excel", filePath, cancellationToken);
        return filePath;
    }

    public async Task<string> ExportHighRiskExcelAsync(CancellationToken cancellationToken = default)
    {
        var filePath = BuildReportPath("high-risk-export", "xlsx");
        var items = await dbContext.Vulnerabilities
            .AsNoTracking()
            .Where(item => item.Severity == "Critical" || item.Severity == "High")
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("HighRisk");
        worksheet.Cell(1, 1).Value = "IP 位址";
        worksheet.Cell(1, 2).Value = "弱點名稱";
        worksheet.Cell(1, 3).Value = "Severity";
        worksheet.Cell(1, 4).Value = "軟體版本";
        worksheet.Cell(1, 5).Value = "特徵碼版本";
        worksheet.Cell(1, 6).Value = "改善期限";

        for (var index = 0; index < items.Count; index += 1)
        {
            var row = index + 2;
            worksheet.Cell(row, 1).Value = items[index].IPAddress;
            worksheet.Cell(row, 2).Value = items[index].VulnName;
            worksheet.Cell(row, 3).Value = items[index].Severity;
            worksheet.Cell(row, 4).Value = items[index].DetectedVersion ?? items[index].ServiceName;
            worksheet.Cell(row, 5).Value = items[index].SignatureVersion;
            worksheet.Cell(row, 6).Value = items[index].DueDate?.ToString("yyyy-MM-dd");
        }

        workbook.SaveAs(filePath);
        await SaveExportRecordAsync("高風險弱點清單", "Excel", filePath, cancellationToken);
        return filePath;
    }

    public async Task<string> ExportIso27001PdfAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var filePath = BuildReportPath("iso27001-summary", "pdf");
        var items = await QueryVulnerabilitiesAsync(start, end, cancellationToken);
        var document = new PdfDocument();
        document.Info.Title = "VulnScan 弱點管理報告";
        document.Info.Author = "VulnScan.Web";

        var titleFont = new XFont(PdfFontResolver.DefaultFontName, 18, XFontStyle.Bold);
        var headerFont = new XFont(PdfFontResolver.DefaultFontName, 10, XFontStyle.Bold);
        var bodyFont = new XFont(PdfFontResolver.DefaultFontName, 9, XFontStyle.Regular);
        var smallFont = new XFont(PdfFontResolver.DefaultFontName, 8, XFontStyle.Regular);

        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        page.Orientation = PdfSharpCore.PageOrientation.Landscape;
        var graphics = XGraphics.FromPdfPage(page);
        var margin = 32d;
        var y = margin;

        graphics.DrawString("VulnScan 弱點管理報告", titleFont, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, 28), XStringFormats.TopLeft);
        y += 30;
        graphics.DrawString($"報表期間：{start:yyyy-MM-dd} 至 {end:yyyy-MM-dd}", bodyFont, XBrushes.Black, new XRect(margin, y, 320, 20), XStringFormats.TopLeft);
        y += 24;

        var vulnerabilityCount = items.Count;
        var highRiskCount = items.Count(item => item.Severity is "Critical" or "High");
        var assetsCount = items.Select(item => item.AssetId).Distinct().Count();
        DrawSummaryCard(graphics, margin, y, 180, 58, "弱點總數", vulnerabilityCount.ToString());
        DrawSummaryCard(graphics, margin + 196, y, 180, 58, "高風險弱點", highRiskCount.ToString());
        DrawSummaryCard(graphics, margin + 392, y, 180, 58, "受影響資產", assetsCount.ToString());
        y += 78;

        var columns = new (string Header, double Width)[]
        {
            ("資產", 90),
            ("IP", 90),
            ("弱點名稱", 185),
            ("Severity", 60),
            ("CVSS", 45),
            ("軟體版本", 90),
            ("特徵碼版本", 95),
            ("最後發現", 90),
        };

        DrawTableHeader(graphics, headerFont, margin, y, columns);
        y += 20;

        foreach (var item in items.OrderByDescending(item => item.LastDetectedAt))
        {
            if (y > page.Height - 36)
            {
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                graphics = XGraphics.FromPdfPage(page);
                y = margin;
                DrawTableHeader(graphics, headerFont, margin, y, columns);
                y += 20;
            }

            DrawTableRow(graphics, bodyFont, smallFont, margin, y, columns, new[]
            {
                item.Asset?.AssetName ?? "-",
                item.IPAddress ?? "-",
                item.VulnName,
                item.Severity ?? "-",
                item.CVSS?.ToString("0.0") ?? "-",
                item.DetectedVersion ?? item.ServiceName ?? "-",
                item.SignatureVersion ?? "-",
                item.LastDetectedAt.ToLocalTime().ToString("yyyy-MM-dd"),
            });
            y += 18;
        }

        document.Save(filePath);
        await SaveExportRecordAsync("ISO27001 弱點管理 PDF", "PDF", filePath, cancellationToken);
        return filePath;
    }

    private string BuildReportPath(string namePrefix, string extension)
    {
        var reportRoot = Path.Combine(_options.ResultRootPath, "reports");
        Directory.CreateDirectory(reportRoot);
        return Path.Combine(reportRoot, $"{namePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}");
    }

    private async Task SaveExportRecordAsync(string reportName, string reportType, string filePath, CancellationToken cancellationToken)
    {
        dbContext.ReportExports.Add(new ReportExport
        {
            ReportName = reportName,
            ReportType = reportType,
            FilePath = filePath,
            ExportedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync("ReportExported", "ReportExport", null, $"{reportName} => {filePath}", null, null, cancellationToken);
    }

    private Task<List<Vulnerability>> QueryVulnerabilitiesAsync(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        return dbContext.Vulnerabilities
            .AsNoTracking()
            .Include(item => item.Asset)
            .Where(item => item.FirstDetectedAt >= start && item.FirstDetectedAt <= end)
            .ToListAsync(cancellationToken);
    }

    private static void DrawSummaryCard(XGraphics graphics, double x, double y, double width, double height, string label, string value)
    {
        graphics.DrawRoundedRectangle(new XPen(XColor.FromArgb(208, 220, 235), 1), XBrushes.WhiteSmoke, x, y, width, height, 10, 10);
        graphics.DrawString(label, new XFont(PdfFontResolver.DefaultFontName, 9, XFontStyle.Regular), XBrushes.DimGray, new XRect(x + 12, y + 10, width - 24, 16), XStringFormats.TopLeft);
        graphics.DrawString(value, new XFont(PdfFontResolver.DefaultFontName, 16, XFontStyle.Bold), XBrushes.Black, new XRect(x + 12, y + 24, width - 24, 24), XStringFormats.TopLeft);
    }

    private static void DrawTableHeader(XGraphics graphics, XFont font, double x, double y, IReadOnlyList<(string Header, double Width)> columns)
    {
        var currentX = x;
        foreach (var column in columns)
        {
            graphics.DrawRectangle(XBrushes.LightGray, currentX, y, column.Width, 18);
            graphics.DrawString(column.Header, font, XBrushes.Black, new XRect(currentX + 4, y + 3, column.Width - 8, 12), XStringFormats.TopLeft);
            currentX += column.Width;
        }
    }

    private static void DrawTableRow(XGraphics graphics, XFont font, XFont smallFont, double x, double y, IReadOnlyList<(string Header, double Width)> columns, IReadOnlyList<string> values)
    {
        var currentX = x;
        for (var index = 0; index < columns.Count; index += 1)
        {
            var column = columns[index];
            var value = index < values.Count ? values[index] : string.Empty;
            graphics.DrawRectangle(new XPen(XColor.FromArgb(220, 226, 235), 0.6), currentX, y, column.Width, 18);
            graphics.DrawString(
                TrimForCell(value, column.Width > 120 ? 40 : 20),
                column.Width > 100 ? smallFont : font,
                XBrushes.Black,
                new XRect(currentX + 3, y + 3, column.Width - 6, 12),
                XStringFormats.TopLeft);
            currentX += column.Width;
        }
    }

    private static string TrimForCell(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 1)] + "…";
    }
}
