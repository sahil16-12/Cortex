using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cortex.Api.Data;
using Cortex.Api.Dtos;
using Cortex.Api.Extensions;
using Cortex.Api.Models;
using Cortex.Api.Services;

namespace Cortex.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private const int MaxPageSize = 50;
    private readonly CortexDbContext _db;
    private readonly ITagService _tagService;

    public TasksController(CortexDbContext db, ITagService tagService)
    {
        _db = db;
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TaskResponse>>> GetTasks(
        [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] string? tag,
        [FromQuery] string? search, [FromQuery] DateTimeOffset? dueBefore, [FromQuery] DateTimeOffset? dueAfter,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        page = Math.Max(page, 1);

        var query = _db.Tasks.AsNoTracking().Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(priority)) query = query.Where(t => t.Priority == priority);
        if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(t => t.Tags.Any(x => x.Name == tag));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => EF.Functions.ILike(t.Title, $"%{search}%"));
        if (dueBefore.HasValue) query = query.Where(t => t.DueDate != null && t.DueDate <= dueBefore);
        if (dueAfter.HasValue) query = query.Where(t => t.DueDate != null && t.DueDate >= dueAfter);

        var totalCount = await query.CountAsync(cancellationToken);

        var tasks = await query
            .OrderBy(t => t.DueDate ?? DateTimeOffset.MaxValue)   // tasks with no due date sort last
            .ThenByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(t => t.Tags)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<TaskResponse>(tasks.Select(ToResponse).ToList(), page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var task = await _db.Tasks.AsNoTracking().Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        return task is null ? NotFound() : Ok(ToResponse(task));
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!_tagService.ValidateTagNames(request.Tags, out var tagError))
        {
            ModelState.AddModelError(nameof(request.Tags), tagError!);
            return ValidationProblem(ModelState);
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Status = request.Status ?? "pending",
            Priority = request.Priority ?? "normal",
            DueDate = request.DueDate,
            Tags = await _tagService.ResolveTagsAsync(userId, request.Tags, cancellationToken)
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, ToResponse(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!_tagService.ValidateTagNames(request.Tags, out var tagError))
        {
            ModelState.AddModelError(nameof(request.Tags), tagError!);
            return ValidationProblem(ModelState);
        }

        var task = await _db.Tasks.Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (task is null) return NotFound();

        task.Title = request.Title.Trim();
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.Tags = await _tagService.ResolveTagsAsync(userId, request.Tags, cancellationToken);
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (task is null) return NotFound();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static TaskResponse ToResponse(TaskItem task) => new(
        task.Id, task.Title, task.Status, task.Priority, task.DueDate,
        task.Tags.Select(t => t.Name).OrderBy(n => n).ToList(),
        task.CreatedAt, task.UpdatedAt
    );
}