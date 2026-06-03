using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public class UsersController : ControllerBase
{
	private readonly IUserService _userService;

	public UsersController(IUserService userService)
	{
		_userService = userService;
	}

	[HttpGet("users/by-email/{email}")]
	public async Task<IActionResult> GetUserByEmail(string email)
	{
		if (string.IsNullOrEmpty(email))
			return BadRequest(new { error = "email is required" });

		var result = await _userService.GetUserByEmailAsync(email);
		if (result == null)
			return NotFound(new { error = "User not found" });

		return Ok(result);
	}

	[HttpGet("users/{userId}/workplaces")]
	public async Task<IActionResult> GetUserWorkplaces(Guid userId)
	{
		if (userId == Guid.Empty)
			return BadRequest(new { error = "userId is required" });

		var result = await _userService.GetUserWorkplacesAsync(userId);
		return Ok(result);
	}
}