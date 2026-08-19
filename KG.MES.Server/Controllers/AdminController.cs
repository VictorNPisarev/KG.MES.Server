using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api/admin")]
//[Authorize(Roles = "Admin,User")]
public partial class AdminController : ControllerBase
{
	//-------------------------
	//POST
	//-------------------------

	//[HttpPost("licenses")]
	[HttpPost("licenses/create")]
	public Task<IActionResult> CreateLicense([FromBody] CreateLicenseRequestDto request)
		=> CreateLicenseHandler(request);

	[HttpPost("licenses/{licenseId}/revoke")]
	public Task<IActionResult> RevokeLicense(Guid licenseId, [FromBody] RevokeLicenseRequestDto? request)
		=> RevokeLicenseHandler(licenseId, request);

	[HttpPost("licenses/{licenseId}/activate")]
	public Task<IActionResult> ActivateLicense(Guid licenseId)
		=> ActivateLicenseHandler(licenseId);

	[HttpPost("licenses/{licenseId}/extend")]
	public Task<IActionResult> ExtendLicense(Guid licenseId, [FromBody] ExtendLicenseRequestDto request)
		=> ExtendLicenseHandler(licenseId, request);

	[HttpPost("users")]
	[HttpPost("users/create")]
	public Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
		=> CreateUserHandler(request);

	[HttpPost("users/{userId}/block")]
	public Task<IActionResult> BlockUser(Guid userId)
		=> BlockUserHandler(userId);

	[HttpPost("users/{userId}/unblock")]
	public Task<IActionResult> UnblockUser(Guid userId)
		=> UnblockUserHandler(userId);

	[HttpPost("users/{userId}/resetPassword")]
	public Task<IActionResult> ResetUserPassword(Guid userId)
		=> ResetUserPasswordHandler(userId);

	[HttpPost("users/{userId}/setRole")]
	public Task<IActionResult> SetUserRole(Guid userId, [FromBody] SetRoleRequestDto request)
		=> SetUserRoleHandler(userId, request);

	//-------------------------
	//GET
	//-------------------------


	[HttpGet("licenses")]
	public Task<IActionResult> GetLicenses([FromQuery] int page = 1, [FromQuery] int limit = 50,
			[FromQuery] string? search = null, [FromQuery] LicenseType? type = null, [FromQuery] bool? isActive = null)
		=> GetLicensesHandler(page, limit, search, type, isActive);

	[HttpGet("licenses/{licenseId}")]
	public Task<IActionResult> GetLicenseById(Guid licenseId)
		=> GetLicenseByIdHandler(licenseId);

	[HttpGet("licenses/{licenseId}/devices")]
	public Task<IActionResult> GetLicenseDevices(Guid licenseId)
		=> GetLicenseDevicesHandler(licenseId);

	[HttpGet("users")]
	public Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] string? search = null)
		=> GetUsersHandler(page, limit, search);

	[HttpGet("users/{userId}")]
	public Task<IActionResult> GetUserById(Guid userId)
		=> GetUserByIdHandler(userId);
}