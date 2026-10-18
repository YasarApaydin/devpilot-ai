using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

[Authorize]
[Route("api/ai-keys")]
public class AiKeysController : ApiController
{
    private readonly IUserAiKeyRepository _userAiKeyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly IAiProviderRegistry _providerRegistry;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiKeysController> _logger;

    public AiKeysController(
        IMediator mediator,
        IUserAiKeyRepository userAiKeyRepository,
        IUserRepository userRepository,
        IApiKeyEncryptionService encryptionService,
        IAiProviderRegistry providerRegistry,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AiKeysController> logger) : base(mediator)
    {
        _userAiKeyRepository = userAiKeyRepository;
        _userRepository = userRepository;
        _encryptionService = encryptionService;
        _providerRegistry = providerRegistry;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserAiKeys(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUserService.UserId.Value;
        var userKeys = await _userAiKeyRepository.GetUserKeysAsync(userId, cancellationToken);
        var keyMap = userKeys
            .Where(k => !string.IsNullOrWhiteSpace(k.Provider))
            .GroupBy(k => k.Provider.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.OrderByDescending(k => k.UpdatedAt ?? k.CreatedAt).First());

        var adapters = _providerRegistry.GetAllAdapters();
        var result = new List<UserAiKeyDto>();

        foreach (var adapter in adapters)
        {
            if (adapter.Provider == "system") continue; // system model handled separately

            keyMap.TryGetValue(adapter.Provider.ToLowerInvariant(), out var savedKey);

            result.Add(new UserAiKeyDto(
                Provider: adapter.Provider,
                DisplayName: adapter.DisplayName,
                IsConfigured: savedKey != null && savedKey.IsActive,
                KeyHint: savedKey?.KeyHint,
                IsValid: savedKey?.IsValid ?? true,
                LastTestedAt: savedKey?.LastTestedAt,
                PreferredModel: savedKey?.PreferredModel,
                DocUrl: adapter.DocUrl,
                AvailableModels: adapter.DefaultModels.ToList()
            ));
        }

        return Ok(ApiResponse<List<UserAiKeyDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> SaveUserAiKey([FromBody] SaveUserAiKeyRequestDto request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUserService.UserId.Value;
        var provider = request.Provider.Trim().ToLowerInvariant();

        var adapter = _providerRegistry.GetAdapter(provider);
        if (adapter == null || adapter.Provider == "system")
        {
            return BadRequest(ApiResponse<UserAiKeyDto>.Fail("Geçersiz veya desteklenmeyen AI sağlayıcısı."));
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest(ApiResponse<UserAiKeyDto>.Fail("API anahtarı boş olamaz."));
        }

        var trimmedKey = request.ApiKey.Trim();

        try
        {
            // Test the connection before saving to verify validity
            var testResult = await adapter.TestConnectionAsync(trimmedKey, cancellationToken);

            var encryptedKey = _encryptionService.EncryptApiKey(trimmedKey);
            var maskedHint = _encryptionService.MaskApiKey(trimmedKey, provider);

            // Use GetAnyByUserAndProviderAsync to find existing records including soft-deleted ones (eliminates 23505 unique constraint violation)
            var existingKey = await _userAiKeyRepository.GetAnyByUserAndProviderAsync(userId, provider, cancellationToken);

            if (existingKey != null)
            {
                existingKey.EncryptedApiKey = encryptedKey;
                existingKey.KeyHint = maskedHint;
                existingKey.IsActive = true;
                existingKey.IsValid = testResult.Success;
                existingKey.LastTestedAt = DateTime.UtcNow;
                existingKey.UpdatedAt = DateTime.UtcNow;
                existingKey.IsDeleted = false; // Resurrect soft-deleted entity
                existingKey.DeletedAt = null;
                existingKey.DeletedBy = null;
                if (!string.IsNullOrWhiteSpace(request.PreferredModel))
                {
                    existingKey.PreferredModel = request.PreferredModel;
                }
                _userAiKeyRepository.Update(existingKey);
            }
            else
            {
                var newKey = new UserAiKey
                {
                    UserId = userId,
                    Provider = provider,
                    EncryptedApiKey = encryptedKey,
                    KeyHint = maskedHint,
                    IsActive = true,
                    IsValid = testResult.Success,
                    LastTestedAt = DateTime.UtcNow,
                    PreferredModel = request.PreferredModel
                };
                await _userAiKeyRepository.AddAsync(newKey, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var responseDto = new UserAiKeyDto(
                Provider: adapter.Provider,
                DisplayName: adapter.DisplayName,
                IsConfigured: true,
                KeyHint: maskedHint,
                IsValid: testResult.Success,
                LastTestedAt: DateTime.UtcNow,
                PreferredModel: request.PreferredModel,
                DocUrl: adapter.DocUrl,
                AvailableModels: testResult.Models ?? adapter.DefaultModels.ToList()
            );

            var message = testResult.Success
                ? $"{adapter.DisplayName} API anahtarı başarıyla doğrulandı ve kaydedildi."
                : $"{adapter.DisplayName} API anahtarı kaydedildi ancak test başarısız oldu: {testResult.Message}";

            return Ok(ApiResponse<UserAiKeyDto>.Ok(responseDto, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AI key for provider {Provider} and user {UserId}", provider, userId);
            return StatusCode(500, ApiResponse<UserAiKeyDto>.Fail($"API anahtarı kaydedilirken sunucu hatası oluştu: {ex.Message}"));
        }
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestUserAiKey([FromBody] TestUserAiKeyRequestDto request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUserService.UserId.Value;
        var provider = request.Provider.Trim().ToLowerInvariant();

        var adapter = _providerRegistry.GetAdapter(provider);
        if (adapter == null)
        {
            return BadRequest(ApiResponse<AiKeyTestResponseDto>.Fail("Desteklenmeyen AI sağlayıcısı."));
        }

        string apiKeyToTest;
        UserAiKey? storedKey = null;

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            apiKeyToTest = request.ApiKey.Trim();
        }
        else
        {
            storedKey = await _userAiKeyRepository.GetByUserAndProviderAsync(userId, provider, cancellationToken);
            if (storedKey == null || !storedKey.IsActive)
            {
                return BadRequest(ApiResponse<AiKeyTestResponseDto>.Fail("Kayıtlı bir API anahtarı bulunamadı. Lütfen yeni bir anahtar girin."));
            }

            try
            {
                apiKeyToTest = _encryptionService.DecryptApiKey(storedKey.EncryptedApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt API key for provider {Provider}", provider);
                return BadRequest(ApiResponse<AiKeyTestResponseDto>.Fail("Şifrelenmiş anahtar çözülemedi. Lütfen anahtarınızı yeniden kaydedin."));
            }
        }

        var testResult = await adapter.TestConnectionAsync(apiKeyToTest, cancellationToken);

        if (storedKey != null)
        {
            storedKey.IsValid = testResult.Success;
            storedKey.LastTestedAt = DateTime.UtcNow;
            _userAiKeyRepository.Update(storedKey);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<AiKeyTestResponseDto>.Ok(testResult));
    }

    [HttpDelete("{provider}")]
    public async Task<IActionResult> DeleteUserAiKey(string provider, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUserService.UserId.Value;
        var normalizedProvider = provider.Trim().ToLowerInvariant();

        var userKeys = await _userAiKeyRepository.GetUserKeysAsync(userId, cancellationToken);
        var keysToDelete = userKeys
            .Where(k => string.Equals(k.Provider.Trim(), normalizedProvider, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (keysToDelete.Count > 0)
        {
            foreach (var key in keysToDelete)
            {
                _userAiKeyRepository.Delete(key);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<bool>.Ok(true, "API anahtarı başarıyla kaldırıldı."));
    }

    [HttpGet("models")]
    public async Task<IActionResult> GetAvailableModels(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUserService.UserId.Value;
        var userKeys = await _userAiKeyRepository.GetUserKeysAsync(userId, cancellationToken);
        var configuredProviders = userKeys.Where(k => k.IsActive && k.IsValid)
            .Select(k => k.Provider.ToLowerInvariant())
            .ToHashSet();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        var models = new List<AiModelInfoDto>();

        // Always include System Default model
        var systemAdapter = _providerRegistry.GetAdapter("system");
        if (systemAdapter != null)
        {
            models.AddRange(systemAdapter.DefaultModels);
        }

        // Add models for all configured providers
        foreach (var adapter in _providerRegistry.GetAllAdapters())
        {
            if (adapter.Provider == "system") continue;

            if (configuredProviders.Contains(adapter.Provider.ToLowerInvariant()))
            {
                models.AddRange(adapter.DefaultModels);
            }
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            defaultProvider = user?.DefaultAiProvider ?? "system",
            defaultModel = user?.DefaultAiModel ?? "default",
            models
        }));
    }

    [HttpPut("preference")]
    public async Task<IActionResult> SetPreference([FromBody] SetAiPreferenceRequestDto request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUserService.UserId.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound();

        var provider = request.Provider.Trim().ToLowerInvariant();
        if (provider != "system")
        {
            var hasKey = await _userAiKeyRepository.GetByUserAndProviderAsync(userId, provider, cancellationToken);
            if (hasKey == null || !hasKey.IsActive)
            {
                return BadRequest(ApiResponse<bool>.Fail($"'{request.Provider}' için kayıtlı API anahtarınız bulunmamaktadır."));
            }
        }

        user.DefaultAiProvider = provider;
        user.DefaultAiModel = request.Model.Trim();
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AiUserPreferencesDto>.Ok(new AiUserPreferencesDto(user.DefaultAiProvider, user.DefaultAiModel)));
    }
}
