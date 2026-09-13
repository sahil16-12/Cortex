using System.ComponentModel.DataAnnotations;

namespace Cortex.Api.Dtos;

public record RegisterRequest(
    [Required]
    [RegularExpression(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        ErrorMessage = "Enter a valid email address."
    )]
    string Email,

    [Required]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "Password must be at least 6 characters."
    )]
    string Password
);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, DateTimeOffset ExpiresAt);