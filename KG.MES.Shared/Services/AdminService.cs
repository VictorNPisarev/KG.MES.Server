// KG.MES.Shared/Services/AdminService.cs
using System.Net.Http.Json;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KG.MES.Shared.Services;

public class AdminService : AuthorizedApiService
{
	private readonly ILogger<AdminService> logger;
	private readonly string baseUrl;

	public AdminService(
		HttpClient httpClient,
		IConfiguration configuration,
		ILogger<AdminService> logger,
		IServiceProvider serviceProvider) : base(httpClient, serviceProvider)
	{
		this.logger = logger;
		baseUrl = configuration["ProductionApi:BaseUrl"] ?? "http://192.168.0.254:3031/api";
	}

	// ========== ЛИЦЕНЗИИ ==========

	public async Task<List<LicenseDto>> GetLicensesAsync(
		int page = 1, int limit = 50, string? search = null,
		LicenseType? type = null, bool? isActive = null)
	{
		try
		{
			EnsureAuthorization();

			var query = $"?page={page}&limit={limit}";
			if (!string.IsNullOrEmpty(search)) query += $"&search={Uri.EscapeDataString(search)}";
			if (type.HasValue) query += $"&type={type.Value}";
			if (isActive.HasValue) query += $"&isActive={isActive.Value}";

			var response = await httpClient.GetFromJsonAsync<PaginatedResponse<LicenseDto>>($"{baseUrl}/admin/licenses{query}");
			return response?.Data ?? new();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching licenses");
			return new List<LicenseDto>();
		}
	}

	public async Task<LicenseDto?> GetLicenseByIdAsync(Guid licenseId)
	{
		try
		{
			EnsureAuthorization();

			return await httpClient.GetFromJsonAsync<LicenseDto>($"{baseUrl}/admin/licenses/{licenseId}");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching license {Id}", licenseId);
			return null;
		}
	}

	public async Task<LicenseDto?> CreateLicenseAsync(CreateLicenseRequestDto request)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/admin/licenses/create", request);
			if (!response.IsSuccessStatusCode) return null;
			return await response.Content.ReadFromJsonAsync<LicenseDto>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error creating license");
			return null;
		}
	}

	public async Task<bool> RevokeLicenseAsync(Guid licenseId, RevokeLicenseRequestDto request)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/admin/licenses/{licenseId}/revoke", request);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error revoking license {Id}", licenseId);
			return false;
		}
	}

	public async Task<bool> ActivateLicenseAsync(Guid licenseId)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsync($"{baseUrl}/admin/licenses/{licenseId}/activate", null);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error activating license {Id}", licenseId);
			return false;
		}
	}

	public async Task<bool> ExtendLicenseAsync(Guid licenseId, int? daysToAdd)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsJsonAsync(
				$"{baseUrl}/admin/licenses/{licenseId}/extend",
				new ExtendLicenseRequestDto { DaysToAdd = daysToAdd });
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error extending license {Id}", licenseId);
			return false;
		}
	}

	public async Task<List<DeviceInfoDto>> GetLicenseDevicesAsync(Guid licenseId)
	{
		try
		{
			EnsureAuthorization();

			return await httpClient.GetFromJsonAsync<List<DeviceInfoDto>>(
				$"{baseUrl}/admin/licenses/{licenseId}/devices") ?? new();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching devices for license {Id}", licenseId);
			return new();
		}
	}

	// ========== ПОЛЬЗОВАТЕЛИ ==========

	public async Task<List<UserAdminListItemDto>> GetUsersAsync(
		int page = 1, int limit = 50, string? search = null)
	{
		try
		{
			EnsureAuthorization();

			var query = $"?page={page}&limit={limit}";
			if (!string.IsNullOrEmpty(search)) query += $"&search={Uri.EscapeDataString(search)}";

			var response = await httpClient.GetFromJsonAsync<PaginatedResponse<UserAdminListItemDto>>($"{baseUrl}/admin/users{query}");
			
			return response?.Data ?? new();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching users");
			return new List<UserAdminListItemDto>();
		}
	}

	public async Task<UserAdminDetailsDto?> GetUserByIdAsync(Guid userId)
	{
		try
		{
			EnsureAuthorization();

			return await httpClient.GetFromJsonAsync<UserAdminDetailsDto>($"{baseUrl}/admin/users/{userId}");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching user {Id}", userId);
			return null;
		}
	}

	public async Task<CreateUserResultDto?> CreateUserAsync(CreateUserRequestDto request)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/admin/users/create", request);
			if (!response.IsSuccessStatusCode) return null;
			return await response.Content.ReadFromJsonAsync<CreateUserResultDto>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error creating user");
			return null;
		}
	}

	public async Task<bool> BlockUserAsync(Guid userId)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsync($"{baseUrl}/admin/users/{userId}/block", null);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error blocking user {Id}", userId);
			return false;
		}
	}

	public async Task<bool> UnblockUserAsync(Guid userId)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsync($"{baseUrl}/admin/users/{userId}/unblock", null);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error unblocking user {Id}", userId);
			return false;
		}
	}

	public async Task<bool> ResetUserPasswordAsync(Guid userId)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsync($"{baseUrl}/admin/users/{userId}/resetPassword", null);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error resetting password for user {Id}", userId);
			return false;
		}
	}

	public async Task<bool> SetUserRoleAsync(Guid userId, SetRoleRequestDto request)
	{
		try
		{
			EnsureAuthorization();

			var response = await httpClient.PostAsJsonAsync($"{baseUrl}/admin/users/{userId}/setRole", request);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error setting role for user {Id}", userId);
			return false;
		}
	}

	public async Task<List<RoleDto>> GetRolesAsync()
	{
		try
		{
			EnsureAuthorization();

			return await httpClient.GetFromJsonAsync<List<RoleDto>>($"{baseUrl}/admin/roles")
				   ?? new List<RoleDto>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching roles");
			return new List<RoleDto>();
		}
	}
}