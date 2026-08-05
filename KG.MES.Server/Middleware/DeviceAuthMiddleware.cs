using KG.MES.Server.Data;
using KG.MES.Server.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Middleware;

public class DeviceAuthMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<DeviceAuthMiddleware> _logger;

	public DeviceAuthMiddleware(RequestDelegate next, ILogger<DeviceAuthMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
	{
		// Пропускаем запросы активации
		if (context.Request.Path.Value?.Contains("/api/activation") == true ||
			context.Request.Path.Value?.Contains("/api/auth/login") == true ||
			context.Request.Path.Value?.Contains("/api/auth/register") == true)
		{
			await _next(context);
			return;
		}

		// Пропускаем запросы без авторизации (если есть)
		var token = context.Request.Headers["Authorization"].FirstOrDefault();
		if (string.IsNullOrEmpty(token))
		{
			await _next(context);
			return;
		}

		// Проверяем DeviceId
		var deviceId = context.Request.Headers["X-Device-Id"].FirstOrDefault();

		// Получаем userId из контекста (устанавливается JWT middleware)
		var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
		{
			await _next(context);
			return;
		}

		using var scope = serviceProvider.CreateScope();
		var userDeviceService = scope.ServiceProvider.GetRequiredService<UserDeviceService>();
		var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// ============================================================
		// 1. ЕСЛИ НЕТ DEVICE ID — ПРОПУСКАЕМ (обратная совместимость)
		// ============================================================
		if (string.IsNullOrEmpty(deviceId))
		{
			_logger.LogWarning(
				"⚠️ Request without DeviceId from user {UserId} to {Path}. " +
				"This is allowed during migration period.",
				userId, context.Request.Path);

			// Логируем для отслеживания (можно добавить отдельную таблицу)
			//await LogMissingDeviceAsync(dbContext, userId, context);

			await _next(context);
			return;
		}

		// ============================================================
		// 2. ПРОВЕРЯЕМ, ВКЛЮЧЕНА ЛИ ПРОВЕРКА
		// ============================================================
		var isDeviceCheckEnabled = await userDeviceService.IsDeviceCheckEnabledAsync(userId);

		if (!isDeviceCheckEnabled)
		{
			// Проверка НЕ включена — сохраняем DeviceId, но не блокируем
			await userDeviceService.RegisterDeviceAsync(userId, deviceId,
				context.Request.Headers["X-Device-Name"]);

			_logger.LogDebug("ℹ️ Device {DeviceId} saved for user {UserId} (check disabled)",
				deviceId, userId);

			await _next(context);
			return;
		}

		// ============================================================
		// 3. ПРОВЕРКА ВКЛЮЧЕНА — ПРОВЕРЯЕМ УСТРОЙСТВО
		// ============================================================
		var existingDevice = await dbContext.UserDevices
		.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);

		//устройство не найдено - проверяю ActivationKey в запросе - возможно это регистрация устройства
		if (existingDevice == null)
		{
			existingDevice = await userDeviceService.RegisterDeviceAsync(userId, deviceId,
				context.Request.Headers["X-Device-Name"], context.Request.Headers["X-Device-ActivationKey"]);

			_logger.LogDebug("ℹ️ Device {DeviceId} saved for user {UserId} (check disabled)",
				deviceId, userId);
		}

		var isValid = existingDevice.IsActive && !string.IsNullOrEmpty(existingDevice.ActivationKey);

		//устройство уже использовалось, но либо отозван ключ, либо несанкционированный экземпляр приложения (без связки ключ-deviceId)
		if (!isValid)
		{
			// Устройство НЕ НАЙДЕНО или неактивно
			_logger.LogWarning(
				"⚠️ Unknown device {DeviceId} for user {UserId} (check enabled)",
				deviceId, userId);

			// Проверка включена — сохраняю DeviceId, но блокирую запрос
			await userDeviceService.RegisterDeviceAsync(userId, deviceId,
				context.Request.Headers["X-Device-Name"]);

			// Отправляем уведомление админу
			await NotifyAdminAsync(dbContext, userId, deviceId, context);

			// ============================================================
			// ⚠️ ВРЕМЕННО: НЕ БЛОКИРУЕМ, ТОЛЬКО ЛОГИРУЕМ
			// ============================================================
			// TODO: Раскомментировать когда все клиенты перейдут на DeviceId
			// context.Response.StatusCode = 403;
			// await context.Response.WriteAsync("Access denied: device not registered");
			// return;

			// Пока пропускаем с предупреждением
			await _next(context);
			return;
		}

		// ✅ Устройство найдено
		_logger.LogDebug("✅ Device {DeviceId} verified for user {UserId}", deviceId, userId);
		await _next(context);
	}


	private async Task NotifyAdminAsync(AppDbContext dbContext, Guid userId, string deviceId, HttpContext context)
	{
		// TODO: Реализовать уведомление
		_logger.LogWarning(
			"🚨 UNKNOWN DEVICE ALERT\n" +
			"User: {UserId}\n" +
			"Device: {DeviceId}\n" +
			"IP: {IP}\n" +
			"Time: {Time}",
			userId, deviceId,
			context.Connection.RemoteIpAddress?.ToString(),
			DateTime.UtcNow);
	}
}