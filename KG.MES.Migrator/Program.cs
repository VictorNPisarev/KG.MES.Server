using System.Globalization;
using System.Text.Json;
using Npgsql;
using Dapper;

namespace KG.MES.Migrator;

class Program
{
	private const string GasUrl = "https://script.google.com/macros/s/AKfycbzoDyvGU4ZHKg4oy1rGmxvxLTfnMATV21eYUzTFsj4pTxz3ii3sqw-i6fk5vElvrqBR-w/exec";

	private static readonly Dictionary<string, string> WorkplaceMap = new();
	private static readonly Dictionary<string, string> RoleMap = new();
	private static readonly Dictionary<string, string> UserMap = new();
	private static readonly Dictionary<string, string> OrderMap = new();
	private static readonly Dictionary<string, string> ProductionOrderMap = new();

	static async Task Main(string[] args)
	{
		var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
							"Host=localhost;Port=5432;Database=KgMes;Username=postgres;Password=x126ko33";

		await using var conn = new NpgsqlConnection(connectionString);
		await conn.OpenAsync();

		Console.WriteLine("🚀 Начало миграции данных...\n");

		try
		{
			await TruncateTables(conn);

			var workplaces = await FetchTable("Workplaces");
			var roles = await FetchTable("Roles");
			var users = await FetchTable("Users");
			var userWorkplaces = await FetchTable("UserWorkplaces");
			var workplaceTransitions = await FetchTable("WorkplaceTransitions");
			var orders = await FetchTable("Orders");
			var prodOrders = await FetchTable("ProductionOrders");
			var footprints = await FetchTable("OrderFootprints");
			var logs = await FetchTable("OperationLogs");
			var bomFlags = await FetchTable("BomFlags");
			Console.WriteLine($"\n🎉 bomFlags: {bomFlags.Count}");

			await MigrateRoles(conn, roles);
			await MigrateWorkplaces(conn, workplaces);
			await MigrateUsers(conn, users);
			await MigrateUserWorkplaces(conn, userWorkplaces);
			await MigrateWorkplaceRelations(conn, workplaceTransitions);
			await MigrateOrders(conn, orders);
			await MigrateProductionOrders(conn, prodOrders);
			await MigrateOrderFootprints(conn, footprints);
			await MigrateOperationLogs(conn, logs);

			// 1. Сначала создаём order_supply и supply_items для всех заказов
			await CreateOrderSupplyForAllOrders(conn);
			// 2. Потом обновляем статусы из BomFlags
			await MigrateBomFlags(conn, bomFlags);
			
			Console.WriteLine("\n🎉 Миграция завершена!");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"❌ Ошибка: {ex.Message}");
			Console.WriteLine(ex.StackTrace);
		}
	}

	private static readonly Dictionary<string, string> TableNameMapping = new()
	{
		{ "workplaces", "Workplaces" },
		{ "roles", "Roles" },
		{ "users", "Users" },
		{ "user_workplaces", "UserWorkplaces" },
		{ "workplace_relations", "WorkplaceTransitions" },
		{ "orders", "Orders" },
		{ "production_orders", "ProductionOrders" },
		{ "order_footprints", "OrderFootprints" },
		{ "operation_logs", "OperationLogs" },
		{ "operation_statistics", "OperationStatistics" },
		{ "bom_flags", "BomFlags" }
	};

	static async Task<List<Dictionary<string, object>>> FetchTable(string tableName)
	{
		// Маппинг имени таблицы для GAS
		var gasTableName = TableNameMapping.FirstOrDefault(x => x.Value == tableName).Key;
		if (string.IsNullOrEmpty(gasTableName))
		{
			Console.WriteLine($"⚠️ Неизвестная таблица: {tableName}");
			return new List<Dictionary<string, object>>();
		}

		Console.WriteLine($"📥 Загрузка {tableName} (GAS: {gasTableName})...");

		using var httpClient = new HttpClient();
		var url = $"{GasUrl}?action=exportData&table={gasTableName}";
		var response = await httpClient.GetStringAsync(url);

		var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(response);

		Console.WriteLine($"✅ Загружено {data?.Count} записей");
		return data ?? new List<Dictionary<string, object>>();
	}
	static async Task TruncateTables(NpgsqlConnection conn)
	{
		Console.WriteLine("🗑️ Очистка таблиц...");

		// Проверяем, существует ли таблица
		var checkTableSql = @"
		SELECT EXISTS (
			SELECT FROM information_schema.tables 
			WHERE table_schema = 'public' AND table_name = 'Workplaces'
		);";

		await using var checkCmd = new NpgsqlCommand(checkTableSql, conn);
		var tableExists = (bool)await checkCmd.ExecuteScalarAsync();

		//if (!tableExists)
		//{
		//	Console.WriteLine("⚠️ Таблицы ещё не созданы, пропускаем очистку");
		//	return;
		//}

		var sql = @"
		TRUNCATE TABLE operation_logs CASCADE;
		TRUNCATE TABLE order_footprints CASCADE;
		TRUNCATE TABLE production_orders CASCADE;
		TRUNCATE TABLE user_workplaces CASCADE;
		TRUNCATE TABLE workplace_transitions CASCADE;
		TRUNCATE TABLE users CASCADE;
		TRUNCATE TABLE orders CASCADE;
		TRUNCATE TABLE workplaces CASCADE;
		TRUNCATE TABLE roles CASCADE;
		TRUNCATE TABLE supply_items CASCADE; 
		TRUNCATE TABLE order_supply CASCADE;";

		await using var cmd = new NpgsqlCommand(sql, conn);
		await cmd.ExecuteNonQueryAsync();
		Console.WriteLine("✅ Таблицы очищены");
	}

	static async Task MigrateWorkplaces(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("🏭 Миграция рабочих мест...");

		foreach (var wp in data)
		{
			var oldId = wp["Row ID"].ToString();
			var newId = Guid.NewGuid();
			WorkplaceMap[oldId] = newId.ToString();
		}

		foreach (var wp in data)
		{
			var oldId = wp["Row ID"].ToString();
			var newId = Guid.Parse(WorkplaceMap[oldId]);
			var oldPrevId = wp.GetValueOrDefault("Предыдущий участок")?.ToString();
			var newPrevId = !string.IsNullOrEmpty(oldPrevId) && WorkplaceMap.ContainsKey(oldPrevId) ? Guid.Parse(WorkplaceMap[oldPrevId]) : (Guid?)null;
			var isWorkplaceStr = wp.GetValueOrDefault("Участок производства")?.ToString();
			var isWorkplace = isWorkplaceStr == "true" || isWorkplaceStr == "Y" || isWorkplaceStr == "True";
			var name = wp.GetValueOrDefault("Статус")?.ToString() ?? "";

			var sql = @"
				INSERT INTO workplaces (id, legacy_id, name, previous_workplace_id, is_workplace, created_at, updated_at)
				VALUES ($1, $2, $3, $4, $5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";

			await using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue(newId);
			cmd.Parameters.AddWithValue(oldId);
			cmd.Parameters.AddWithValue(name);
			cmd.Parameters.AddWithValue(newPrevId ?? (object)DBNull.Value);
			cmd.Parameters.AddWithValue(isWorkplace);
			await cmd.ExecuteNonQueryAsync();
		}

		Console.WriteLine($"✅ Добавлено {data.Count} рабочих мест");
	}

	static async Task MigrateRoles(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("👥 Миграция ролей...");

		foreach (var role in data)
		{
			var oldId = role["Row ID"].ToString();
			var newId = Guid.NewGuid();
			RoleMap[oldId] = newId.ToString();
		}

		foreach (var role in data)
		{
			var oldId = role["Row ID"].ToString();
			var newId = Guid.Parse(RoleMap[oldId]);

			var name = role.GetValueOrDefault("Роль")?.ToString() ?? "";
			var description = role.GetValueOrDefault("Описание")?.ToString() ?? "";
			var level = name switch
			{
				"admin" => 100,
				"advanced" => 70,
				"middle" => 40,
				_ => 10
			};

			var sql = @"
				INSERT INTO roles (id, legacy_id, name, description, level, created_at, updated_at)
				VALUES ($1, $2, $3, $4, $5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";

			await using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue(newId);
			cmd.Parameters.AddWithValue(oldId);
			cmd.Parameters.AddWithValue(name);
			cmd.Parameters.AddWithValue(description);
			cmd.Parameters.AddWithValue(level);
			await cmd.ExecuteNonQueryAsync();
		}

		Console.WriteLine($"✅ Добавлено {data.Count} ролей");
	}

	static async Task MigrateUsers(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("👤 Миграция пользователей...");

		foreach (var user in data)
		{
			var oldId = user["Row ID"].ToString();
			var newId = Guid.NewGuid();
			UserMap[oldId] = newId.ToString();
		}

		foreach (var user in data)
		{
			var oldId = user["Row ID"].ToString();
			var newId = Guid.Parse(UserMap[oldId]);

			var email = user.GetValueOrDefault("Email")?.ToString() ?? "";
			var name = user.GetValueOrDefault("Наименование")?.ToString() ?? "";
			var roleOldId = user.GetValueOrDefault("Роль")?.ToString();
			var roleId = !string.IsNullOrEmpty(roleOldId) && RoleMap.ContainsKey(roleOldId) ? Guid.Parse(RoleMap[roleOldId]) : (Guid?)null;

			var sql = @"
				INSERT INTO users (id, legacy_id, email, name, role_id, created_at, updated_at)
				VALUES ($1, $2, $3, $4, $5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";

			await using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue(newId);
			cmd.Parameters.AddWithValue(oldId);
			cmd.Parameters.AddWithValue(email);
			cmd.Parameters.AddWithValue(name);
			cmd.Parameters.AddWithValue(roleId ?? (object)DBNull.Value);
			await cmd.ExecuteNonQueryAsync();
		}

		Console.WriteLine($"✅ Добавлено {data.Count} пользователей");
	}

	static async Task MigrateUserWorkplaces(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("🔗 Миграция привязки пользователей...");

		var count = 0;
		foreach (var uw in data)
		{
			var newId = Guid.NewGuid();
			var userIdOld = uw.GetValueOrDefault("UserId")?.ToString();
			var workplaceIdOld = uw.GetValueOrDefault("WorkplaceId")?.ToString();

			var userId = !string.IsNullOrEmpty(userIdOld) && UserMap.ContainsKey(userIdOld) ? Guid.Parse(UserMap[userIdOld]) : (Guid?)null;
			var workplaceId = !string.IsNullOrEmpty(workplaceIdOld) && WorkplaceMap.ContainsKey(workplaceIdOld) ? Guid.Parse(WorkplaceMap[workplaceIdOld]) : (Guid?)null;

			if (userId != null && workplaceId != null)
			{
				var sql = @"
					INSERT INTO user_workplaces (id, legacy_id, user_id, workplace_id, created_at)
					VALUES ($1, $2, $3, $4, CURRENT_TIMESTAMP)
					ON CONFLICT (user_id, workplace_id) DO NOTHING";

				await using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue(newId);
				cmd.Parameters.AddWithValue(uw["Row ID"].ToString());
				cmd.Parameters.AddWithValue(userId.Value);
				cmd.Parameters.AddWithValue(workplaceId.Value);
				await cmd.ExecuteNonQueryAsync();
				count++;
			}
		}

		Console.WriteLine($"✅ Добавлено {count} привязок");
	}

	static async Task MigrateWorkplaceRelations(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("🔀 Миграция связей между участками...");

		var count = 0;
		foreach (var rel in data)
		{
			var rowId = rel.GetValueOrDefault("WorkplaceRelationId")?.ToString();
			if (string.IsNullOrEmpty(rowId))
			{
				Console.WriteLine("⚠️ Пропущена запись: отсутствует WorkplaceRelationId");
				continue;
			}

			var fromIdOld = rel.GetValueOrDefault("WorkplaceFromId")?.ToString();
			var toIdOld = rel.GetValueOrDefault("WorkplaceToId")?.ToString();

			if (string.IsNullOrEmpty(fromIdOld) || string.IsNullOrEmpty(toIdOld))
			{
				Console.WriteLine($"⚠️ Пропущена запись {rowId}: отсутствуют WorkplaceFromId или WorkplaceToId");
				continue;
			}

			if (!WorkplaceMap.ContainsKey(fromIdOld))
			{
				Console.WriteLine($"⚠️ Пропущена запись {rowId}: from_workplace {fromIdOld} не найден в маппинге");
				continue;
			}

			if (!WorkplaceMap.ContainsKey(toIdOld))
			{
				Console.WriteLine($"⚠️ Пропущена запись {rowId}: to_workplace {toIdOld} не найден в маппинге");
				continue;
			}

			var newId = Guid.NewGuid();
			var fromId = Guid.Parse(WorkplaceMap[fromIdOld]);
			var toId = Guid.Parse(WorkplaceMap[toIdOld]);

			var sql = @"
				INSERT INTO workplace_transitions (id, legacy_id, from_workplace_id, to_workplace_id, transition_type, created_at)
				VALUES ($1, $2, $3, $4, $5, CURRENT_TIMESTAMP)
				ON CONFLICT (from_workplace_id, to_workplace_id) DO NOTHING";

			await using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue(newId);
			cmd.Parameters.AddWithValue(rowId);
			cmd.Parameters.AddWithValue(fromId);
			cmd.Parameters.AddWithValue(toId);
			cmd.Parameters.AddWithValue("sequential");
			await cmd.ExecuteNonQueryAsync();
			count++;
		}

		Console.WriteLine($"✅ Добавлено {count} связей");
	}

	static async Task MigrateOrders(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("📦 Миграция заказов...");

		foreach (var order in data)
		{
			var oldId = order["Row ID"].ToString();
			var newId = Guid.NewGuid();
			OrderMap[oldId] = newId.ToString();
		}

		foreach (var order in data)
		{
			var oldId = order["Row ID"].ToString();
			var newId = Guid.Parse(OrderMap[oldId]);

			var orderNumber = order.GetValueOrDefault("Номер заказа")?.ToString() ?? "";
			var readyDate = order.GetValueOrDefault("Готовность") != null ? DateTime.Parse(order["Готовность"].ToString()) : (DateTime?)null;

			var windowCount = int.Parse(order.GetValueOrDefault("Окна, шт")?.ToString() ?? "0");
			var windowArea = ParseDecimal(order.GetValueOrDefault("Окна, м2")?.ToString());
			var plateCount = int.Parse(order.GetValueOrDefault("Щитовые, шт")?.ToString() ?? "0");
			var plateArea = ParseDecimal(order.GetValueOrDefault("Щитовые, м2")?.ToString());

			var isEconom = order.GetValueOrDefault("Эконом")?.ToString() == "true";
			var isClaim = order.GetValueOrDefault("Рекламация")?.ToString() == "true";
			var isOnlyPaid = order.GetValueOrDefault("Оплачен")?.ToString() == "true";

			var sql = @"
				INSERT INTO orders (id, legacy_id, order_number, ready_date, 
					window_count, window_area, plate_count, plate_area,
					is_econom, is_claim, is_only_paid, created_at, updated_at)
				VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";

			await using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue(newId);
			cmd.Parameters.AddWithValue(oldId);
			cmd.Parameters.AddWithValue(orderNumber);
			cmd.Parameters.AddWithValue(readyDate ?? (object)DBNull.Value);
			cmd.Parameters.AddWithValue(windowCount);
			cmd.Parameters.AddWithValue(windowArea);
			cmd.Parameters.AddWithValue(plateCount);
			cmd.Parameters.AddWithValue(plateArea);
			cmd.Parameters.AddWithValue(isEconom);
			cmd.Parameters.AddWithValue(isClaim);
			cmd.Parameters.AddWithValue(isOnlyPaid);
			await cmd.ExecuteNonQueryAsync();
		}

		Console.WriteLine($"✅ Добавлено {data.Count} заказов");
	}

	static async Task MigrateProductionOrders(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("🏭 Миграция производственных заказов...");

		foreach (var po in data)
		{
			var oldId = po["Row ID"].ToString();
			var newId = Guid.NewGuid();
			ProductionOrderMap[oldId] = newId.ToString();
		}

		foreach (var po in data)
		{
			var oldId = po["Row ID"].ToString();
			var newId = Guid.Parse(ProductionOrderMap[oldId]);

			var orderIdOld = po.GetValueOrDefault("ID заказа")?.ToString();
			var orderId = !string.IsNullOrEmpty(orderIdOld) && OrderMap.ContainsKey(orderIdOld) ? Guid.Parse(OrderMap[orderIdOld]) : (Guid?)null;
			if (orderId == null) continue;

			var workplaceIdOld = po.GetValueOrDefault("ID статуса")?.ToString();
			var workplaceId = !string.IsNullOrEmpty(workplaceIdOld) && WorkplaceMap.ContainsKey(workplaceIdOld) ? Guid.Parse(WorkplaceMap[workplaceIdOld]) : (Guid?)null;
			var comment = po.GetValueOrDefault("Примечания")?.ToString() ?? "";
			var lumber = po.GetValueOrDefault("Брус")?.ToString() ?? "";
			var glazingBead = po.GetValueOrDefault("Штапик")?.ToString() ?? "";
			var isTwoSidePaint = po.GetValueOrDefault("Двухсторонняя покраска")?.ToString() == "true";

			var sql = @"
				INSERT INTO production_orders (id, legacy_id, order_id, current_workplace_id, 
					comment, lumber, glazing_bead, is_two_side_paint, created_at, updated_at)
				VALUES ($1, $2, $3, $4, $5, $6, $7, $8, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";

			await using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue(newId);
			cmd.Parameters.AddWithValue(oldId);
			cmd.Parameters.AddWithValue(orderId.Value);
			cmd.Parameters.AddWithValue(workplaceId ?? (object)DBNull.Value);
			cmd.Parameters.AddWithValue(comment);
			cmd.Parameters.AddWithValue(lumber);
			cmd.Parameters.AddWithValue(glazingBead);
			cmd.Parameters.AddWithValue(isTwoSidePaint);
			await cmd.ExecuteNonQueryAsync();
		}

		Console.WriteLine($"✅ Добавлено {data.Count} производственных заказов");
	}

	static async Task MigrateOrderFootprints(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("👣 Миграция следов заказов...");

		var count = 0;
		foreach (var fp in data)
		{
			var prodOrderIdOld = fp.GetValueOrDefault("OrderInProductId")?.ToString();
			var prodOrderId = !string.IsNullOrEmpty(prodOrderIdOld) && ProductionOrderMap.ContainsKey(prodOrderIdOld) ? Guid.Parse(ProductionOrderMap[prodOrderIdOld]) : (Guid?)null;

			var workplaceIdOld = fp.GetValueOrDefault("WorkplaceId")?.ToString();
			var workplaceId = !string.IsNullOrEmpty(workplaceIdOld) && WorkplaceMap.ContainsKey(workplaceIdOld) ? Guid.Parse(WorkplaceMap[workplaceIdOld]) : (Guid?)null;

			if (prodOrderId != null && workplaceId != null)
			{
				var newId = Guid.NewGuid();
				var sql = @"
					INSERT INTO order_footprints (id, legacy_id, production_order_id, workplace_id, status, created_at, updated_at)
					VALUES ($1, $2, $3, $4, $5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
					ON CONFLICT (production_order_id, workplace_id) DO NOTHING";

				await using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue(newId);
				cmd.Parameters.AddWithValue(fp["Row ID"].ToString());
				cmd.Parameters.AddWithValue(prodOrderId.Value);
				cmd.Parameters.AddWithValue(workplaceId.Value);
				cmd.Parameters.AddWithValue(fp.GetValueOrDefault("Status")?.ToString() ?? "planned");
				await cmd.ExecuteNonQueryAsync();
				count++;
			}
		}

		Console.WriteLine($"✅ Добавлено {count} следов");
	}

	static async Task MigrateOperationLogs(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("📝 Миграция логов операций...");

		var count = 0;
		foreach (var log in data)
		{
			var prodOrderIdOld = log.GetValueOrDefault("OrderInProductId")?.ToString();
			var prodOrderId = !string.IsNullOrEmpty(prodOrderIdOld) && ProductionOrderMap.ContainsKey(prodOrderIdOld) ? Guid.Parse(ProductionOrderMap[prodOrderIdOld]) : (Guid?)null;

			var workplaceIdOld = log.GetValueOrDefault("WorkplaceId")?.ToString();
			var workplaceId = !string.IsNullOrEmpty(workplaceIdOld) && WorkplaceMap.ContainsKey(workplaceIdOld) ? Guid.Parse(WorkplaceMap[workplaceIdOld]) : (Guid?)null;

			var userIdOld = log.GetValueOrDefault("UserId")?.ToString();
			var userId = !string.IsNullOrEmpty(userIdOld) && UserMap.ContainsKey(userIdOld) ? Guid.Parse(UserMap[userIdOld]) : (Guid?)null;

			if (prodOrderId != null && workplaceId != null)
			{
				var newId = Guid.NewGuid();
				var sql = @"
					INSERT INTO operation_logs (id, legacy_id, production_order_id, workplace_id, user_id, 
						operation_type, operation_time, notes, source, created_at)
					VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, CURRENT_TIMESTAMP)";

				await using var cmd = new NpgsqlCommand(sql, conn);
				cmd.Parameters.AddWithValue(newId);
				cmd.Parameters.AddWithValue(log["Row ID"].ToString());
				cmd.Parameters.AddWithValue(prodOrderId.Value);
				cmd.Parameters.AddWithValue(workplaceId.Value);
				cmd.Parameters.AddWithValue(userId ?? (object)DBNull.Value);
				cmd.Parameters.AddWithValue(log.GetValueOrDefault("OperationType")?.ToString() ?? "");
				cmd.Parameters.AddWithValue(DateTime.TryParse(log.GetValueOrDefault("OperationDateTime")?.ToString(), out var dt) ? dt : DateTime.UtcNow);
				cmd.Parameters.AddWithValue(log.GetValueOrDefault("Notes")?.ToString() ?? "");
				cmd.Parameters.AddWithValue(log.GetValueOrDefault("Source")?.ToString() ?? "migration");
				await cmd.ExecuteNonQueryAsync();
				count++;
			}
		}

		Console.WriteLine($"✅ Добавлено {count} логов");
	}

	static async Task CreateOrderSupplyForAllOrders(NpgsqlConnection conn)
	{
		Console.WriteLine("🏭 Создание order_supply для всех заказов...");

		// Получаем все заказы
		var orders = await conn.QueryAsync<Guid>("SELECT id FROM orders");
		var orderIds = orders.ToList();

		// Получаем все типы материалов
		var supplyTypes = await conn.QueryAsync<(Guid id, string name)>("SELECT id, name FROM supply_types");
		var supplyTypeList = supplyTypes.ToList();

		var count = 0;

		foreach (var orderId in orderIds)
		{
			// Проверяем, существует ли order_supply
			var exists = await conn.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM order_supply WHERE order_id = @orderId", new { orderId }) > 0;

			if (!exists)
			{
				var orderSupplyId = Guid.NewGuid();

				// Создаём order_supply
				await conn.ExecuteAsync(@"
				INSERT INTO order_supply (id, order_id, created_at, updated_at)
				VALUES (@id, @orderId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
					new { id = orderSupplyId, orderId });

				// Создаём supply_items для каждого типа
				foreach (var supplyType in supplyTypeList)
				{
					await conn.ExecuteAsync(@"
					INSERT INTO supply_items (id, order_supply_id, supply_type_id, created_at, updated_at)
					VALUES (gen_random_uuid(), @orderSupplyId, @supplyTypeId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
						new { orderSupplyId, supplyTypeId = supplyType.id });
				}

				count++;
				if (count % 100 == 0) Console.WriteLine($"   Обработано {count} заказов...");
			}
		}

		Console.WriteLine($"✅ Создано order_supply для {count} заказов");
	}

	static async Task MigrateBomFlags(NpgsqlConnection conn, List<Dictionary<string, object>> data)
	{
		Console.WriteLine("🚚 Обновление статусов из BomFlags...");

		// Получаем ID статусов
		var statusMap = new Dictionary<string, Guid>();
		var statusResult = await conn.QueryAsync<(Guid id, string condition_code)>("SELECT id, condition_code FROM supply_conditions");
		foreach (var status in statusResult)
		{
			statusMap[status.condition_code] = status.id;
		}

		var count = 0;

		foreach (var bom in data)
		{
			var orderIdOld = bom.GetValueOrDefault("ordersToDoId")?.ToString();
			if (string.IsNullOrEmpty(orderIdOld) || !OrderMap.ContainsKey(orderIdOld))
			{
				continue;
			}

			var orderId = Guid.Parse(OrderMap[orderIdOld]);

			// Получаем order_supply_id
			var orderSupplyId = await conn.ExecuteScalarAsync<Guid>(
				"SELECT id FROM order_supply WHERE order_id = @orderId", new { orderId });

			if (orderSupplyId == Guid.Empty) continue;

			// Обновляем lumber
			var lumberStatus = GetLumberStatus(bom);
			if (lumberStatus != null && statusMap.ContainsKey(lumberStatus))
			{
				await conn.ExecuteAsync(@"
				UPDATE supply_items 
				SET condition_id = @conditionId, updated_at = CURRENT_TIMESTAMP
				WHERE order_supply_id = @orderSupplyId AND supply_type_id = 
					(SELECT id FROM supply_types WHERE name = 'lumber')",
					new { orderSupplyId, conditionId = statusMap[lumberStatus] });
			}

			// Обновляем furniture
			var furnitureStatus = GetBoolStatus(bom.GetValueOrDefault("Фурнитура"));
			if (furnitureStatus == true && statusMap.ContainsKey("in_stock"))
			{
				await conn.ExecuteAsync(@"
				UPDATE supply_items 
				SET condition_id = @conditionId, updated_at = CURRENT_TIMESTAMP
				WHERE order_supply_id = @orderSupplyId AND supply_type_id = 
					(SELECT id FROM supply_types WHERE name = 'furniture')",
					new { orderSupplyId, conditionId = statusMap["in_stock"] });
			}

			// Обновляем glass
			var glassStatus = GetBoolStatus(bom.GetValueOrDefault("Стеклопакеты"));
			if (glassStatus == true && statusMap.ContainsKey("in_stock"))
			{
				await conn.ExecuteAsync(@"
				UPDATE supply_items 
				SET condition_id = @conditionId, updated_at = CURRENT_TIMESTAMP
				WHERE order_supply_id = @orderSupplyId AND supply_type_id = 
					(SELECT id FROM supply_types WHERE name = 'glass')",
					new { orderSupplyId, conditionId = statusMap["in_stock"] });
			}

			// Обновляем paint
			var paintStatus = GetBoolStatus(bom.GetValueOrDefault("ЛКМ"));
			if (paintStatus == true && statusMap.ContainsKey("in_stock"))
			{
				await conn.ExecuteAsync(@"
				UPDATE supply_items 
				SET condition_id = @conditionId, updated_at = CURRENT_TIMESTAMP
				WHERE order_supply_id = @orderSupplyId AND supply_type_id = 
					(SELECT id FROM supply_types WHERE name = 'paint')",
					new { orderSupplyId, conditionId = statusMap["in_stock"] });
			}

			// Обновляем alumWaterShield
			var alumStatus = GetBoolStatus(bom.GetValueOrDefault("ППС, В/О"));
			if (alumStatus == true && statusMap.ContainsKey("in_stock"))
			{
				await conn.ExecuteAsync(@"
				UPDATE supply_items 
				SET condition_id = @conditionId, updated_at = CURRENT_TIMESTAMP
				WHERE order_supply_id = @orderSupplyId AND supply_type_id = 
					(SELECT id FROM supply_types WHERE name = 'alumWaterShield')",
					new { orderSupplyId, conditionId = statusMap["in_stock"] });
			}

			count++;
			if (count % 100 == 0) Console.WriteLine($"   Обработано {count} заказов...");
		}

		Console.WriteLine($"✅ Обновлено {count} заказов из BomFlags");
	}
	
	static async Task ProcessMaterial(
		NpgsqlConnection conn,
		Dictionary<string, Guid> typeMap,
		Dictionary<string, Guid> statusMap,
		Guid orderSupplyId,
		string materialName,
		string? condition,
		decimal? quantity,
		string? comment)
	{
		if (!typeMap.ContainsKey(materialName)) return;

		var supplyTypeId = typeMap[materialName];
		var conditionId = !string.IsNullOrEmpty(condition) && statusMap.ContainsKey(condition)
			? statusMap[condition] : (Guid?)null;

		// Проверяем, существует ли запись
		var exists = await conn.ExecuteScalarAsync<int>(
			@"SELECT COUNT(*) FROM supply_items 
		  WHERE order_supply_id = @orderSupplyId AND supply_type_id = @supplyTypeId",
			new { orderSupplyId, supplyTypeId }) > 0;

		if (!exists)
		{
			await conn.ExecuteAsync(@"
			INSERT INTO supply_items (id, order_supply_id, supply_type_id, condition_id, 
				quantity, comment, created_at, updated_at)
			VALUES (gen_random_uuid(), @orderSupplyId, @supplyTypeId, @conditionId, 
				@quantity, @comment, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
				new
				{
					orderSupplyId,
					supplyTypeId,
					conditionId,
					quantity,
					comment
				});
		}
		else
		{
			// Обновляем существующую запись
			await conn.ExecuteAsync(@"
			UPDATE supply_items 
			SET condition_id = @conditionId,
				quantity = @quantity,
				comment = @comment,
				updated_at = CURRENT_TIMESTAMP
			WHERE order_supply_id = @orderSupplyId AND supply_type_id = @supplyTypeId",
				new
				{
					orderSupplyId,
					supplyTypeId,
					conditionId,
					quantity,
					comment
				});
		}
	}

	static string? GetLumberStatus(Dictionary<string, object> bom)
	{
		var inStock = GetBoolStatus(bom.GetValueOrDefault("lumberInStock"));
		var ordered = GetBoolStatus(bom.GetValueOrDefault("lumberOrdered"));

		if (inStock == true ) return "in_stock";
		if (ordered == true) return "ordered";
		return null;
	}

	static bool? GetBoolStatus(object? value)
	{
		if (value == null) return null;
		var str = value.ToString()?.ToLower();
		return str == "true" || str == "y" || str == "yes" || str == "1";
	}
	// Вспомогательный метод для парсинга decimal с запятой
	static decimal ParseDecimal(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return 0;

		// Заменяем запятую на точку
		value = value.Replace(',', '.');

		if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
			return result;

		return 0;
	}
}

public static class DictionaryExtensions
{
	public static object? GetValueOrDefault(this Dictionary<string, object> dict, string key)
	{
		return dict.TryGetValue(key, out var value) ? value : null;
	}
}