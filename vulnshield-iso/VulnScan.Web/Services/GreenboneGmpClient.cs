using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public sealed class GreenboneGmpClient(
    ILogger<GreenboneGmpClient> logger) : IGreenboneGmpClient
{
    public async Task<IReadOnlyList<GreenboneReportSummary>> GetRecentReportsAsync(GreenboneOptions options, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Host))
        {
            return Array.Empty<GreenboneReportSummary>();
        }

        using var session = await OpenSessionAsync(options, cancellationToken);
        var request = new XElement(
            "get_reports",
            new XAttribute("details", "0"),
            new XAttribute("filter", options.ReportFilter));

        var response = await session.SendCommandAsync(request, cancellationToken);
        EnsureSuccess(response, "查詢 Greenbone 報表清單失敗");

        return response.Elements("report")
            .Select(report => new GreenboneReportSummary
            {
                ReportId = report.Attribute("id")?.Value ?? string.Empty,
                TaskName = report.Element("task")?.Element("name")?.Value ?? "(unknown task)",
                ScanStatus = report.Element("task")?.Element("status")?.Value
                    ?? report.Element("scan_end")?.Value
                    ?? "Unknown",
                ScanDate = ParseDateTime(report.Element("creation_time")?.Value)
                    ?? ParseDateTime(report.Element("date")?.Value),
                Severity = report.Element("severity")?.Element("full")?.Value
                    ?? report.Element("severity")?.Value
                    ?? string.Empty,
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.ReportId))
            .Take(Math.Max(1, options.SyncTopReports))
            .ToList();
    }

    public async Task<string> DownloadReportXmlAsync(GreenboneOptions options, string reportId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportId))
        {
            throw new ArgumentException("reportId is required.", nameof(reportId));
        }

        using var session = await OpenSessionAsync(options, cancellationToken);
        var request = new XElement(
            "get_reports",
            new XAttribute("report_id", reportId),
            new XAttribute("details", "1"),
            new XAttribute("ignore_pagination", "1"),
            new XAttribute("filter", options.ResultFilter));

        var response = await session.SendCommandAsync(request, cancellationToken);
        EnsureSuccess(response, $"下載 Greenbone 報表失敗：{reportId}");

        var reportWrapper = response.Element("report");
        if (reportWrapper is null)
        {
            throw new InvalidOperationException($"Greenbone report {reportId} response did not contain a report element.");
        }

        if (string.Equals(reportWrapper.Attribute("content_type")?.Value, "text/xml", StringComparison.OrdinalIgnoreCase))
        {
            var nestedReport = reportWrapper.Element("report");
            if (nestedReport is not null)
            {
                return nestedReport.ToString(SaveOptions.DisableFormatting);
            }

            return reportWrapper.ToString(SaveOptions.DisableFormatting);
        }

        var payload = reportWrapper.Value?.Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException($"Greenbone report {reportId} returned an empty payload.");
        }

        try
        {
            var decoded = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(decoded);
        }
        catch (FormatException exception)
        {
            logger.LogError(exception, "Failed to decode Greenbone report {ReportId} as Base64.", reportId);
            throw;
        }
    }

    private async Task<GmpSession> OpenSessionAsync(GreenboneOptions options, CancellationToken cancellationToken)
    {
        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(options.Host, options.Port, cancellationToken);

        var sslStream = new SslStream(
            tcpClient.GetStream(),
            leaveInnerStreamOpen: false,
            (sender, certificate, chain, errors) => options.IgnoreCertificateErrors || errors == SslPolicyErrors.None);

        await sslStream.AuthenticateAsClientAsync(
            new SslClientAuthenticationOptions
            {
                TargetHost = options.Host,
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            },
            cancellationToken);

        var session = new GmpSession(tcpClient, sslStream);
        var authRequest = new XElement(
            "authenticate",
            new XElement(
                "credentials",
                new XElement("username", options.Username),
                new XElement("password", options.Password)));

        var authResponse = await session.SendCommandAsync(authRequest, cancellationToken);
        EnsureSuccess(authResponse, "Greenbone GMP 驗證失敗");
        return session;
    }

    private static void EnsureSuccess(XElement response, string errorPrefix)
    {
        var status = response.Attribute("status")?.Value;
        if (status is not null && status.StartsWith("2", StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException($"{errorPrefix}：{response.Attribute("status_text")?.Value ?? response.ToString(SaveOptions.DisableFormatting)}");
    }

    private static DateTime? ParseDateTime(string? value)
    {
        return DateTime.TryParse(value, out var parsedValue) ? parsedValue : null;
    }

    private sealed class GmpSession(TcpClient tcpClient, SslStream sslStream) : IDisposable
    {
        private readonly XmlReader _reader = XmlReader.Create(
            sslStream,
            new XmlReaderSettings
            {
                Async = false,
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
            });

        public async Task<XElement> SendCommandAsync(XElement request, CancellationToken cancellationToken)
        {
            var payload = request.ToString(SaveOptions.DisableFormatting) + "\n";
            var bytes = Encoding.UTF8.GetBytes(payload);
            await sslStream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken);
            await sslStream.FlushAsync(cancellationToken);
            return await ReadElementAsync(cancellationToken);
        }

        private Task<XElement> ReadElementAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.Element)
                {
                    return Task.FromResult(XElement.Load(_reader.ReadSubtree()));
                }
            }

            throw new InvalidOperationException("Greenbone GMP connection closed without a complete XML response.");
        }

        public void Dispose()
        {
            _reader.Dispose();
            sslStream.Dispose();
            tcpClient.Dispose();
        }
    }
}
