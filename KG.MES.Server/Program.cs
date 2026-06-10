using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using KG.MES.Server.Data;
using KG.MES.Server.Hubs;
using KG.MES.Server.Services;
using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Регистрируем DbContext
ConfigureDatabase(builder.Services, builder.Configuration);

// Регистрация API сервисов
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISupplyService, SupplyService>();
builder.Services.AddScoped<IWorkplaceService, WorkplaceService>();

// Добавляем контроллеры с настройкой JSON (игнорировать циклы)
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
		options.JsonSerializerOptions.WriteIndented = true;
	});

// Добавляем SignalR
//builder.Services.AddSignalR();

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS для доступа с любых устройств
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader();
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Инициализация NotificationHelper (после app.Build())
//var hubContext = app.Services.GetRequiredService<IHubContext<NotificationHub>>();
//NotificationHelper.Initialize(hubContext);

app.UseCors("AllowAll");

app.MapControllers();

//app.MapHub<NotificationHub>("/notificationHub");

app.Run();

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
	var connectionString = GetConnectionString();
	services.AddDbContext<AppDbContext>(options =>
		options.UseNpgsql(connectionString));
}

static string GetConnectionString()
{
	// Чтение из .env или переменных окружения
	var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
	var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
	var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "KgMes";
	var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
	var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "x126ko33";

	return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

public partial class Program { }