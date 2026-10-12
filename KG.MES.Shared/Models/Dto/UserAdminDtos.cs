using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// DTO для создания пользователя
/// </summary>
public class CreateUserRequestDto
{
	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("password")]
	public string? Password { get; set; }

	[JsonPropertyName("roleName")]
	public string? RoleName { get; set; } = "User";
}

/// <summary>
/// DTO для назначения роли
/// </summary>
public class SetRoleRequestDto
{
	[JsonPropertyName("roleName")]
	public string RoleName { get; set; } = string.Empty;
}

/// <summary>
/// DTO для списка пользователей (краткая информация)
/// </summary>
public class UserAdminListItemDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("roleName")]
	public string? RoleName { get; set; }

	[JsonPropertyName("isActive")]
	public bool IsActive { get; set; }

	[JsonPropertyName("isPasswordSet")]
	public bool IsPasswordSet { get; set; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("lastLoginAt")]
	public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// DTO для детальной информации о пользователе
/// </summary>
public class UserAdminDetailsDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("roleName")]
	public string? RoleName { get; set; }

	[JsonPropertyName("roleLevel")]
	public int? RoleLevel { get; set; }

	[JsonPropertyName("isActive")]
	public bool IsActive { get; set; }

	[JsonPropertyName("isPasswordSet")]
	public bool IsPasswordSet { get; set; }

	[JsonPropertyName("CreatedAt")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("lastLoginAt")]
	public DateTime? LastLoginAt { get; set; }

	[JsonPropertyName("workplaces")]
	public List<UserWorkplaceDto> Workplaces { get; set; } = [];
}

/// <summary>
/// DTO для результата создания пользователя
/// </summary>
public class CreateUserResultDto
{
	[JsonPropertyName("success")]
	public bool Success { get; set; }

	[JsonPropertyName("error")]
	public string? Error { get; set; }

	[JsonPropertyName("user")]
	public UserAdminListItemDto? User { get; set; }
}

/// <summary>
/// DTO для результата сброса пароля
/// </summary>
public class ResetPasswordResultDto
{
	[JsonPropertyName("success")]
	public bool Success { get; set; }

	[JsonPropertyName("newPassword")]
	public string? NewPassword { get; set; }

	[JsonPropertyName("error")]
	public string? Error { get; set; }
}