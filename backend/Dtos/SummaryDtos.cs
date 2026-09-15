namespace Cortex.Api.Dtos;

public record WeeklySummaryResponse(
    int TasksCompletedThisWeek,
    int TasksCreatedThisWeek,
    int NotesCreatedThisWeek,
    List<TaskResponse> OverdueTasks,
    List<TaskResponse> DueThisWeek
);