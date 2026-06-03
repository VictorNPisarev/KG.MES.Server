using KG.MES.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KG.MES.Server.Tests;

public static class TestStartup
{
	public static void ConfigureTestDatabase(IServiceCollection services)
	{
		services.AddDbContext<AppDbContext>(options =>
			options.UseInMemoryDatabase("TestDb"));
	}
}