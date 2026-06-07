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

        var titleFont = new XFont(PdfFontResolver.DefaultFontName, 22, XFontStyle.Bold);
        var subtitleFont = new XFont(PdfFontResolver.DefaultFontName, 14, XFontStyle.Regular);
        var headerFont = new XFont(PdfFontResolver.DefaultFontName, 10, XFontStyle.Bold);
        var bodyFont = new XFont(PdfFontResolver.DefaultFontName, 9, XFontStyle.Regular);
        var smallFont = new XFont(PdfFontResolver.DefaultFontName, 8, XFontStyle.Regular);

        var now = DateTime.UtcNow.ToLocalTime();
        var pageCount = 0;

        // ---------- Cover Page ----------
        pageCount++;
        var coverPage = document.AddPage();
        coverPage.Size = PdfSharpCore.PageSize.A4;
        coverPage.Orientation = PdfSharpCore.PageOrientation.Portrait;
        var coverGraphics = XGraphics.FromPdfPage(coverPage);
        DrawCoverPage(coverGraphics, coverPage, start, end, now);

        // ---------- Summary Page ----------
        pageCount++;
        var summaryPage = document.AddPage();
        summaryPage.Size = PdfSharpCore.PageSize.A4;
        summaryPage.Orientation = PdfSharpCore.PageOrientation.Portrait;
        var summaryGraphics = XGraphics.FromPdfPage(summaryPage);
        var summaryY = DrawSummaryPage(summaryGraphics, summaryPage, items, pageCount);
        DrawFooter(summaryGraphics, summaryPage, now, pageCount);

        // ---------- Detail Table ----------
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

        var detailPage = document.AddPage();
        detailPage.Size = PdfSharpCore.PageSize.A4;
        detailPage.Orientation = PdfSharpCore.PageOrientation.Landscape;
        var detailGraphics = XGraphics.FromPdfPage(detailPage);
        var margin = 32d;
        var y = margin;

        detailGraphics.DrawString("弱點明細", titleFont, XBrushes.Black, new XRect(margin, y, detailPage.Width - margin * 2, 28), XStringFormats.TopLeft);
        y += 30;

        DrawTableHeader(detailGraphics, headerFont, margin, y, columns);
        y += 20;

        foreach (var item in items.OrderByDescending(item => item.LastDetectedAt))
        {
            if (y > detailPage.Height - 48)
            {
                DrawFooter(detailGraphics, detailPage, now, pageCount);
                pageCount++;
                detailPage = document.AddPage();
                detailPage.Size = PdfSharpCore.PageSize.A4;
                detailPage.Orientation = PdfSharpCore.PageOrientation.Landscape;
                detailGraphics = XGraphics.FromPdfPage(detailPage);
                y = margin + 10;
                DrawTableHeader(detailGraphics, headerFont, margin, y, columns);
                y += 20;
            }

            DrawTableRow(detailGraphics, bodyFont, smallFont, margin, y, columns, new[]
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

        DrawFooter(detailGraphics, detailPage, now, pageCount);

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

    private static void DrawCoverPage(XGraphics graphics, PdfPage page, DateTime start, DateTime end, DateTime now)
    {
        var margin = 48d;
        var centerX = page.Width / 2;

        graphics.DrawRectangle(new XPen(XColor.FromArgb(52, 73, 94), 0), XBrushes.White, 0, 0, page.Width, page.Height);

        var accentColor = XColor.FromArgb(41, 128, 185);
        graphics.DrawRectangle(new XSolidBrush(accentColor), 0, page.Height * 0.35, page.Width, 4);

        var titleFont = new XFont(PdfFontResolver.DefaultFontName, 28, XFontStyle.Bold);
        var subtitleFont = new XFont(PdfFontResolver.DefaultFontName, 16, XFontStyle.Regular);
        var infoFont = new XFont(PdfFontResolver.DefaultFontName, 11, XFontStyle.Regular);

        graphics.DrawString("弱點管理報告", titleFont, new XSolidBrush(accentColor), new XRect(margin, page.Height * 0.20, page.Width - margin * 2, 40), XStringFormats.TopCenter);
        graphics.DrawString("Vulnerability Management Report", subtitleFont, XBrushes.DimGray, new XRect(margin, page.Height * 0.20 + 44, page.Width - margin * 2, 24), XStringFormats.TopCenter);

        graphics.DrawRectangle(new XSolidBrush(accentColor), 0, page.Height * 0.35, page.Width, 4);

        graphics.DrawString($"報表期間：{start:yyyy-MM-dd} 至 {end:yyyy-MM-dd}", infoFont, XBrushes.Black, new XRect(margin, page.Height * 0.42, 300, 20), XStringFormats.TopLeft);
        graphics.DrawString($"產出時間：{now:yyyy-MM-dd HH:mm:ss}", infoFont, XBrushes.Black, new XRect(margin, page.Height * 0.42 + 24, 300, 20), XStringFormats.TopLeft);

        graphics.DrawString("ISO/IEC 27001 資訊安全管理系統", infoFont, XBrushes.DimGray, new XRect(margin, page.Height * 0.70, page.Width - margin * 2, 20), XStringFormats.TopCenter);
        graphics.DrawString("VulnScan.Web", new XFont(PdfFontResolver.DefaultFontName, 10, XFontStyle.Regular), XBrushes.Gray, new XRect(margin, page.Height * 0.70 + 20, page.Width - margin * 2, 20), XStringFormats.TopCenter);
    }

    private static double DrawSummaryPage(XGraphics graphics, PdfPage page, IReadOnlyList<Vulnerability> items, int pageNumber)
    {
        var margin = 32d;
        var y = margin;
        var titleFont = new XFont(PdfFontResolver.DefaultFontName, 18, XFontStyle.Bold);
        var headerFont = new XFont(PdfFontResolver.DefaultFontName, 10, XFontStyle.Bold);
        var bodyFont = new XFont(PdfFontResolver.DefaultFontName, 9, XFontStyle.Regular);

        graphics.DrawString("摘要統計", titleFont, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, 28), XStringFormats.TopLeft);
        y += 32;

        var vulnerabilityCount = items.Count;
        var highRiskCount = items.Count(item => item.Severity is "Critical" or "High");
        var assetsCount = items.Select(item => item.AssetId).Distinct().Count();
        DrawSummaryCard(graphics, margin, y, (page.Width - margin * 2 - 16) / 3, 58, "弱點總數", vulnerabilityCount.ToString());
        DrawSummaryCard(graphics, margin + (page.Width - margin * 2 - 16) / 3 + 8, y, (page.Width - margin * 2 - 16) / 3, 58, "高風險弱點", highRiskCount.ToString());
        DrawSummaryCard(graphics, margin + (page.Width - margin * 2 - 16) / 3 * 2 + 16, y, (page.Width - margin * 2 - 16) / 3, 58, "受影響資產", assetsCount.ToString());
        y += 72;

        // --- Severity Distribution ---
        graphics.DrawString("風險等級分佈", headerFont, XBrushes.Black, new XRect(margin, y, 200, 20), XStringFormats.TopLeft);
        y += 24;

        var severityOrder = new[] { "Critical", "High", "Medium", "Low", "Info" };
        var severityColors = new Dictionary<string, XColor>
        {
            ["Critical"] = XColor.FromArgb(192, 57, 43),
            ["High"] = XColor.FromArgb(231, 76, 60),
            ["Medium"] = XColor.FromArgb(243, 156, 18),
            ["Low"] = XColor.FromArgb(46, 204, 113),
            ["Info"] = XColor.FromArgb(149, 165, 166),
        };

        foreach (var severity in severityOrder)
        {
            var count = items.Count(item => item.Severity == severity);
            var barWidth = vulnerabilityCount > 0 ? (double)count / vulnerabilityCount * 300 : 0;
            var color = severityColors.GetValueOrDefault(severity, XColor.FromArgb(149, 165, 166));

            graphics.DrawString(severity, bodyFont, XBrushes.Black, new XRect(margin, y, 60, 16), XStringFormats.TopLeft);
            graphics.DrawRectangle(new XSolidBrush(color), margin + 64, y + 2, barWidth, 12);
            graphics.DrawString(count.ToString(), bodyFont, XBrushes.Black, new XRect(margin + 370, y, 40, 16), XStringFormats.TopLeft);
            y += 18;
        }

        y += 12;

        // --- Status Distribution ---
        graphics.DrawString("處理狀態分佈", headerFont, XBrushes.Black, new XRect(margin, y, 200, 20), XStringFormats.TopLeft);
        y += 24;

        var statusGroups = items.GroupBy(item => item.Status ?? "未處理")
            .OrderByDescending(g => g.Count());
        foreach (var group in statusGroups)
        {
            var count = group.Count();
            var barWidth = vulnerabilityCount > 0 ? (double)count / vulnerabilityCount * 300 : 0;
            graphics.DrawString(group.Key, bodyFont, XBrushes.Black, new XRect(margin, y, 60, 16), XStringFormats.TopLeft);
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(52, 152, 219)), margin + 64, y + 2, barWidth, 12);
            graphics.DrawString(count.ToString(), bodyFont, XBrushes.Black, new XRect(margin + 370, y, 40, 16), XStringFormats.TopLeft);
            y += 18;
        }

        y += 12;

        // --- Top Affected Assets ---
        graphics.DrawString("前十大受影響資產", headerFont, XBrushes.Black, new XRect(margin, y, 200, 20), XStringFormats.TopLeft);
        y += 24;

        var topAssets = items.Where(item => item.Asset != null)
            .GroupBy(item => item.Asset!.AssetName)
            .OrderByDescending(g => g.Count())
            .Take(10);

        foreach (var group in topAssets)
        {
            graphics.DrawString(group.Key, bodyFont, XBrushes.Black, new XRect(margin, y, 200, 16), XStringFormats.TopLeft);
            graphics.DrawString(group.Count().ToString(), bodyFont, XBrushes.Black, new XRect(margin + 280, y, 40, 16), XStringFormats.TopLeft);
            y += 18;
        }

        return y;
    }

    private static void DrawFooter(XGraphics graphics, PdfPage page, DateTime now, int pageNumber)
    {
        var footerFont = new XFont(PdfFontResolver.DefaultFontName, 7, XFontStyle.Regular);
        var footerY = page.Height - 20;
        graphics.DrawString($"VulnScan.Web | {now:yyyy-MM-dd HH:mm}", footerFont, XBrushes.Gray, new XRect(32, footerY, 300, 12), XStringFormats.TopLeft);
        graphics.DrawString($"第 {pageNumber} 頁", footerFont, XBrushes.Gray, new XRect(page.Width - 100, footerY, 64, 12), XStringFormats.TopRight);
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
