using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cortex.Api.Data;
using Cortex.Api.Dtos;
using Cortex.Api.Extensions;
using Cortex.Api.Mapping;

namespace Cortex.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/summary")]
public class SummaryController : ControllerBase
{
    private const int MaxListItems = 20; // full lists available via /api/tasks with filters

    private readonly CortexDbContext _db;
    public SummaryController(CortexDbContext db) => _db = db;

    [HttpGet("weekly")]
    public async Task<ActionResult<WeeklySummaryResponse>> GetWeeklySummary(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var now = DateTimeOffset.UtcNow;
        var weekAgo = now.AddDays(-7);
        var weekFromNow = now.AddDays(7);

        var tasksCompletedThisWeek = await _db.Tasks.CountAsync(
            t => t.UserId == userId && t.Status == "done" && t.CompletedAt != null && t.CompletedAt >= weekAgo,
            cancellationToken);

        var tasksCreatedThisWeek = await _db.Tasks.CountAsync(
            t => t.UserId == userId && t.CreatedAt >= weekAgo, cancellationToken);

        var notesCreatedThisWeek = await _db.Notes.CountAsync(
            n => n.UserId == userId && n.CreatedAt >= weekAgo, cancellationToken);

        var overdueTasks = await _db.Tasks.AsNoTracking()
            .Where(t => t.UserId == userId && t.Status != "done" && t.DueDate != null && t.DueDate < now)
            .OrderBy(t => t.DueDate)
            .Take(MaxListItems)
            .Include(t => t.Tags)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var dueThisWeek = await _db.Tasks.AsNoTracking()
            .Where(t => t.UserId == userId && t.Status != "done" && t.DueDate != null
                        && t.DueDate >= now && t.DueDate <= weekFromNow)
            .OrderBy(t => t.DueDate)
            .Take(MaxListItems)
            .Include(t => t.Tags)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return Ok(new WeeklySummaryResponse(
            tasksCompletedThisWeek,
            tasksCreatedThisWeek,
            notesCreatedThisWeek,
            overdueTasks.Select(TaskMapper.ToResponse).ToList(),
            dueThisWeek.Select(TaskMapper.ToResponse).ToList()
        ));
    }
}