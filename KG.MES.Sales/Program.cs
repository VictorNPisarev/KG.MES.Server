using KG.MES.Sales.Components;
using KG.MES.Shared.Helpers;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Models.Config;
using KG.MES.Shared.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureApplicationCookie(options =>
{
	options.Cookie.Name = "AuthCookie_Sales";
	options.Cookie.Path = "/sales";
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
	options.Cookie.Name = ".Session.Sales";
	options.Cookie.Path = "/sales";
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient<ProductionApiService>();
builder.Services.AddSingleton(LoadViewSettings());
builder.Services.AddScoped<IEventAggregator, EventAggregator>();
//builder.Services.AddScoped<ISocketService, SocketService>();
builder.Services.AddScoped<ISocketService, SignalRService>();
builder.Services.AddHttpClient<AuthService>();

var app = builder.Build();

app.UsePathBase("/sales");

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await LoadDataAsync(app.Services, app.Environment);

app.Run();

async Task LoadDataAsync(IServiceProvider services, IWebHostEnvironment env)
{
	using var scope = services.CreateScope();
	var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

	try
	{
		//var baseConfig = Path.Combine(env.ContentRootPath, "..", "KG.MES.Shared", "Config", "BadgeStyles.Base.json");
		var baseConfig = Path.Combine(env.ContentRootPath, "Config", "BadgeStyles.Base.json");
		var appConfig = Path.Combine(env.ContentRootPath, "Config", "BadgeStyles.json");
		BadgeHelper.LoadConfig(baseConfig, appConfig);
		logger.LogInformation("Badges config loaded successfully");
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Failed to load badges config");
	}
}

OrderViewSettings LoadViewSettings()
{
	var settingsPath = Path.Combine(builder.Environment.ContentRootPath, "Config", "orderViewSettings.json");
	if (File.Exists(settingsPath))
	{
		var json = File.ReadAllText(settingsPath);
		return JsonSerializer.Deserialize<OrderViewSettings>(json,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new OrderViewSettings();
	}
	return new OrderViewSettings();
}