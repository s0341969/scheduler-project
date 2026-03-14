namespace OrgChartSystem.Contracts;

public class BackupFileDto
{
    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastWriteTimeUtc { get; set; }
}

public class RestoreBackupRequest
{
    public string FileName { get; set; } = string.Empty;
}
