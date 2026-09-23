using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public partial class UsersController : ControllerBase
{
	#region POST

	[HttpPost("users/{email}/set-password")]
	public Task<IActionResult> SetPassword(string email, [FromBody] SetPasswordRequestDto request) => SetPasswordHandler(email, request);

	[HttpPost("users/set-password")]
	public Task<IActionResult> SetPasswordCompatible([FromBody] SetPasswordRequestDto request) => SetPasswordHandler(request);

	[HttpPost("users/me/change-password")]
	[Authorize]
	public Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
	=> ChangePasswordHandler(request);

	#endregion

	#region GET

	[HttpGet("users/by-email/{email}")]
	public Task<IActionResult> GetUserByEmail(string email) => GetUserByEmailHandler(email);

	[HttpGet("users/{userId}/workplaces")]
	public Task<IActionResult> GetUserWorkplaces(Guid userId) => GetUserWorkplacesHandler(userId);

	#endregion
}