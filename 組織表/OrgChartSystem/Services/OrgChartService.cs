using Microsoft.EntityFrameworkCore;
using OrgChartSystem.Contracts;
using OrgChartSystem.Data;
using OrgChartSystem.Models;

namespace OrgChartSystem.Services;

public class OrgChartService(AppDbContext db)
{
    private static readonly HashSet<string> AllowedDisplayModes = ["person", "code"];

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

    public async Task<OrgNodeDto> CreateNodeAsync(NodeMutationRequest request)
    {
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

        return Map(node);
    }

    public async Task<OrgNodeDto?> UpdateNodeAsync(int nodeId, NodeMutationRequest request)
    {
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

        return Map(node);
    }

    public async Task<bool> DeleteNodeAsync(int nodeId)
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

        for (var i = 0; i < siblings.Count; i++)
        {
            siblings[i].SortOrder = i;
            siblings[i].UpdatedAtUtc = DateTime.UtcNow;
        }

        db.OrgNodes.RemoveRange(nodes.Where(x => deleteSet.Contains(x.Id)));
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MoveNodeAsync(int nodeId, string direction)
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

        return true;
    }

    public async Task<bool> RepositionNodeAsync(int nodeId, int? newParentId, int? newIndex)
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
        return true;
    }

    public async Task<string> UpdateDisplayModeAsync(string displayMode)
    {
        var normalized = NormalizeDisplayMode(displayMode);
        var setting = await GetOrCreateSettingAsync();

        setting.DisplayMode = normalized;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return normalized;
    }

    public async Task ImportAsync(ImportOrgChartRequest request)
    {
        if (request.Nodes.Count == 0)
        {
            throw new InvalidOperationException("匯入內容不可為空。請至少提供一個根節點。");
        }

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
}
