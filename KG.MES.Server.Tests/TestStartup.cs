using KG.MES.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KG.MES.Shared.Tests;

public static class TestStartup
{
	public static void ConfigureTestDatabase(IServiceCollection services)
	{
		services.AddDbContext<AppDbContext>(options =>
			options.UseInMemoryDatabase("TestDb"));
	}
}