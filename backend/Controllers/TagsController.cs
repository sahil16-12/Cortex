using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cortex.Api.Data;
using Cortex.Api.Dtos;
using Cortex.Api.Extensions;

namespace Cortex.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly CortexDbContext _db;
    public TagsController(CortexDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TagResponse>>> GetTags(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var tags = await _db.Tags
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse(t.Id, t.Name, t.Notes.Count, t.Tasks.Count))
            .ToListAsync(cancellationToken);

        return Ok(tags);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> RenameTag(Guid id, RenameTagRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var newName = request.Name.Trim().ToLowerInvariant();

        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (tag is null) return NotFound();

        if (tag.Name == newName) return NoContent(); // already this name, nothing to do

        var nameTaken = await _db.Tags.AnyAsync(t => t.UserId == userId && t.Name == newName, cancellationToken);
        if (nameTaken) return Conflict($"A tag named '{newName}' already exists.");

        tag.Name = newName;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (tag is null) return NotFound();

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}