using KG.MES.Server.Data;
using KG.MES.Server.Extensions;
using KG.MES.Shared.Model.Enums;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

/// <summary>
/// Сервис для автоматического добавления в бд типов оповещений при старте сервера. Новый тип достаточно добавить только в enum.
/// </summary>
public class NotificationEventSeeder : IHostedService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<NotificationEventSeeder> _logger;

	public NotificationEventSeeder(
		IServiceProvider serviceProvider,
		ILogger<NotificationEventSeeder> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var existingCodes = await context.NotificationEvents
			.Select(e => e.EventCode)
			.ToListAsync(cancellationToken);

		var allEnums = Enum.GetValues<NotificationEventType>();
		var addedCount = 0;

		foreach (var enumValue in allEnums)
		{
			var code = enumValue.ToCode();

			if (existingCodes.Contains(code))
				continue;

			context.NotificationEvents.Add(new NotificationEvent
			{
				Id = Guid.NewGuid(),
				EventCode = code,
				Description = enumValue.ToDescription()
			});

			_logger.LogInformation("Adding missing notification event: {Code}", code);
			addedCount++;
		}

		if (addedCount > 0)
		{
			await context.SaveChangesAsync(cancellationToken);
			_logger.LogInformation("Notification event seeder: added {Count} events", addedCount);
		}
		else
		{
			_logger.LogInformation("Notification event seeder: all events up to date");
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}