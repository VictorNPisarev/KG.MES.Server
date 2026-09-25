using System.Net.Http.Headers;
using KG.MES.Shared.Services;

namespace KG.MES.Shared.Services;

public abstract class AuthorizedApiService
{
	protected readonly HttpClient httpClient;
	protected readonly UserSessionService session;

	protected AuthorizedApiService(HttpClient httpClient, UserSessionService session)
	{
		this.httpClient = httpClient;
		this.session = session;
	}

	protected void EnsureAuthorization()
	{
		if (!string.IsNullOrEmpty(session.AccessToken))
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