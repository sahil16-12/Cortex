namespace Cortex.Api.Models;

public class Tag
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public List<Note> Notes { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
}