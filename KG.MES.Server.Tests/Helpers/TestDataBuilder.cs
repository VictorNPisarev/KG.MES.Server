using KG.MES.Server.Data;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KG.MES.Server.Tests.Helpers;

public class TestDataBuilder
{
	private readonly List<Role> _roles = [];
	private readonly List<User> _users = [];
	private readonly List<Workplace> _workplaces = [];
	private readonly List<UserWorkplace> _userWorkplaces = [];
	private readonly List<Order> _orders = [];
	private readonly List<ProductionOrder> _productionOrders = [];
	private readonly List<OrderFootprint> _orderFootprints = [];
	private readonly List<WorkplaceTransition> _workplaceTransitions = [];
	private readonly List<Comment> _comments = [];
	private readonly List<OrderSupply> _orderSupplies = [];
	private readonly List<SupplyItem> _supplyItems = [];
	private readonly List<SupplyType> _supplyTypes = [];
	private readonly List<SupplyCondition> _supplyConditions = [];


	public TestDataBuilder WithRole(Action<Role> configure)
	{
		var role = new Role { Id = Guid.NewGuid(), Name = "DefaultRole", Level = 5 };
		configure(role);
		_roles.Add(role);
		return this;
	}

	public TestDataBuilder WithUser(Action<User> configure)
	{
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = "test@example.com",
			Name = "TestUser",
			RoleId = _roles.FirstOrDefault()?.Id ?? Guid.NewGuid()
		};
		configure(user);
		_users.Add(user);
		return this;
	}

	public TestDataBuilder WithWorkplace(Action<Workplace> configure)
	{
		var workplace = new Workplace
		{
			Id = Guid.NewGuid(),
			Name = "DefaultWorkplace",
			IsWorkplace = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		configure(workplace);
		_workplaces.Add(workplace);
		return this;
	}

	public TestDataBuilder WithUserWorkplace(Guid userId, Guid workplaceId)
	{
		_userWorkplaces.Add(new UserWorkplace
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			WorkplaceId = workplaceId,
			CreatedAt = DateTime.UtcNow
		});
		return this;
	}

	public TestDataBuilder WithOrder(Action<Order> configure)
	{
		var order = new Order
		{
			Id = Guid.NewGuid(),
			OrderNumber = "TEST-001",
			ReadyDate = DateTime.UtcNow.AddDays(7),
			WindowCount = 0,
			WindowArea = 0,
			PlateCount = 0,
			PlateArea = 0,
			IsEconom = false,
			IsClaim = false,
			IsOnlyPaid = false,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		configure(order);
		_orders.Add(order);
		return this;
	}

	public TestDataBuilder WithProductionOrder(Action<ProductionOrder> configure)
	{
		var productionOrder = new ProductionOrder
		{
			Id = Guid.NewGuid(),
			OrderId = _orders.LastOrDefault()?.Id ?? Guid.NewGuid(), // Автоматически связываем с последним заказом
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		configure(productionOrder);
		_productionOrders.Add(productionOrder);
		return this;
	}

	public TestDataBuilder WithOrderFootprint(Action<OrderFootprint> configure)
	{
		var footprint = new OrderFootprint
		{
			Id = Guid.NewGuid(),
			ProductionOrderId = _productionOrders.LastOrDefault()?.Id ?? Guid.NewGuid(),
			WorkplaceId = _workplaces.LastOrDefault()?.Id ?? Guid.NewGuid(),
			Status = "planned",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		configure(footprint);
		_orderFootprints.Add(footprint);
		return this;
	}

	public TestDataBuilder WithWorkplaceTransition(Action<WorkplaceTransition> configure)
	{
		var transition = new WorkplaceTransition
		{
			Id = Guid.NewGuid(),
			FromWorkplaceId = _workplaces.FirstOrDefault()?.Id ?? Guid.NewGuid(),
			ToWorkplaceId = _workplaces.LastOrDefault()?.Id ?? Guid.NewGuid(),
			CreatedAt = DateTime.UtcNow,
		};
		configure(transition);
		_workplaceTransitions.Add(transition);
		return this;
	}

	public TestDataBuilder WithStandardWorkflow()
	{
		var noneId = Guid.NewGuid();
		var cutoffId = Guid.NewGuid();
		var profilingId = Guid.NewGuid();
		var assemblyId = Guid.NewGuid();

		WithWorkplace(w => { w.Id = noneId; w.Name = "none"; w.IsWorkplace = false; });
		WithWorkplace(w => { w.Id = cutoffId; w.Name = "Торцовка"; w.IsWorkplace = true; });
		WithWorkplace(w => { w.Id = profilingId; w.Name = "Профилирование"; w.IsWorkplace = true; });
		WithWorkplace(w => { w.Id = assemblyId; w.Name = "Сборка"; w.IsWorkplace = true; });

		WithWorkplaceTransition(t => { t.FromWorkplaceId = noneId; t.ToWorkplaceId = cutoffId; });
		WithWorkplaceTransition(t => { t.FromWorkplaceId = cutoffId; t.ToWorkplaceId = profilingId; });
		WithWorkplaceTransition(t => { t.FromWorkplaceId = profilingId; t.ToWorkplaceId = assemblyId; });

		return this;
	}

	public TestDataBuilder WithComment(Action<Comment> configure)
	{
		var comment = new Comment
		{
			Id = Guid.NewGuid(),
			OrderId = _orders.LastOrDefault()?.Id ?? Guid.NewGuid(),
			UserId = _users.LastOrDefault()?.Id,
			Content = "Test comment",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		configure(comment);
		_comments.Add(comment);
		return this;
	}

	public TestDataBuilder WithOrderSupply(Action<OrderSupply> configure)
	{
		var os = new OrderSupply
		{
			Id = Guid.NewGuid(),
			OrderId = _orders.LastOrDefault()?.Id ?? Guid.NewGuid()
		};
		configure(os);
		_orderSupplies.Add(os);
		return this;
	}

	public TestDataBuilder WithSupplyItem(Action<SupplyItem> configure)
	{
		var si = new SupplyItem
		{
			Id = Guid.NewGuid(),
			OrderSupplyId = _orderSupplies.LastOrDefault()?.Id ?? Guid.NewGuid(),
			SupplyTypeId = Guid.NewGuid()
		};
		configure(si);
		_supplyItems.Add(si);
		return this;
	}

	public TestDataBuilder WithSupplyType(Action<SupplyType> configure)
	{
		var supplyType = new SupplyType
		{
			Id = Guid.NewGuid(),
			Name = "TestSupply",
			IsActive = true // Важно для создания supply_items
		};
		configure(supplyType);
		_supplyTypes.Add(supplyType);
		return this;
	}

	public TestDataBuilder WithSupplyCondition(Action<SupplyCondition> configure)
	{
		var condition = new SupplyCondition
		{
			Id = Guid.NewGuid(),
			ConditionCode = "pending",
			SortOrder = 1
		};
		configure(condition);
		_supplyConditions.Add(condition);
		return this;
	}


	public void Build(IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.Database.EnsureCreated();

		db.Roles.AddRange(_roles);
		db.Users.AddRange(_users);
		db.Workplaces.AddRange(_workplaces);
		db.UserWorkplaces.AddRange(_userWorkplaces);
		db.Orders.AddRange(_orders);
		db.ProductionOrders.AddRange(_productionOrders);
		db.OrderFootprints.AddRange(_orderFootprints);
		db.WorkplaceTransitions.AddRange(_workplaceTransitions);
		db.Comments.AddRange(_comments);
		db.OrderSupplies.AddRange(_orderSupplies);
		db.SupplyItems.AddRange(_supplyItems);
		db.SupplyTypes.AddRange(_supplyTypes);
		db.SupplyConditions.AddRange(_supplyConditions);

		db.SaveChanges();
	}
}