using System.ComponentModel.DataAnnotations;

namespace Cortex.Api.Dtos;

public record TaskResponse(
    Guid Id, string Title, string Status, string Priority,
    DateTimeOffset? DueDate, List<string> Tags,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt
);

public record CreateTaskRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [AllowedValues("pending", "in_progress", "done", null)] string? Status,
    [AllowedValues("low", "normal", "high", null)] string? Priority,
    DateTimeOffset? DueDate,
    [MaxLength(10, ErrorMessage = "A task can have at most 10 tags.")] List<string>? Tags
);

public record UpdateTaskRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, AllowedValues("pending", "in_progress", "done")] string Status,
    [Required, AllowedValues("low", "normal", "high")] string Priority,
    DateTimeOffset? DueDate,
    [MaxLength(10, ErrorMessage = "A task can have at most 10 tags.")] List<string>? Tags
);