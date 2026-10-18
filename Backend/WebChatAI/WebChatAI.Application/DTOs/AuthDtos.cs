using System.Text.Json.Serialization;

namespace WebChatAI.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailVerified,
    string? AvatarUrl,
    DateTime CreatedAt
);

public record AuthResponseDto(
    UserDto User,
    string AccessToken,
    int? ExpiresIn = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? RefreshToken = null
);

public record SecureAuthResponseDto(
    UserDto User,
    string Message = "Authentication successful"
);

public record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? ConfirmPassword
);

public record LoginRequestDto(
    string Email,
    string Password,
    bool? RememberMe
);

public record VerifyEmailRequestDto(
    string? Token,
    string? Code
);

public record ResendVerificationRequestDto(
    string Email
);

public record ForgotPasswordRequestDto(
    string Email
);

public record ResetPasswordRequestDto(
    string Token,
    string NewPassword,
    string? ConfirmPassword
);

public record RefreshTokenRequestDto(
    string? RefreshToken = null
);
