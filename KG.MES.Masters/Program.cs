using KG.MES.Shared.Helpers;
using KG.MES.Shared.Services;
using KG.MES.Masters.Components;
using KG.MES.Shared.Models.Config;
using System.Text.Json;
using KG.MES.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureApplicationCookie(options =>
{
	options.Cookie.Name = "AuthCookie_Masters";
	options.Cookie.Path = "/masters";
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
	options.Cookie.Name = ".Session.Masters";
	options.Cookie.Path = "/masters";
});

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient<ProductionApiService>();
builder.Services.AddHttpClient<AuthService>();
builder.Services.AddHttpClient<AdminService>();
builder.Services.AddSingleton(LoadViewSettings());
builder.Services.AddSingleton<SupplyService>();
builder.Services.AddScoped<IEventAggregator, EventAggregator>();
builder.Services.AddScoped<ISocketService, SignalRService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<LicenseService>();

var app = builder.Build();

app.UsePathBase("/masters");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

await LoadDataAsync(app.Services, app.Environment);

app.Run();

async Task LoadDataAsync(IServiceProvider services, IWebHostEnvironment env)
{
	using var scope = services.CreateScope();
	var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

	//Загрузка конфига для выделения цветом полей-ярлыков
	try
	{
		//var baseConfig = Path.Combine(env.ContentRootPath, "..", "KG.MES.Shared", "Config", "BadgeStyles.Base.json");
		var baseConfig = Path.Combine(env.ContentRootPath, "Config", "BadgeStyles.Base.json");
		var appConfig = Path.Combine(env.ContentRootPath, "Config", "BadgeStyles.json");

		logger.LogInformation($"baseConfig: ${baseConfig}");
		logger.LogInformation($"appConfig: ${appConfig}");

		BadgeHelper.LoadConfig(baseConfig, appConfig);

		logger.LogInformation("Badges config loaded successfully");
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Failed to load badges config");
	}
}

/// Загрузка настроек отображения списка заказов и карточки
OrderViewSettings LoadViewSettings()
{
	var settingsPath = Path.Combine(builder.Environment.ContentRootPath, "Config", "orderViewSettings.json");
	if (File.Exists(settingsPath))
	{
		var json = File.ReadAllText(settingsPath);
		var appSettings = JsonSerializer.Deserialize<OrderViewSettings>(json,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new OrderViewSettings();

		return appSettings;
	}
	else
	{
		return new OrderViewSettings();
	}
}