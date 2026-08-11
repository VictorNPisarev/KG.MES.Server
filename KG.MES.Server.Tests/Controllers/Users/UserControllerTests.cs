using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KG.MES.Server.Tests.Controllers.Users;

[Trait("Category", "Users")]
public class UsersControllerTests : TestBase
{
	public UsersControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetUserByEmail_ShouldReturnExpectedResponse()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;

		BuildTestData(builder => builder
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u => { u.Email = "test@example.com"; u.Name = "Тест"; createdUser = u; 
			}));

		// 4. Act (Выполняем запрос)
		var response = await client.GetAsync("/api/users/by-email/test@example.com");

		// 5. Assert (Проверки)
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var json = JsonDocument.Parse(content);

		json.RootElement.GetProperty("email").GetString().Should().Be("test@example.com");
		json.RootElement.GetProperty("name").GetString().Should().Be("Тест");
		json.RootElement.GetProperty("role_name").GetString().Should().Be("Middle");
		json.RootElement.GetProperty("role_level").GetInt32().Should().Be(10);

		json.RootElement.EnumerateObject().Select(p => p.Name).Should()
			.BeEquivalentTo(["id", "email", "name", "role_id", "role_name", "role_level"]);
	}

	[Fact]
	public async Task GetUserWorkplaces_ShouldReturnExpectedResponse()
	{
		// Arrange
		var client = CreateClient();

		// Создаем данные через Builder (читается как английский текст!)
		Role? createdRole = null;
		User? createdUser = null;
		var workplaces = new List<Workplace>();

		BuildTestData(builder => builder
			.WithRole(r => { r.Name = "Simple"; r.Level = 10; createdRole = r; })
			.WithUser(u => { u.Email = "test@example.com"; u.Name = "Тест"; createdUser = u; })
			.WithWorkplace(w => { w.Name = "Торцовка"; workplaces.Add(w); })
			.WithWorkplace(w => { w.Name = "Столярка"; workplaces.Add(w); })
			.WithUserWorkplace(createdUser!.Id, workplaces[0].Id)
			.WithUserWorkplace(createdUser.Id, workplaces[1].Id));

		// Act
		var response = await client.GetAsync($"/api/users/{createdUser!.Id}/workplaces");

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
		var json = await ParseJsonResponse(response);

		json.GetArrayLength().Should().Be(2);
	}

	[Fact]
	public async Task SetPassword_ShouldHashPassword()
	{
		// Arrange
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;

		BuildTestData(builder => builder
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u =>
			{
				u.Email = "test@example.com";
				u.Name = "Тест";
				u.RoleId = createdRole?.Id;
				u.PasswordHash = null;
				u.IsPasswordSet = false;
				createdUser = u;
			}));

		var request = new
		{
			email = "test@example.com",
			newPassword = "NewPassword123!"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/users/set-password", request);

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

		// Проверяем, что пароль установлен
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");

		user.Should().NotBeNull();
		user!.PasswordHash.Should().NotBeNullOrEmpty();
		user.IsPasswordSet.Should().BeTrue();
	}

	// ====== Вспомогательные методы ======

	private async Task<JsonElement> ParseJsonResponse(HttpResponseMessage response)
	{
		var content = await response.Content.ReadAsStringAsync();
		return JsonDocument.Parse(content).RootElement;
	}

}