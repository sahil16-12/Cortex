using Cortex.Api.Models;

namespace Cortex.Api.Services;

public interface ITagService
{
    bool ValidateTagNames(List<string>? tagNames, out string? error);
    Task<List<Tag>> ResolveTagsAsync(Guid userId, List<string>? tagNames, CancellationToken cancellationToken);
}