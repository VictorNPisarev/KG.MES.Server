using KG.MES.Server.Models.Dto;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public partial class AuthController : ControllerBase
{
	[HttpPost("login")]
	public Task<IActionResult> Login([FromBody] LoginRequestDto request) => LoginHandler(request);

	[HttpPost("refresh")]
	public Task<IActionResult> Refresh([FromBody] RefreshRequestDto request) => RefreshHandler(request);

}