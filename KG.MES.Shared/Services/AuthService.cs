// KG.MES.Shared/Services/AuthService.cs
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KG.MES.Shared.Models.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KG.MES.Shared.Services;

public class AuthService
{
	private readonly HttpClient _httpClient;
	private readonly ILogger<AuthService> _logger;
	private readonly string _baseUrl;

	public string? LastError { get; private set; }

	private class ErrorResponse
	{
		[JsonPropertyName("error")]
		public string? Error { get; set; }
	}

	public AuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthService> logger)
	{
		_httpClient = httpClient;
		_logger = logger;
		_baseUrl = configuration["ProductionApi:BaseUrl"] ?? "http://192.168.0.179:3031/api";
	}

	public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
	{
		try
		{
			var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/auth/login", request);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				_logger.LogWarning("Login failed: {Error}", error?.Error);

				// Сохраняем ошибку для отображения
				LastError = error?.Error ?? "Неверные учётные данные";
				return null;
			}

			return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during login");
			LastError = $"Error during login: {ex.Message}";
			return null;
		}
	}

	public async Task<LoginResponseDto?> RefreshAsync(RefreshRequestDto refreshRequestDto)
	{
		try
		{
			var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/auth/refresh", refreshRequestDto);

			if (!response.IsSuccessStatusCode) return null;
			return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error refreshing token");
			return null;
		}
	}
}