namespace VulnScan.Web.Models;

public sealed class LocalAuthOptions
{
    public const string SectionName = "LocalAuth";

    public string SharedPassword { get; set; } = "ChangeThisPassword!";
}
