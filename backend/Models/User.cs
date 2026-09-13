namespace Cortex.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Note> Notes { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
}