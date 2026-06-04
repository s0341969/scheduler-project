using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class NmapXmlParserService(ApplicationDbContext dbContext) : INmapXmlParserService
{
    public async Task ParseAndSaveAsync(int runId, string xmlPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(xmlPath))
        {
            throw new FileNotFoundException("找不到 Nmap XML 結果檔。", xmlPath);
        }

        var document = XDocument.Load(xmlPath);
        var run = await dbContext.ScanRuns.FirstOrDefaultAsync(item => item.RunId == runId, cancellationToken)
                  ?? throw new InvalidOperationException($"找不到 ScanRun {runId}。");

        var hostElements = document.Descendants("host").ToList();
        var portCount = 0;

        foreach (var hostElement in hostElements)
        {
            var ipAddress = hostElement.Descendants("address").FirstOrDefault()?.Attribute("addr")?.Value;
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                continue;
            }

            var asset = await dbContext.Assets.FirstOrDefaultAsync(item => item.IPAddress == ipAddress, cancellationToken);
            if (asset is null)
            {
                asset = new Asset
                {
                    AssetName = ipAddress,
                    IPAddress = ipAddress,
                    AssetType = "Other",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                dbContext.Assets.Add(asset);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var portElement in hostElement.Descendants("port"))
            {
                var state = portElement.Element("state")?.Attribute("state")?.Value;
                if (!string.Equals(state, "open", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dbContext.AssetPorts.Add(new AssetPort
                {
                    AssetId = asset.AssetId,
                    RunId = runId,
                    IPAddress = ipAddress,
                    PortNumber = int.TryParse(portElement.Attribute("portid")?.Value, out var portNumber) ? portNumber : null,
                    Protocol = portElement.Attribute("protocol")?.Value,
                    ServiceName = portElement.Element("service")?.Attribute("name")?.Value,
                    ServiceProduct = portElement.Element("service")?.Attribute("product")?.Value,
                    ServiceVersion = portElement.Element("service")?.Attribute("version")?.Value,
                    State = state,
                    DetectedAt = DateTime.UtcNow,
                });
                portCount += 1;
            }

            asset.LastScanTime = DateTime.UtcNow;
        }

        run.TotalHosts = hostElements.Count;
        run.TotalOpenPorts = portCount;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
