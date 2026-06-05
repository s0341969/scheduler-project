using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public sealed class GreenboneSettingsService(
    ApplicationDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<GreenboneOptions> fallbackOptions,
    IAuditLogService auditLogService) : IGreenboneSettingsService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("VulnScan.Web.Greenbone.Password");
    private readonly GreenboneOptions _fallbackOptions = fallbackOptions.Value;

    public async Task<GreenboneOptions> GetEffectiveOptionsAsync(CancellationToken cancellationToken = default)
    {
        var stored = await dbContext.Set<GreenboneIntegrationSetting>()
            .AsNoTracking()
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (stored is null)
        {
            return CloneOptions(_fallbackOptions);
        }

        return new GreenboneOptions
        {
            Enabled = stored.IsEnabled,
            Host = stored.Host,
            Port = stored.Port,
            Username = stored.Username,
            Password = Unprotect(stored.ProtectedPassword),
            IgnoreCertificateErrors = stored.IgnoreCertificateErrors,
            SyncTopReports = stored.SyncTopReports,
            ReportFilter = stored.ReportFilter,
            ResultFilter = stored.ResultFilter,
        };
    }

    public async Task<GreenboneSettingsFormViewModel> GetFormAsync(CancellationToken cancellationToken = default)
    {
        var stored = await dbContext.Set<GreenboneIntegrationSetting>()
            .AsNoTracking()
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (stored is null)
        {
            return new GreenboneSettingsFormViewModel
            {
                IsEnabled = _fallbackOptions.Enabled,
                Host = _fallbackOptions.Host,
                Port = _fallbackOptions.Port,
                Username = _fallbackOptions.Username,
                IgnoreCertificateErrors = _fallbackOptions.IgnoreCertificateErrors,
                SyncTopReports = _fallbackOptions.SyncTopReports,
                ReportFilter = _fallbackOptions.ReportFilter,
                ResultFilter = _fallbackOptions.ResultFilter,
                HasStoredPassword = !string.IsNullOrWhiteSpace(_fallbackOptions.Password),
            };
        }

        return new GreenboneSettingsFormViewModel
        {
            IsEnabled = stored.IsEnabled,
            Host = stored.Host,
            Port = stored.Port,
            Username = stored.Username,
            IgnoreCertificateErrors = stored.IgnoreCertificateErrors,
            SyncTopReports = stored.SyncTopReports,
            ReportFilter = stored.ReportFilter,
            ResultFilter = stored.ResultFilter,
            HasStoredPassword = !string.IsNullOrWhiteSpace(stored.ProtectedPassword),
        };
    }

    public async Task SaveAsync(GreenboneSettingsFormViewModel form, string updatedBy, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.Set<GreenboneIntegrationSetting>()
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting is null)
        {
            setting = new GreenboneIntegrationSetting();
            dbContext.Add(setting);
        }

        setting.IsEnabled = form.IsEnabled;
        setting.Host = form.Host.Trim();
        setting.Port = form.Port;
        setting.Username = form.Username.Trim();
        setting.IgnoreCertificateErrors = form.IgnoreCertificateErrors;
        setting.SyncTopReports = form.SyncTopReports;
        setting.ReportFilter = form.ReportFilter.Trim();
        setting.ResultFilter = form.ResultFilter.Trim();
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = updatedBy;

        if (!string.IsNullOrWhiteSpace(form.Password))
        {
            setting.ProtectedPassword = _protector.Protect(form.Password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "GreenboneSettingsSaved",
            nameof(GreenboneIntegrationSetting),
            setting.SettingId,
            $"更新 Greenbone 設定：{setting.Host}:{setting.Port} / {setting.Username}",
            updatedBy,
            null,
            cancellationToken);
    }

    private string Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            return _protector.Unprotect(value);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static GreenboneOptions CloneOptions(GreenboneOptions options)
    {
        return new GreenboneOptions
        {
            Enabled = options.Enabled,
            Host = options.Host,
            Port = options.Port,
            Username = options.Username,
            Password = options.Password,
            IgnoreCertificateErrors = options.IgnoreCertificateErrors,
            SyncTopReports = options.SyncTopReports,
            ReportFilter = options.ReportFilter,
            ResultFilter = options.ResultFilter,
        };
    }
}
