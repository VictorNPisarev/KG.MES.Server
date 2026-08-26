using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using KG.MES.Shared.Data;
using KG.MES.Shared.Extensions;
using KG.MES.Shared.Hubs;
using KG.MES.Shared.Services;
using KG.MES.Shared.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Регистрация DbContext
ConfigureDatabase(builder.Services, builder.Configuration);

// Инициализация часового пояса через конфиг
DateTimeExtensions.Initialize(builder.Configuration);

// Регистрация API сервисов
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISupplyService, SupplyService>();
builder.Services.AddScoped<IWorkplaceService, WorkplaceService>();
builder.Services.AddScoped<OrderAttributeService>();

// Добавляем контроллеры с настройкой JSON (игнорировать циклы)
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
		options.JsonSerializerOptions.WriteIndented = true;
	});

builder.Services.AddScoped<LeadTimeCalculationService>(); 
builder.Services.AddHttpClient();

// Добавляем SignalR
builder.Services.AddSignalR();

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS для доступа с любых устройств
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()   //Оставляю пока не реализована авторизация. Для SignalR с авторизацией нужно будет WithOrigins(...)
			  .AllowAnyMethod()
			  .AllowAnyHeader();
			//  .AllowCredentials(); // ← ВАЖНО для SignalR! (но когда будет авторизация)
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors("AllowAll");
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

// Инициализация NotificationHelper (после app.Build())
var hubContext = app.Services.GetRequiredService<IHubContext<NotificationHub>>();
NotificationHelper.Initialize(hubContext);


app.MapControllers();

app.MapHub<NotificationHub>("/notificationHub");

app.Run();

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
	var connectionString = configuration.GetConnectionString("DefaultConnection")
	?? "Host=localhost;Port=5432;Database=KgMes;Username=postgres;Password=postgres";

	services.AddDbContext<AppDbContext>(options =>
		options.UseNpgsql(connectionString));
}

public partial class Program { }