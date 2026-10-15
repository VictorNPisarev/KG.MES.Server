using Microsoft.AspNetCore.Components;

namespace KG.MES.Sales.Components.Pages;

public partial class Registration
{
	[SupplyParameterFromQuery(Name = "email")]
	public string? Email { get; set; }

	[SupplyParameterFromQuery(Name = "token")]
	public string? RegistrationToken { get; set; }

}
