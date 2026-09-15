using Cortex.Api.Data;
using Cortex.Api.Dtos;
using Cortex.Api.Extensions;
using Cortex.Api.Models;
using Cortex.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notes")]
public class NotesController : ControllerBase
{
    private const int MaxPageSize = 50;
    private readonly CortexDbContext _db;
    private readonly ITagService _tagService;

    public NotesController(CortexDbContext db, ITagService tagService)
    {
        _db = db;
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<NoteResponse>>> GetNotes(
        [FromQuery] string? search,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        page = Math.Max(page, 1);

        var baseQuery = _db.Notes.AsNoTracking().Where(n => n.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
            baseQuery = baseQuery.Where(n =>
                EF.Functions.ILike(n.Title, $"%{search}%") ||
                EF.Functions.ILike(n.Body, $"%{search}%"));

        if (!string.IsNullOrWhiteSpace(tag))
            baseQuery = baseQuery.Where(n => n.Tags.Any(t => t.Name == tag));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var notes = await baseQuery
            .OrderByDescending(n => n.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(n => n.Tags)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<NoteResponse>(notes.Select(ToResponse).ToList(), page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteResponse>> GetNote(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var note = await _db.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

        return note is null ? NotFound() : Ok(ToResponse(note));
    }

    [HttpPost]
    public async Task<ActionResult<NoteResponse>> CreateNote(CreateNoteRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!_tagService.ValidateTagNames(request.Tags, out var tagError))
        {
            ModelState.AddModelError(nameof(request.Tags), tagError!);
            return ValidationProblem(ModelState);
        }

        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Body = request.Body ?? string.Empty,
            Tags = await _tagService.ResolveTagsAsync(userId, request.Tags, cancellationToken)
        };

        _db.Notes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetNote), new { id = note.Id }, ToResponse(note));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateNote(Guid id, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!_tagService.ValidateTagNames(request.Tags, out var tagError))
        {
            ModelState.AddModelError(nameof(request.Tags), tagError!);
            return ValidationProblem(ModelState);
        }

        var note = await _db.Notes
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

        if (note is null) return NotFound();

        note.Title = request.Title.Trim();
        note.Body = request.Body ?? string.Empty;
        note.Tags = await _tagService.ResolveTagsAsync(userId, request.Tags, cancellationToken);
        note.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
        if (note is null) return NotFound();

        _db.Notes.Remove(note);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
    private static NoteResponse ToResponse(Note note) => new(
        note.Id,
        note.Title,
        note.Body,
        note.Tags.Select(t => t.Name).OrderBy(n => n).ToList(),
        note.CreatedAt,
        note.UpdatedAt
    );
}