using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class AdminController
{
	private readonly ILicenseService licenseService;
	private readonly IUserService userService;

	public AdminController(ILicenseService licenseService, IUserService userService)
	{
		this.licenseService = licenseService;
		this.userService = userService;
	}

	public Task<IActionResult> GetLicensesHandler(int page, int limit, string? search, LicenseType? type, bool? isActive)
		=> HandleAsync(() => licenseService.GetAllLicensesAsync(page, limit, search, type, isActive));

	public Task<IActionResult> GetLicenseByIdHandler(Guid licenseId)
		=> HandleAsync(() => licenseService.GetLicenseDetailsAsync(licenseId));

	public Task<IActionResult> CreateLicenseHandler(CreateLicenseRequestDto request)
		=> HandleAsync(async () =>
		{
			//if (request.LicenseType == LicenseType.MultiDevice && request.MaxDevices.HasValue && request.MaxDevices < 1)
			//	throw new ArgumentException("MaxDevices должно быть >= 1");

			return await licenseService.CreateAsync();//.CreateLicenseAsync(request);
		});

	public Task<IActionResult> RevokeLicenseHandler(Guid licenseId, RevokeLicenseRequestDto? request)
		=> HandleAsync(async () =>
		{
			var result = await licenseService.RevokeAsync(licenseId, request?.Reason ?? "Отозвано администратором"); //.RevokeLicenseAsync(licenseId, request?.Reason ?? "Отозвано администратором");
			if (!result)
				throw new KeyNotFoundException("Лицензия не найдена");
			return result;
		});

	public Task<IActionResult> ActivateLicenseHandler(Guid licenseId)
		=> HandleAsync(async () =>
		{
			var result = await licenseService.ActivateAsync(licenseId);
			if (!result)
				throw new KeyNotFoundException("Лицензия не найдена");
			return result;
		});

	public Task<IActionResult> GetLicenseDevicesHandler(Guid licenseId)
		=> HandleAsync(() => licenseService.GetLicenseDevicesAsync(licenseId));

	public Task<IActionResult> ExtendLicenseHandler(Guid licenseId, ExtendLicenseRequestDto request)
		=> HandleAsync(async () =>
		{
			var result = await licenseService.ExtendLicenseAsync(licenseId, request.DaysToAdd);
			if (!result)
				throw new KeyNotFoundException("Лицензия не найдена");
			return result;
		});

	public Task<IActionResult> GetUsersHandler(int page = 1, int limit = 50, string? search = null)
		=> HandleAsync(() => userService.GetAllUsersAsync(page, limit, search));

	public Task<IActionResult> GetUserByIdHandler(Guid userId)
		=> HandleAsync(() => userService.GetUserByIdAsync(userId));

	public Task<IActionResult> CreateUserHandler(CreateUserRequestDto request)
		=> HandleAsync(async () =>
		{
			if (string.IsNullOrEmpty(request.Email))
				throw new ArgumentException("Email обязателен");

			return await userService.CreateUserAsync(request);
		});

	public Task<IActionResult> BlockUserHandler(Guid userId)
		=> HandleAsync(async () =>
		{
			var result = await userService.BlockUserAsync(userId);
			if (!result)
				throw new KeyNotFoundException("Пользователь не найден");
			return result;
		});

	public Task<IActionResult> UnblockUserHandler(Guid userId)
		=> HandleAsync(async () =>
		{
			var result = await userService.UnblockUserAsync(userId);
			if (!result)
				throw new KeyNotFoundException("Пользователь не найден");
			return result;
		});

	public Task<IActionResult> ResetUserPasswordHandler(Guid userId)
		=> HandleAsync(() => userService.ResetPasswordAsync(userId));

	public Task<IActionResult> SetUserRoleHandler(Guid userId, SetRoleRequestDto request)
		=> HandleAsync(async () =>
		{
			if (string.IsNullOrEmpty(request.RoleName))
				throw new ArgumentException("Укажите роль");

			var result = await userService.SetUserRoleAsync(userId, request.RoleName);
			if (!result)
				throw new KeyNotFoundException("Пользователь или роль не найдены");
			return result;
		});

	// Вспомогательный метод для обработки результатов
	private async Task<IActionResult> HandleAsync<T>(Func<Task<T>> action)
	{
		try
		{
			var result = await action();
			return Ok(result);
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message });
		}
	}

	// Перегрузка для void методов
	private async Task<IActionResult> HandleAsync(Func<Task<bool>> action)
	{
		try
		{
			var result = await action();
			return Ok(new { success = result });
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message });
		}
	}

	private async Task<IActionResult> HandleAsync(Func<Task<ResetPasswordResultDto>> action)
	{
		try
		{
			var result = await action();
			return Ok(result);
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message });
		}
	}
}