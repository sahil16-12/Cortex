using Cortex.Api.Dtos;
using Cortex.Api.Models;

namespace Cortex.Api.Mapping;

public static class TaskMapper
{
    public static TaskResponse ToResponse(TaskItem task) => new(
        task.Id, task.Title, task.Status, task.Priority, task.DueDate, task.CompletedAt,
        task.Tags.Select(t => t.Name).OrderBy(n => n).ToList(),
        task.CreatedAt, task.UpdatedAt
    );
}