using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public partial class UsersController : ControllerBase
{
	[HttpPost("users/{email}/set-password")]
	public Task<IActionResult> SetPassword(string email, [FromBody] SetPasswordRequestDto request) => SetPasswordHandler(email, request);

	[HttpPost("users/set-password")]
	public Task<IActionResult> SetPasswordCompatible([FromBody] SetPasswordRequestDto request) => SetPasswordHandler(request);

	[HttpGet("users/by-email/{email}")]
	public Task<IActionResult> GetUserByEmail(string email) => GetUserByEmailHandler(email);

	[HttpGet("users/{userId}/workplaces")]
	public Task<IActionResult> GetUserWorkplaces(Guid userId) => GetUserWorkplacesHandler(userId);
}