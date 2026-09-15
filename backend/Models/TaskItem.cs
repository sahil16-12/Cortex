namespace Cortex.Api.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";   // pending | in_progress | done
    public string Priority { get; set; } = "normal";  // low | normal | high
    public DateTimeOffset? CompletedAt { get; set; } // Populate when status moves to "done" state
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public List<Tag> Tags { get; set; } = new();
}