namespace WebChatAI.Application.DTOs;

public record AiModelInfoDto(
    string Id,
    string Name,
    string Provider,
    string Description,
    bool IsDefault = false,
    int? ContextWindow = null
);

public record UserAiKeyDto(
    string Provider,
    string DisplayName,
    bool IsConfigured,
    string? KeyHint,
    bool IsValid,
    DateTime? LastTestedAt,
    string? PreferredModel,
    string DocUrl,
    List<AiModelInfoDto> AvailableModels
);

public record SaveUserAiKeyRequestDto(
    string Provider,
    string ApiKey,
    string? PreferredModel = null
);

public record TestUserAiKeyRequestDto(
    string Provider,
    string? ApiKey = null
);

public record AiKeyTestResponseDto(
    bool Success,
    string Message,
    List<AiModelInfoDto>? Models = null
);

public record SetAiPreferenceRequestDto(
    string Provider,
    string Model
);

public record AiUserPreferencesDto(
    string DefaultProvider,
    string DefaultModel
);
