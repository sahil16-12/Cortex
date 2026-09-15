using System.ComponentModel.DataAnnotations;

namespace Cortex.Api.Dtos;

public record NoteResponse(
    Guid Id,
    string Title,
    string Body,
    List<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record CreateNoteRequest(
    [Required, StringLength(200, MinimumLength = 1)]
    string Title,

    [StringLength(20000)]
    string? Body,

    [MaxLength(10, ErrorMessage = "A note can have at most 10 tags.")]
    List<string>? Tags
);

public record UpdateNoteRequest(
    [Required, StringLength(200, MinimumLength = 1)]
    string Title,

    [StringLength(20000)]
    string? Body,

    [MaxLength(10, ErrorMessage = "A note can have at most 10 tags.")]
    List<string>? Tags
);