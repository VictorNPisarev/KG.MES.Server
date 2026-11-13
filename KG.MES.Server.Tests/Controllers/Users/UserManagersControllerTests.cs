using System.Net;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using KG.MES.Server.Tests.Helpers;

namespace KG.MES.Server.Tests.Controllers.Users;

[Trait("Category", "Users")]
public class UsersManagersControllerTests : TestBase
{
	public UsersManagersControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetManagers_ShouldReturnOnlySalesManagers()
	{
		// Arrange
		var client = CreateClient();

		Guid salesManagerRoleId = Guid.Empty;
		Guid masterRoleId = Guid.Empty;

		var salesManager1Id = Guid.NewGuid();
		var salesManager2Id = Guid.NewGuid();
		var masterId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithRole(r => { r.Id = Guid.NewGuid(); r.Code = "SALES_MANAGER"; r.Name = "Sales manager"; r.Level = 10; salesManagerRoleId = r.Id; })
			.WithRole(r => { r.Id = Guid.NewGuid(); r.Code = "MASTER"; r.Name = "Master"; r.Level = 10; masterRoleId = r.Id; })
			.WithUser(u => { u.Id = salesManager1Id; u.Email = "sm1@test.com"; u.Name = "Анна Менеджерова"; u.RoleId = salesManagerRoleId; u.IsActive = true; })
			.WithUser(u => { u.Id = salesManager2Id; u.Email = "sm2@test.com"; u.Name = "Борис Продавцов"; u.RoleId = salesManagerRoleId; u.IsActive = true; })
			.WithUser(u => { u.Id = masterId; u.Email = "master@test.com"; u.Name = "Виктор Мастеров"; u.RoleId = masterRoleId; u.IsActive = true; }));

		// Act
		var response = await client.GetAsync("/api/users/managers");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SalesManagerDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);
		result.Should().Contain(m => m.Id == salesManager1Id);
		result.Should().Contain(m => m.Id == salesManager2Id);
		result.Should().NotContain(m => m.Id == masterId);
	}

	[Fact]
	public async Task GetManagers_WithSearch_ShouldFilterByNameOrEmail()
	{
		// Arrange
		var client = CreateClient();

		Guid salesManagerRoleId = Guid.Empty;

		BuildTestData(builder => builder
			.WithRole(r => { r.Id = Guid.NewGuid(); r.Code = "SALES_MANAGER"; r.Name = "Sales manager"; r.Level = 10; salesManagerRoleId = r.Id; })
			.WithUser(u => { u.Email = "anna@company.ru"; u.Name = "Анна Смирнова"; u.RoleId = salesManagerRoleId; u.IsActive = true; })
			.WithUser(u => { u.Email = "boris@other.ru"; u.Name = "Борис Иванов"; u.RoleId = salesManagerRoleId; u.IsActive = true; })
			.WithUser(u => { u.Email = "anna.ivanova@company.ru"; u.Name = "Анна Иванова"; u.RoleId = salesManagerRoleId; u.IsActive = true; }));

		// Act — по имени
		var byName = await client.GetAsync("/api/users/managers?search=анна");
		// Act — по email
		var byEmail = await client.GetAsync("/api/users/managers?search=@company.ru");

		// Assert
		byName.StatusCode.Should().Be(HttpStatusCode.OK);
		byEmail.StatusCode.Should().Be(HttpStatusCode.OK);

		var byNameResult = JsonSerializer.Deserialize<List<SalesManagerDto>>(
			await byName.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		var byEmailResult = JsonSerializer.Deserialize<List<SalesManagerDto>>(
			await byEmail.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		byNameResult!.Should().HaveCount(2);
		byNameResult.Should().OnlyContain(m => m.Name.Contains("Анна"));

		byEmailResult!.Should().HaveCount(2);
		byEmailResult.Should().OnlyContain(m => m.Email.Contains("@company.ru"));
	}

	[Fact]
	public async Task SearchUsers_ShouldReturnAllUsers()
	{
		// Arrange
		var client = CreateClient();

		Guid salesManagerRoleId = Guid.Empty;
		Guid masterRoleId = Guid.Empty;

		BuildTestData(builder => builder
			.WithRole(r => { r.Id = Guid.NewGuid(); r.Code = "SALES_MANAGER"; r.Name = "Sales manager"; r.Level = 10; salesManagerRoleId = r.Id; })
			.WithRole(r => { r.Id = Guid.NewGuid(); r.Code = "MASTER"; r.Name = "Master"; r.Level = 10; masterRoleId = r.Id; })
			.WithUser(u => { u.Email = "sm@test.com"; u.Name = "Менеджер"; u.RoleId = salesManagerRoleId; u.IsActive = true; })
			.WithUser(u => { u.Email = "master@test.com"; u.Name = "Мастер"; u.RoleId = masterRoleId; u.IsActive = true; }));

		// Act
		var response = await client.GetAsync("/api/users/search");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SalesManagerDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);
		result.Select(u => u.Name).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task SearchUsers_WithSearch_ShouldFilter()
	{
		// Arrange
		var client = CreateClient();

		BuildTestData(builder => builder
			.WithUser(u => { u.Email = "alice@company.ru"; u.Name = "Алиса"; u.IsActive = true; })
			.WithUser(u => { u.Email = "bob@other.ru"; u.Name = "Боб"; u.IsActive = true; })
			.WithUser(u => { u.Email = "charlie@company.ru"; u.Name = "Чарли"; u.IsActive = true; }));

		// Act
		var response = await client.GetAsync("/api/users/search?search=company.ru");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SalesManagerDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);
		result.Should().OnlyContain(u => u.Email.Contains("company.ru"));
	}
}