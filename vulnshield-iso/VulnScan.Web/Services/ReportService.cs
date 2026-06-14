using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class ReportService(
    ApplicationDbContext dbContext,
    IAuditLogService auditLogService,
    IOptions<VulnScanOptions> options) : IReportService
{
    private const string FontName = "Microsoft YaHei";

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
        var now = DateTime.UtcNow.ToLocalTime();

        var vulnerabilityCount = items.Count;
        var highRiskCount = items.Count(item => item.Severity is "Critical" or "High");
        var assetsCount = items.Select(item => item.AssetId).Distinct().Count();

        var pageNumber = 0;

        Document.Create(container =>
        {
            var orderedItems = items.OrderByDescending(item => item.LastDetectedAt).ToList();

            // ---------- Cover Page ----------
            pageNumber++;
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(48);
                page.DefaultTextStyle(x => x.FontFamily(FontName).FontSize(11));

                page.Content().Column(col =>
                {
            col.Item().Height(170);

            col.Item().AlignCenter().Text("弱點管理報告")
                        .FontSize(28).Bold().FontColor("#2980B9");
                    col.Item().AlignCenter().Text("Vulnerability Management Report")
                        .FontSize(16).FontColor(Colors.Grey.Darken3);

                    col.Item().Height(30);
                    col.Item().Background("#2980B9").Height(4).ExtendHorizontal();

                    col.Item().Height(40);
                    col.Item().Text($"報表期間：{start:yyyy-MM-dd} 至 {end:yyyy-MM-dd}");
                    col.Item().Text($"產出時間：{now:yyyy-MM-dd HH:mm:ss}");

                    col.Item().PaddingTop(120);
                    col.Item().AlignCenter().Text("ISO/IEC 27001 資訊安全管理系統")
                        .FontColor(Colors.Grey.Darken3);
                    col.Item().AlignCenter().Text("VulnScan.Web")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            // ---------- Summary Page ----------
            pageNumber++;
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontFamily(FontName));

                page.Content().Column(col =>
                {
                    col.Item().Text("摘要統計").FontSize(18).Bold();
                    col.Item().Height(16);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().CardStyle()
                            .Column(card =>
                            {
                                card.Item().Text("弱點總數").FontSize(9).FontColor(Colors.Grey.Darken3);
                                card.Item().Text(vulnerabilityCount.ToString()).FontSize(16).Bold();
                            });
                        row.ConstantItem(8);
                        row.RelativeItem().CardStyle()
                            .Column(card =>
                            {
                                card.Item().Text("高風險弱點").FontSize(9).FontColor(Colors.Grey.Darken3);
                                card.Item().Text(highRiskCount.ToString()).FontSize(16).Bold();
                            });
                        row.ConstantItem(8);
                        row.RelativeItem().CardStyle()
                            .Column(card =>
                            {
                                card.Item().Text("受影響資產").FontSize(9).FontColor(Colors.Grey.Darken3);
                                card.Item().Text(assetsCount.ToString()).FontSize(16).Bold();
                            });
                    });

                    col.Item().Height(20);

                    // --- Severity Distribution ---
                    col.Item().Text("風險等級分佈").FontSize(10).Bold();
                    col.Item().Height(8);

                    var severityOrder = new[] { "Critical", "High", "Medium", "Low", "Info" };
                    var severityColors = new Dictionary<string, string>
                    {
                        ["Critical"] = "#C0392B",
                        ["High"] = "#E74C3C",
                        ["Medium"] = "#F39C12",
                        ["Low"] = "#2ECC71",
                        ["Info"] = "#95A5A6",
                    };

                    foreach (var severity in severityOrder)
                    {
                        var count = items.Count(item => item.Severity == severity);
                        var barRatio = vulnerabilityCount > 0 ? (double)count / vulnerabilityCount : 0;

                        col.Item().Row(row =>
                        {
                            row.ConstantItem(60).Text(severity).FontSize(9);
                            row.RelativeItem().AlignLeft().Background(severityColors[severity])
                                .Width((float)(barRatio * 300)).Height(12);
                            row.ConstantItem(40).PaddingLeft(4).Text(count.ToString()).FontSize(9);
                        });
                        col.Item().Height(2);
                    }

                    col.Item().Height(12);

                    // --- Status Distribution ---
                    col.Item().Text("處理狀態分佈").FontSize(10).Bold();
                    col.Item().Height(8);

                    var statusGroups = items.GroupBy(item => item.Status ?? "未處理")
                        .OrderByDescending(g => g.Count());
                    foreach (var group in statusGroups)
                    {
                        var count = group.Count();
                        var barRatio = vulnerabilityCount > 0 ? (double)count / vulnerabilityCount : 0;

                        col.Item().Row(row =>
                        {
                            row.ConstantItem(60).Text(group.Key).FontSize(9);
                            row.RelativeItem().AlignLeft().Background("#3498DB")
                                .Width((float)(barRatio * 300)).Height(12);
                            row.ConstantItem(40).PaddingLeft(4).Text(count.ToString()).FontSize(9);
                        });
                        col.Item().Height(2);
                    }

                    col.Item().Height(12);

                    // --- Top Affected Assets ---
                    col.Item().Text("前十大受影響資產").FontSize(10).Bold();
                    col.Item().Height(8);

                    var topAssets = items.Where(item => item.Asset != null)
                        .GroupBy(item => item.Asset!.AssetName)
                        .OrderByDescending(g => g.Count())
                        .Take(10);

                    foreach (var group in topAssets)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(group.Key).FontSize(9);
                            row.ConstantItem(50).Text(group.Count().ToString()).FontSize(9);
                        });
                        col.Item().Height(2);
                    }
                });

                page.Footer().AlignCenter().Text($"VulnScan.Web | {now:yyyy-MM-dd HH:mm} | 第 {pageNumber} 頁")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
            });

            // ---------- Detail Table ----------
            pageNumber++;
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontFamily(FontName));

                page.Content().Column(col =>
                {
                    col.Item().Text("弱點明細").FontSize(18).Bold();
                    col.Item().Height(12);

                    col.Item().Table(table =>
                    {
                        var columns = new (string Header, float Width)[]
                        {
                            ("資產", 55),
                            ("IP", 65),
                            ("弱點名稱", 200),
                            ("Severity", 55),
                            ("CVSS", 40),
                            ("軟體版本", 70),
                            ("特徵碼版本", 70),
                            ("最後發現", 70),
                        };

                        table.ColumnsDefinition(columnsDef =>
                        {
                            foreach (var (_, w) in columns)
                                columnsDef.ConstantColumn(w);
                        });

                        table.Header(header =>
                        {
                            foreach (var (name, _) in columns)
                            {
                                header.Cell()
                                    .Background(Colors.Grey.Lighten2)
                                    .Padding(4)
                                    .Text(name).FontSize(9).Bold();
                            }
                        });

                        var borderColor = "#DCE1EB";
                        foreach (var item in orderedItems)
                        {
                            var values = new[]
                            {
                                item.Asset?.AssetName ?? "-",
                                item.IPAddress ?? "-",
                                item.VulnName,
                                item.Severity ?? "-",
                                item.CVSS?.ToString("0.0") ?? "-",
                                item.DetectedVersion ?? item.ServiceName ?? "-",
                                item.SignatureVersion ?? "-",
                                item.LastDetectedAt.ToLocalTime().ToString("yyyy-MM-dd"),
                            };

                            foreach (var value in values)
                            {
                                table.Cell()
                                    .Border(0.5f).BorderColor(borderColor)
                                    .Padding(3)
                                    .Text(value).FontSize(8);
                            }
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"VulnScan.Web | {now:yyyy-MM-dd HH:mm} | 第 {pageNumber} 頁")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf(filePath);

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
}

internal static class ReportContainerExtensions
{
    public static IContainer CardStyle(this IContainer container)
    {
        return container
            .Shrink()
            .Border(1)
            .BorderColor("#D0DCED")
            .Background("#F5F5F5")
            .Padding(12);
    }
}
