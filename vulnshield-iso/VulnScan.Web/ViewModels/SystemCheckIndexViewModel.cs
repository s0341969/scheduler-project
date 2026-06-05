namespace VulnScan.Web.ViewModels;

public sealed class SystemCheckIndexViewModel
{
    public required NmapCheckViewModel Nmap { get; init; }

    public required GreenboneCheckViewModel Greenbone { get; init; }

    public required DatabaseCheckViewModel ActiveDatabase { get; init; }

    public required DatabaseCheckViewModel Sqlite { get; init; }

    public required DatabaseCheckViewModel MsSql { get; init; }
}

public sealed class NmapCheckViewModel
{
    public bool IsInstalled { get; init; }

    public string StatusText { get; init; } = string.Empty;

    public string ResolvedPath { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

public sealed class GreenboneCheckViewModel
{
    public bool IsConfigured { get; init; }

    public bool IsEnabled { get; init; }

    public string Endpoint { get; init; } = string.Empty;

    public string Account { get; init; } = string.Empty;

    public string StatusText { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

public sealed class DatabaseCheckViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string StatusText { get; init; } = string.Empty;

    public bool IsConfigured { get; init; }

    public bool IsActiveProvider { get; init; }

    public bool? CanConnect { get; init; }

    public string Target { get; init; } = string.Empty;

    public string Detail { get; init; } = string.Empty;
}
