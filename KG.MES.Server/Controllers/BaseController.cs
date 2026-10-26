using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public abstract class BaseController : ControllerBase
{
	protected Guid? CurrentUserId
	{
		get
		{
			var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return Guid.TryParse(claim, out var userId) ? userId : null;
		}
	}

	protected string? CurrentRoleName =>
		User.FindFirst(ClaimTypes.Role)?.Value;
}