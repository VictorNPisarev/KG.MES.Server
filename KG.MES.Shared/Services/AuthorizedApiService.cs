using System.Net.Http.Headers;
using KG.MES.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KG.MES.Shared.Services;

public abstract class AuthorizedApiService
{
	protected readonly HttpClient httpClient;
	private readonly IServiceProvider serviceProvider;

	protected AuthorizedApiService(HttpClient httpClient, IServiceProvider serviceProvider)
	{
		this.httpClient = httpClient;
		this.serviceProvider = serviceProvider;
	}

	protected void EnsureAuthorization()
	{
		var session = serviceProvider.GetService<UserSessionService>();

		if (session != null && !string.IsNullOrEmpty(session.AccessToken))
		{
			httpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", session.AccessToken);
		}
		else
		{
			httpClient.DefaultRequestHeaders.Authorization = null;
		}
	}
}