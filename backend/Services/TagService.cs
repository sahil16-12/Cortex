using Microsoft.EntityFrameworkCore;
using Cortex.Api.Data;
using Cortex.Api.Models;

namespace Cortex.Api.Services;

public class TagService : ITagService
{
    private readonly CortexDbContext _db;
    public TagService(CortexDbContext db) => _db = db;

    public bool ValidateTagNames(List<string>? tagNames, out string? error)
    {
        error = null;
        if (tagNames is null) return true;
        if (tagNames.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > 50))
        {
            error = "Each tag must be 1-50 characters.";
            return false;
        }
        return true;
    }

    public async Task<List<Tag>> ResolveTagsAsync(Guid userId, List<string>? tagNames, CancellationToken cancellationToken)
    {
        if (tagNames is null || tagNames.Count == 0) return new List<Tag>();

        var normalized = tagNames.Select(t => t.Trim().ToLowerInvariant()).Distinct().ToList();

        var existing = await _db.Tags
            .Where(t => t.UserId == userId && normalized.Contains(t.Name))
            .ToListAsync(cancellationToken);

        var missingNames = normalized.Except(existing.Select(t => t.Name));
        var created = missingNames.Select(name => new Tag { Id = Guid.NewGuid(), UserId = userId, Name = name }).ToList();

        if (created.Count > 0) _db.Tags.AddRange(created);
        return existing.Concat(created).ToList();
    }
}