using System.ComponentModel.DataAnnotations;

namespace Cortex.Api.Dtos;

public record TagResponse(Guid Id, string Name, int NoteCount, int TaskCount);

public record RenameTagRequest(
    [Required, StringLength(50, MinimumLength = 1)] string Name
);