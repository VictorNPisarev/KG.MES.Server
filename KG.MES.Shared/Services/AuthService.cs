// KG.MES.Shared/Services/AuthService.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KG.MES.Shared.Models.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KG.MES.Shared.Services;

public class AuthService : AuthorizedApiService
{
	private readonly ILogger<AuthService> logger;
	private readonly string baseUrl;
	public string? LastError { get; private set; }

	private class ErrorResponse
	{
		[JsonPropertyName("error")]
		public string? Error { get; set; }

		[JsonPropertyName("currentPasswordError")]
		public bool? CurrentPasswordError { get; set; }

		[JsonPropertyName("newPasswordError")]
		public bool? NewPasswordError { get; set; }

	}

	public AuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthService> logger,
		IServiceProvider serviceProvider) : base(httpClient, serviceProvider)
	{
		this.logger = logger;
		baseUrl = configuration["ProductionApi:BaseUrl"] ?? "http://192.168.0.179:3031/api";
	}

	public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
	{
		try
		{
			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/auth/login", request);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				logger.LogWarning("Login failed: {Error}", error?.Error);

				// Сохраняем ошибку для отображения
				LastError = error?.Error ?? "Неверные учётные данные";
				return null;
			}

			return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error during login");
			LastError = $"Error during login: {ex.Message}";
			return null;
		}
	}

	public async Task<LoginResponseDto?> RefreshAsync(RefreshRequestDto refreshRequestDto)
	{
		try
		{
			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/auth/refresh", refreshRequestDto);

			if (!response.IsSuccessStatusCode) return null;
			return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error refreshing token");
			return null;
		}
	}

	public async Task<(bool Success, string? Error, bool? CurrentPassError, bool? NewPassError)> ChangePasswordAsync(string currentPassword, string newPassword)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/users/me/change-password",
				new ChangePasswordRequestDto { CurrentPassword = currentPassword, NewPassword = newPassword });

			if (response.IsSuccessStatusCode)
				return (true, null, null, null);

			var errorContent = await response.Content.ReadAsStringAsync();
			var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			return (false, error?.Error ?? "Не удалось изменить пароль", error?.CurrentPasswordError, error?.NewPasswordError);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error changing password");
			return (false, $"Ошибка соединения {ex.Message}", false, false);
		}
	}

	public async Task<(bool Success, string? Error)> SetPasswordAsync(string email, string newPassword)
	{
		try
		{
			var response = await httpClient.PostAsJsonAsync(
				$"{baseUrl}/users/set-password",
				new { email, newPassword });

			if (response.IsSuccessStatusCode)
				return (true, null);

			var errorContent = await response.Content.ReadAsStringAsync();
			var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			return (false, error?.Error ?? "Не удалось установить пароль");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error setting password");
			return (false, "Ошибка соединения с сервером");
		}
	}

	public async Task<LoginResponseDto?> RegisterAsync(RegisterRequestDto request)
	{
		try
		{
			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/auth/register", request);
			var json = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				return JsonSerializer.Deserialize<LoginResponseDto>(json,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			}

			var error = JsonSerializer.Deserialize<ErrorResponse>(json,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			LastError = error?.Error ?? "Ошибка регистрации";
			return null;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error during registration");
			LastError = "Ошибка соединения";
			return null;
		}
	}
}