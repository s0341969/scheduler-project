namespace VulnScan.Web.Models;

public sealed class LocalAuthOptions
{
    public const string SectionName = "LocalAuth";

    public IList<BootstrapUserOptions> BootstrapUsers { get; set; } = new List<BootstrapUserOptions>();
}

public sealed class BootstrapUserOptions
{
    public string Account { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string RoleName { get; set; } = "Viewer";

    public string Password { get; set; } = string.Empty;
}
