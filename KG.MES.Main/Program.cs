using System.Text.Json;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models;
using KG.MES.Main.Models.Xml;
using KG.MES.Main.Services;
using KG.MES.Shared.Helpers;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Models.Config;
using KG.MES.Shared.Serialization;
using KG.MES.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.Cookie.Name = "AuthCookie_Main";
	options.Cookie.Path = "/main";
});

builder.Services.AddSession(options =>
{
	options.Cookie.Name = ".Session.Main";
	options.Cookie.Path = "/main";
});

// Add our custom services
builder.Services.AddScoped<IXmlReaderService, XmlReaderService>();
builder.Services.AddHttpClient<I1CExportService, OneCExportService>();
builder.Services.AddHttpClient<ProductionApiService>();
builder.Services.AddHttpClient<AuthService>();
builder.Services.AddHttpClient<AdminService>();

builder.Services.AddSingleton<SortingIndicatorService>();
builder.Services.AddSingleton<MaterialTypeConfigService>();
builder.Services.AddSingleton<HolidayCalendarService>();
builder.Services.AddSingleton<IMaterialFactory, MaterialFactory>();
builder.Services.AddScoped<IDocumentItemFactory, DocumentItemFactory>();
builder.Services.AddSingleton(LoadViewSettings(builder.Environment));
builder.Services.AddSingleton<SupplyService>();
builder.Services.AddScoped<IEventAggregator, EventAggregator>();
builder.Services.AddScoped<ISocketService, SocketService>();
//builder.Services.AddScoped<ISocketService, SignalRService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<LicenseService>();

var app = builder.Build();

app.UsePathBase("/main");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await LoadDataAsync(app.Services, app.Environment);

app.Run();

async Task LoadDataAsync(IServiceProvider services, IWebHostEnvironment env)
{
	using var scope = services.CreateScope();
	var indicatorService = scope.ServiceProvider.GetRequiredService<SortingIndicatorService>();
	var materialTypeConfigService = scope.ServiceProvider.GetRequiredService<MaterialTypeConfigService>();
	var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

	var indicatorsPath = Path.Combine(env.ContentRootPath, "Data", "sorting_indicators.json");
	var categoriesPath = Path.Combine(env.ContentRootPath, "Data", "indicator_categories.json");
	var materialTypeConfigPath = Path.Combine(env.ContentRootPath, "Data", "material_types_rules.json");

	try
	{
		await indicatorService.LoadAsync(indicatorsPath, categoriesPath);
		await materialTypeConfigService.LoadAsync(materialTypeConfigPath);

		logger.LogInformation("Indicators and categories loaded successfully");
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Failed to load indicators and categories");
	}

	//Загрузка конфига для выделения цветом полей-ярлыков
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

/// Загрузка настроек отображения списка заказов и карточки
OrderViewSettings LoadViewSettings(IWebHostEnvironment env)
{
	var settingsPath = Path.Combine(env.ContentRootPath, "Config", "orderViewSettings.json");
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
