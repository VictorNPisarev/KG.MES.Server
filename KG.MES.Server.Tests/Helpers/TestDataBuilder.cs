using KG.MES.Server.Data;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KG.MES.Server.Tests.Helpers;

public class TestDataBuilder
{
	private readonly List<Role> _roles = new();
	private readonly List<User> _users = new();
	private readonly List<Workplace> _workplaces = new();
	private readonly List<UserWorkplace> _userWorkplaces = new();

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

	// Теперь Build принимает IServiceProvider (как ты и вызываешь в тесте)
	public void Build(IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.Database.EnsureCreated();

		db.Roles.AddRange(_roles);
		db.Users.AddRange(_users);
		db.Workplaces.AddRange(_workplaces);
		db.UserWorkplaces.AddRange(_userWorkplaces);
		db.SaveChanges();
	}
}