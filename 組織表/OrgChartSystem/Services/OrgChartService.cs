using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrgChartSystem.Contracts;
using OrgChartSystem.Data;
using OrgChartSystem.Models;

namespace OrgChartSystem.Services;

public class OrgChartService(AppDbContext db, IConfiguration config)
{
    private static readonly HashSet<string> AllowedDisplayModes = ["person", "code"];
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<OrgChartResponse> GetChartAsync()
    {
        var settings = await GetOrCreateSettingAsync();
        var nodes = await db.OrgNodes
            .AsNoTracking()
            .ToListAsync();

        return new OrgChartResponse
        {
            DisplayMode = settings.DisplayMode,
            Nodes = BuildTree(nodes)
        };
    }

    public async Task<List<SearchResultDto>> SearchAsync(string query, int limit = 50)
    {
        var keyword = query?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        var normalizedLimit = Math.Clamp(limit, 1, 200);
        var nodes = await db.OrgNodes
            .AsNoTracking()
            .ToListAsync();

        var nodeById = nodes.ToDictionary(x => x.Id);

        var matched = nodes
            .Where(x =>
                ContainsIgnoreCase(x.Code, keyword) ||
                ContainsIgnoreCase(x.DepartmentName, keyword) ||
                ContainsIgnoreCase(x.PersonName, keyword) ||
                ContainsIgnoreCase(x.Title, keyword) ||
                ContainsIgnoreCase(x.Email, keyword) ||
                ContainsIgnoreCase(x.Phone, keyword))
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Take(normalizedLimit)
            .Select(x => new SearchResultDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Code = x.Code,
                DepartmentName = x.DepartmentName,
                PersonName = x.PersonName,
                Title = x.Title,
                Path = BuildPath(x, nodeById)
            })
            .ToList();

        return matched;
    }

    public async Task<ImportPreviewResponse> PreviewImportAsync(ImportOrgChartRequest request)
    {
        if (request.Nodes.Count == 0)
        {
            throw new InvalidOperationException("匯入內容不可為空。請至少提供一個根節點。");
        }

        var mode = NormalizeDisplayMode(request.DisplayMode);
        var currentCount = await db.OrgNodes.CountAsync();

        var incomingNodeCount = 0;
        var maxDepth = 0;

        for (var i = 0; i < request.Nodes.Count; i++)
        {
            ValidateImportNodeTree(request.Nodes[i], 1, ref incomingNodeCount, ref maxDepth);
        }

        var warnings = new List<string>();
        if (incomingNodeCount < currentCount)
        {
            warnings.Add("匯入後節點數量較目前少，可能會刪除部分資料。");
        }

        if (maxDepth >= 8)
        {
            warnings.Add("新組織層級較深，建議先確認拖拉與顯示可讀性。");
        }

        if (mode == "code")
        {
            warnings.Add("匯入後顯示模式將切換為代碼視圖。");
        }

        return new ImportPreviewResponse
        {
            CurrentNodeCount = currentCount,
            IncomingNodeCount = incomingNodeCount,
            RootCount = request.Nodes.Count,
            MaxDepth = maxDepth,
            Warnings = warnings
        };
    }

    public async Task<OrgNodeDto> CreateNodeAsync(NodeMutationRequest request, string actor = "system", string role = "editor")
    {
        ValidateNodeMutation(request);
        await ValidateParentAsync(request.ParentId, null);

        var newSortOrder = await GetNextSortOrderAsync(request.ParentId);
        var node = new OrgNode
        {
            ParentId = request.ParentId,
            Code = NormalizeText(request.Code, 64),
            DepartmentName = NormalizeText(request.DepartmentName, 128),
            PersonName = NormalizeText(request.PersonName, 64),
            Title = NormalizeText(request.Title, 64),
            Email = NormalizeText(request.Email, 128),
            Phone = NormalizeText(request.Phone, 32),
            SortOrder = newSortOrder,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.OrgNodes.Add(node);
        await db.SaveChangesAsync();

        await LogAuditAsync("node.create", node.Id, $"新增節點：{node.DepartmentName}/{node.PersonName}", actor, role);
        return Map(node);
    }

    public async Task<OrgNodeDto?> UpdateNodeAsync(int nodeId, NodeMutationRequest request, string actor = "system", string role = "editor")
    {
        ValidateNodeMutation(request);

        var node = await db.OrgNodes.FirstOrDefaultAsync(x => x.Id == nodeId);
        if (node is null)
        {
            return null;
        }

        await ValidateParentAsync(request.ParentId, nodeId);

        if (node.ParentId != request.ParentId)
        {
            await MoveToNewParentAsync(node, request.ParentId);
        }

        node.Code = NormalizeText(request.Code, 64);
        node.DepartmentName = NormalizeText(request.DepartmentName, 128);
        node.PersonName = NormalizeText(request.PersonName, 64);
        node.Title = NormalizeText(request.Title, 64);
        node.Email = NormalizeText(request.Email, 128);
        node.Phone = NormalizeText(request.Phone, 32);
        node.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await LogAuditAsync("node.update", node.Id, $"更新節點：{node.DepartmentName}/{node.PersonName}", actor, role);

        return Map(node);
    }

    public async Task<bool> DeleteNodeAsync(int nodeId, string actor = "system", string role = "editor")
    {
        var nodes = await db.OrgNodes.ToListAsync();
        var target = nodes.FirstOrDefault(x => x.Id == nodeId);
        if (target is null)
        {
            return false;
        }

        var deleteSet = CollectDescendantIds(nodeId, nodes).ToHashSet();
        var siblings = nodes
            .Where(x => x.ParentId == target.ParentId && !deleteSet.Contains(x.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToList();

        ResequenceSiblings(siblings);

        db.OrgNodes.RemoveRange(nodes.Where(x => deleteSet.Contains(x.Id)));
        await db.SaveChangesAsync();
        await LogAuditAsync("node.delete", nodeId, $"刪除節點及子孫共 {deleteSet.Count} 筆", actor, role);

        return true;
    }

    public async Task<bool> MoveNodeAsync(int nodeId, string direction, string actor = "system", string role = "editor")
    {
        var normalizedDirection = direction.Trim().ToLowerInvariant();
        if (normalizedDirection is not ("up" or "down"))
        {
            throw new InvalidOperationException("方向必須是 up 或 down。");
        }

        var node = await db.OrgNodes.FirstOrDefaultAsync(x => x.Id == nodeId);
        if (node is null)
        {
            return false;
        }

        var siblings = await db.OrgNodes
            .Where(x => x.ParentId == node.ParentId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        var index = siblings.FindIndex(x => x.Id == nodeId);
        if (index < 0)
        {
            return false;
        }

        var targetIndex = normalizedDirection == "up" ? index - 1 : index + 1;
        if (targetIndex < 0 || targetIndex >= siblings.Count)
        {
            return true;
        }

        var swapNode = siblings[targetIndex];
        (node.SortOrder, swapNode.SortOrder) = (swapNode.SortOrder, node.SortOrder);
        node.UpdatedAtUtc = DateTime.UtcNow;
        swapNode.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await LogAuditAsync("node.move", nodeId, $"方向：{normalizedDirection}", actor, role);

        return true;
    }

    public async Task<bool> RepositionNodeAsync(int nodeId, int? newParentId, int? newIndex, string actor = "system", string role = "editor")
    {
        var node = await db.OrgNodes.FirstOrDefaultAsync(x => x.Id == nodeId);
        if (node is null)
        {
            return false;
        }

        await ValidateParentAsync(newParentId, nodeId);

        var oldParentId = node.ParentId;
        var sameParent = oldParentId == newParentId;

        if (sameParent)
        {
            var siblings = await db.OrgNodes
                .Where(x => x.ParentId == oldParentId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var currentIndex = siblings.FindIndex(x => x.Id == nodeId);
            if (currentIndex < 0)
            {
                return false;
            }

            var others = siblings.Where(x => x.Id != nodeId).ToList();
            var requestedIndex = Math.Max(0, newIndex ?? others.Count);
            var insertIndex = Math.Min(requestedIndex, others.Count);

            if (requestedIndex > currentIndex)
            {
                insertIndex = Math.Max(0, insertIndex - 1);
            }

            others.Insert(insertIndex, node);
            ResequenceSiblings(others);
        }
        else
        {
            var oldSiblings = await db.OrgNodes
                .Where(x => x.ParentId == oldParentId && x.Id != nodeId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var newSiblings = await db.OrgNodes
                .Where(x => x.ParentId == newParentId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var insertIndex = Math.Min(Math.Max(0, newIndex ?? newSiblings.Count), newSiblings.Count);
            node.ParentId = newParentId;
            node.UpdatedAtUtc = DateTime.UtcNow;
            newSiblings.Insert(insertIndex, node);

            ResequenceSiblings(oldSiblings);
            ResequenceSiblings(newSiblings);
        }

        await db.SaveChangesAsync();
        await LogAuditAsync("node.reposition", nodeId, $"parentId={newParentId?.ToString() ?? "null"}, index={newIndex?.ToString() ?? "null"}", actor, role);
        return true;
    }

    public async Task<string> UpdateDisplayModeAsync(string displayMode, string actor = "system", string role = "editor")
    {
        var normalized = NormalizeDisplayMode(displayMode);
        var setting = await GetOrCreateSettingAsync();

        setting.DisplayMode = normalized;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await LogAuditAsync("settings.update", null, $"displayMode={normalized}", actor, role);

        return normalized;
    }

    public async Task ImportAsync(ImportOrgChartRequest request, string actor = "system", string role = "editor")
    {
        var preview = await PreviewImportAsync(request);
        _ = preview;

        await CreateSnapshotAsync("匯入前自動快照", actor);

        var mode = NormalizeDisplayMode(request.DisplayMode);
        var oldNodes = await db.OrgNodes.ToListAsync();

        db.OrgNodes.RemoveRange(oldNodes);

        var setting = await GetOrCreateSettingAsync();
        setting.DisplayMode = mode;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        var importedRoots = new List<OrgNode>();
        for (var i = 0; i < request.Nodes.Count; i++)
        {
            importedRoots.Add(BuildImportNode(request.Nodes[i], null, i));
        }

        db.OrgNodes.AddRange(importedRoots);
        await db.SaveChangesAsync();

        await LogAuditAsync("import.apply", null, $"匯入 {request.Nodes.Count} 個根節點", actor, role);
    }

    public async Task<List<SnapshotDto>> ListSnapshotsAsync(int take = 50)
    {
        var normalizedTake = Math.Clamp(take, 1, 200);

        return await db.OrgChartSnapshots
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(normalizedTake)
            .Select(x => new SnapshotDto
            {
                Id = x.Id,
                Reason = x.Reason,
                Actor = x.Actor,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<SnapshotDto> CreateSnapshotAsync(string reason, string actor = "system")
    {
        var chart = await GetChartAsync();

        var snapshot = new OrgChartSnapshot
        {
            Reason = NormalizeText(string.IsNullOrWhiteSpace(reason) ? "手動快照" : reason, 128),
            DataJson = JsonSerializer.Serialize(chart),
            Actor = NormalizeActor(actor),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.OrgChartSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        await LogAuditAsync("snapshot.create", null, $"快照#{snapshot.Id} {snapshot.Reason}", actor, "editor");

        return new SnapshotDto
        {
            Id = snapshot.Id,
            Reason = snapshot.Reason,
            Actor = snapshot.Actor,
            CreatedAtUtc = snapshot.CreatedAtUtc
        };
    }

    public async Task<bool> RestoreSnapshotAsync(int snapshotId, string actor = "system", string role = "editor")
    {
        var snapshot = await db.OrgChartSnapshots.FirstOrDefaultAsync(x => x.Id == snapshotId);
        if (snapshot is null)
        {
            return false;
        }

        var chart = JsonSerializer.Deserialize<OrgChartResponse>(snapshot.DataJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (chart is null || chart.Nodes.Count == 0)
        {
            throw new InvalidOperationException("快照資料無效，無法回復。");
        }

        await CreateSnapshotAsync($"回復快照#{snapshotId}前自動快照", actor);

        var request = new ImportOrgChartRequest
        {
            DisplayMode = chart.DisplayMode,
            Nodes = chart.Nodes.Select(MapToImportNode).ToList()
        };

        await ImportAsync(request, actor, role);
        await LogAuditAsync("snapshot.restore", null, $"回復快照#{snapshotId}", actor, role);

        return true;
    }

    public List<BackupFileDto> ListDatabaseBackups()
    {
        var directory = GetBackupDirectory();
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Directory
            .GetFiles(directory, "orgchart-*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .Take(100)
            .Select(x => new BackupFileDto
            {
                FileName = x.Name,
                SizeBytes = x.Length,
                LastWriteTimeUtc = x.LastWriteTimeUtc
            })
            .ToList();
    }

    public async Task<BackupFileDto> CreateDatabaseBackupAsync(string reason, string actor = "system", string role = "editor")
    {
        var dbPath = GetDatabasePath();
        if (!File.Exists(dbPath))
        {
            throw new InvalidOperationException("資料庫檔案不存在，無法建立備份。");
        }

        var directory = GetBackupDirectory();
        Directory.CreateDirectory(directory);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"orgchart-{stamp}.db";
        var destination = Path.Combine(directory, fileName);

        await db.Database.CloseConnectionAsync();
        File.Copy(dbPath, destination, overwrite: false);

        var info = new FileInfo(destination);

        await LogAuditAsync("backup.create", null, $"{fileName} ({NormalizeText(reason, 64)})", actor, role);

        return new BackupFileDto
        {
            FileName = fileName,
            SizeBytes = info.Length,
            LastWriteTimeUtc = info.LastWriteTimeUtc
        };
    }

    public async Task RestoreDatabaseBackupAsync(string fileName, string actor = "system", string role = "editor")
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("請提供備份檔名。");
        }

        var directory = GetBackupDirectory();
        var source = Path.Combine(directory, Path.GetFileName(fileName.Trim()));
        if (!File.Exists(source))
        {
            throw new InvalidOperationException("指定備份檔不存在。");
        }

        var dbPath = GetDatabasePath();

        await CreateDatabaseBackupAsync("還原前自動備份", actor, role);

        await db.Database.CloseConnectionAsync();
        File.Copy(source, dbPath, overwrite: true);

        await LogAuditAsync("backup.restore", null, fileName, actor, role);
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(int take = 100)
    {
        var normalizedTake = Math.Clamp(take, 1, 500);
        return await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(normalizedTake)
            .ToListAsync();
    }

    private async Task<OrgChartSetting> GetOrCreateSettingAsync()
    {
        var setting = await db.OrgChartSettings.FirstOrDefaultAsync(x => x.Id == 1);
        if (setting is not null)
        {
            return setting;
        }

        setting = new OrgChartSetting
        {
            Id = 1,
            DisplayMode = "person",
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.OrgChartSettings.Add(setting);
        await db.SaveChangesAsync();
        return setting;
    }

    private async Task ValidateParentAsync(int? parentId, int? selfId)
    {
        if (parentId is null)
        {
            return;
        }

        if (selfId == parentId)
        {
            throw new InvalidOperationException("節點不可設定自己為上層。");
        }

        var parentExists = await db.OrgNodes.AnyAsync(x => x.Id == parentId.Value);
        if (!parentExists)
        {
            throw new InvalidOperationException("指定的上層節點不存在。");
        }

        if (selfId is null)
        {
            return;
        }

        var nodes = await db.OrgNodes
            .AsNoTracking()
            .Select(x => new { x.Id, x.ParentId })
            .ToListAsync();

        var descendants = CollectDescendantIds(selfId.Value, nodes.Select(x => new OrgNode { Id = x.Id, ParentId = x.ParentId }).ToList());
        if (descendants.Contains(parentId.Value))
        {
            throw new InvalidOperationException("不可把節點移動到自己的下層。");
        }
    }

    private async Task MoveToNewParentAsync(OrgNode node, int? newParentId)
    {
        var oldParentId = node.ParentId;
        var oldSortOrder = node.SortOrder;

        var oldSiblings = await db.OrgNodes
            .Where(x => x.ParentId == oldParentId && x.SortOrder > oldSortOrder)
            .ToListAsync();

        foreach (var sibling in oldSiblings)
        {
            sibling.SortOrder -= 1;
            sibling.UpdatedAtUtc = DateTime.UtcNow;
        }

        node.ParentId = newParentId;
        node.SortOrder = await GetNextSortOrderAsync(newParentId);
        node.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task<int> GetNextSortOrderAsync(int? parentId)
    {
        var maxSort = await db.OrgNodes
            .Where(x => x.ParentId == parentId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync();

        return (maxSort ?? -1) + 1;
    }

    private static void ResequenceSiblings(List<OrgNode> siblings)
    {
        for (var i = 0; i < siblings.Count; i++)
        {
            siblings[i].SortOrder = i;
            siblings[i].UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private static OrgNode BuildImportNode(ImportOrgNodeRequest request, OrgNode? parent, int sortOrder)
    {
        var node = new OrgNode
        {
            Parent = parent,
            Code = NormalizeText(request.Code, 64),
            DepartmentName = NormalizeText(request.DepartmentName, 128),
            PersonName = NormalizeText(request.PersonName, 64),
            Title = NormalizeText(request.Title, 64),
            Email = NormalizeText(request.Email, 128),
            Phone = NormalizeText(request.Phone, 32),
            SortOrder = sortOrder,
            UpdatedAtUtc = DateTime.UtcNow
        };

        for (var i = 0; i < request.Children.Count; i++)
        {
            node.Children.Add(BuildImportNode(request.Children[i], node, i));
        }

        return node;
    }

    private static ImportOrgNodeRequest MapToImportNode(OrgNodeDto dto)
    {
        return new ImportOrgNodeRequest
        {
            Code = dto.Code,
            DepartmentName = dto.DepartmentName,
            PersonName = dto.PersonName,
            Title = dto.Title,
            Email = dto.Email,
            Phone = dto.Phone,
            Children = dto.Children.Select(MapToImportNode).ToList()
        };
    }

    private static string NormalizeDisplayMode(string? mode)
    {
        var normalized = string.IsNullOrWhiteSpace(mode)
            ? "person"
            : mode.Trim().ToLowerInvariant();

        if (!AllowedDisplayModes.Contains(normalized))
        {
            throw new InvalidOperationException("顯示模式僅支援 person 或 code。");
        }

        return normalized;
    }

    private static string NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    private static string NormalizeActor(string actor)
    {
        return NormalizeText(string.IsNullOrWhiteSpace(actor) ? "system" : actor, 64);
    }

    private static List<OrgNodeDto> BuildTree(List<OrgNode> nodes)
    {
        var lookup = nodes.ToLookup(x => x.ParentId);
        return BuildBranch(null, lookup);
    }

    private static List<OrgNodeDto> BuildBranch(int? parentId, ILookup<int?, OrgNode> lookup)
    {
        var result = new List<OrgNodeDto>();

        foreach (var node in lookup[parentId].OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            var dto = Map(node);
            dto.Children = BuildBranch(node.Id, lookup);
            result.Add(dto);
        }

        return result;
    }

    private static OrgNodeDto Map(OrgNode node)
    {
        return new OrgNodeDto
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Code = node.Code,
            DepartmentName = node.DepartmentName,
            PersonName = node.PersonName,
            Title = node.Title,
            Email = node.Email,
            Phone = node.Phone,
            SortOrder = node.SortOrder
        };
    }

    private static IEnumerable<int> CollectDescendantIds(int nodeId, List<OrgNode> nodes)
    {
        var byParent = nodes.ToLookup(x => x.ParentId);
        var result = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(nodeId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!result.Add(current))
            {
                continue;
            }

            foreach (var child in byParent[current])
            {
                stack.Push(child.Id);
            }
        }

        return result;
    }

    private static bool ContainsIgnoreCase(string value, string keyword)
    {
        return value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPath(OrgNode node, Dictionary<int, OrgNode> nodeById)
    {
        var path = new List<string>();
        var current = node;

        while (true)
        {
            var label = string.IsNullOrWhiteSpace(current.DepartmentName)
                ? (string.IsNullOrWhiteSpace(current.PersonName) ? current.Code : current.PersonName)
                : current.DepartmentName;

            path.Add(string.IsNullOrWhiteSpace(label) ? $"#{current.Id}" : label);

            if (current.ParentId is null)
            {
                break;
            }

            if (!nodeById.TryGetValue(current.ParentId.Value, out var parent))
            {
                break;
            }

            current = parent;
        }

        path.Reverse();
        return string.Join(" > ", path);
    }

    private void ValidateNodeMutation(NodeMutationRequest request)
    {
        var dept = request.DepartmentName?.Trim() ?? string.Empty;
        var person = request.PersonName?.Trim() ?? string.Empty;
        var code = request.Code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(dept) && string.IsNullOrWhiteSpace(person) && string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("部門名稱、人員姓名、單位代碼至少需填寫其中一項。");
        }

        EnsureMaxLength(request.Code, 64, "單位代碼");
        EnsureMaxLength(request.DepartmentName, 128, "部門名稱");
        EnsureMaxLength(request.PersonName, 64, "人員姓名");
        EnsureMaxLength(request.Title, 64, "職稱");
        EnsureMaxLength(request.Email, 128, "電子郵件");
        EnsureMaxLength(request.Phone, 32, "聯絡電話");

        var email = request.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
        {
            throw new InvalidOperationException("電子郵件格式不正確。");
        }
    }

    private static void EnsureMaxLength(string? value, int maxLength, string fieldName)
    {
        if (!string.IsNullOrEmpty(value) && value.Trim().Length > maxLength)
        {
            throw new InvalidOperationException($"{fieldName}長度不可超過 {maxLength} 字元。");
        }
    }

    private void ValidateImportNodeTree(ImportOrgNodeRequest node, int depth, ref int count, ref int maxDepth)
    {
        count++;
        maxDepth = Math.Max(maxDepth, depth);

        if (count > 5000)
        {
            throw new InvalidOperationException("匯入節點數量不可超過 5000。");
        }

        EnsureMaxLength(node.Code, 64, "單位代碼");
        EnsureMaxLength(node.DepartmentName, 128, "部門名稱");
        EnsureMaxLength(node.PersonName, 64, "人員姓名");
        EnsureMaxLength(node.Title, 64, "職稱");
        EnsureMaxLength(node.Email, 128, "電子郵件");
        EnsureMaxLength(node.Phone, 32, "聯絡電話");

        var email = node.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
        {
            throw new InvalidOperationException("匯入資料中包含格式不正確的電子郵件。");
        }

        var dept = node.DepartmentName?.Trim() ?? string.Empty;
        var person = node.PersonName?.Trim() ?? string.Empty;
        var code = node.Code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(dept) && string.IsNullOrWhiteSpace(person) && string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("匯入資料中有節點缺少部門名稱、人員姓名與單位代碼。");
        }

        foreach (var child in node.Children)
        {
            ValidateImportNodeTree(child, depth + 1, ref count, ref maxDepth);
        }
    }

    private async Task LogAuditAsync(string action, int? nodeId, string detail, string actor, string role)
    {
        var entry = new AuditLog
        {
            Action = NormalizeText(action, 64),
            NodeId = nodeId,
            Detail = NormalizeText(detail, 2048),
            Actor = NormalizeActor(actor),
            Role = NormalizeText(string.IsNullOrWhiteSpace(role) ? "unknown" : role, 32),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync();
    }

    private string GetBackupDirectory()
    {
        var configured = config["Backup:Directory"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        return Path.Combine(AppContext.BaseDirectory, "backups");
    }

    private string GetDatabasePath()
    {
        var connectionString = config.GetConnectionString("DefaultConnection") ?? "Data Source=orgchart.db";
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new InvalidOperationException("找不到 SQLite 資料庫路徑。");
        }

        return Path.GetFullPath(builder.DataSource);
    }
}
