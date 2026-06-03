using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KG.MES.Server.Tests.Controllers;

public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public UsersControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetUserByEmail_ShouldReturnExpectedResponse()
	{
		// 1. Настраиваем подмену БД (убираем PgSQL, вешаем InMemory)
		var customFactory = _factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				// Удаляем старые конфигурации (.NET 9/10)
				services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
				services.RemoveAll<DbContextOptions<AppDbContext>>();

				// Регистрируем InMemory
				services.AddDbContext<AppDbContext>(options =>
					options.UseInMemoryDatabase("TestDb"));
			});
		});

		// 2. Создаем клиент. 
		// Именно в этот момент хост собирается, и DI-контейнер окончательно формируется!
		var client = customFactory.CreateClient();

		// 3. СИДИРОВАНИЕ ДАННЫХ (Правильный способ)
		// Мы берем Services у уже собранной фабрики и создаем нормальный Scope
		using (var scope = customFactory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			// Явно создаем БД (для InMemory это инициализирует хранилище)
			db.Database.EnsureCreated();

			db.Roles.Add(new Role
			{
				Id = Guid.Parse("d8e3dbbd-8f69-409b-8e4e-19e34f3b8179"),
				Name = "Middle",
				Level = 10
			});

			db.Users.Add(new User
			{
				Id = Guid.Parse("6449f64c-1119-4d5a-94ab-3105014f0110"),
				Email = "victor.n.pisarev@gmail.com",
				Name = "Виктор",
				RoleId = Guid.Parse("d8e3dbbd-8f69-409b-8e4e-19e34f3b8179"),
			});

			db.SaveChanges();
		}

		// 4. Act (Выполняем запрос)
		var response = await client.GetAsync("/api/users/by-email/victor.n.pisarev@gmail.com");

		// 5. Assert (Проверки)
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var json = JsonDocument.Parse(content);

		json.RootElement.GetProperty("id").GetString().Should().Be("6449f64c-1119-4d5a-94ab-3105014f0110");
		json.RootElement.GetProperty("email").GetString().Should().Be("victor.n.pisarev@gmail.com");
		json.RootElement.GetProperty("name").GetString().Should().Be("Виктор");
		json.RootElement.GetProperty("role_id").GetString().Should().Be("d8e3dbbd-8f69-409b-8e4e-19e34f3b8179");
		json.RootElement.GetProperty("role_name").GetString().Should().Be("Middle");
		json.RootElement.GetProperty("role_level").GetInt32().Should().Be(10);

		json.RootElement.EnumerateObject().Select(p => p.Name).Should()
			.BeEquivalentTo(["id", "email", "name", "role_id", "role_name", "role_level"]);
	}

	[Fact]
	public async Task GetUserWorkplaces_ShouldReturnExpectedResponse()
	{
		// Arrange
		var customFactory = SetupTestFactory();
		var client = customFactory.CreateClient();

		// Создаем данные через Builder (читается как английский текст!)
		Role? createdRole = null;
		User? createdUser = null;
		var workplaces = new List<Workplace>();

		new TestDataBuilder()
			.WithRole(r => { r.Name = "Simple"; r.Level = 10; createdRole = r; })
			.WithUser(u => { u.Email = "test@example.com"; u.Name = "Тест"; createdUser = u; })
			.WithWorkplace(w => { w.Name = "Торцовка"; workplaces.Add(w); })
			.WithWorkplace(w => { w.Name = "Столярка"; workplaces.Add(w); })
			.WithUserWorkplace(createdUser!.Id, workplaces[0].Id)
			.WithUserWorkplace(createdUser.Id, workplaces[1].Id)
			.Build(customFactory.Services);

		// Act
		var response = await client.GetAsync($"/api/users/{createdUser.Id}/workplaces");

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
		var json = await ParseJsonResponse(response);

		json.GetArrayLength().Should().Be(2);
		// ... остальные проверки
	}

	// ====== Вспомогательные методы ======

	private WebApplicationFactory<Program> SetupTestFactory()
	{
		return _factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
				services.RemoveAll<DbContextOptions<AppDbContext>>();
				services.AddDbContext<AppDbContext>(options =>
					options.UseInMemoryDatabase("TestDb"));
			});
		});
	}

	private async Task<JsonElement> ParseJsonResponse(HttpResponseMessage response)
	{
		var content = await response.Content.ReadAsStringAsync();
		return JsonDocument.Parse(content).RootElement;
	}

}