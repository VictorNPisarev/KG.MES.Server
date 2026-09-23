using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class UsersController
{
	private readonly IUserService _userService;

	public UsersController(IUserService userService)
	{
		_userService = userService;
	}

	public async Task<IActionResult> SetPasswordHandler(SetPasswordRequestDto request)
	{
		return await SetPasswordHandler(request.Email, request);
	}
	public async Task<IActionResult> SetPasswordHandler(string email, SetPasswordRequestDto request)
	{
		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.NewPassword))
			return BadRequest(new { error = "Email and password are required" });

		var user = await _userService.GetUserByEmailAsync(email);
		if (user == null)
			return NotFound(new { error = "User not found" });

		var result = await _userService.SetPasswordAsync(user.Id, request.NewPassword);
		if (!result)
			return BadRequest(new { error = "Failed to set password" });

		return Ok(new { message = "Password set successfully" });
	}

	public async Task<IActionResult> GetUserByEmailHandler(string email)
	{
		if (string.IsNullOrEmpty(email))
			return BadRequest(new { error = "email is required" });

		var result = await _userService.GetUserByEmailAsync(email);
		if (result == null)
			return NotFound(new { error = "User not found" });

		return Ok(result);
	}

	public async Task<IActionResult> GetUserWorkplacesHandler(Guid userId)
	{
		if (userId == Guid.Empty)
			return BadRequest(new { error = "userId is required" });

		var result = await _userService.GetUserWorkplacesAsync(userId);
		return Ok(result);
	}

	public async Task<IActionResult> ChangePasswordHandler(ChangePasswordRequestDto request)
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		
		if (!Guid.TryParse(userIdClaim, out var userId))
			return Unauthorized(new { error = "Invalid user" });

		if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
			return BadRequest(new { error = "Current and new passwords are required" });

		if (request.NewPassword.Length < 6)
			return BadRequest(new { error = "Password must be at least 6 characters" });

		var result = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
		if (!result)
			return BadRequest(new { error = "Current password is incorrect" });

		return Ok(new { message = "Password changed successfully" });
	}
}