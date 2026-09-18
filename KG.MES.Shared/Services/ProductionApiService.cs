using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.Shared.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KG.MES.Shared.Services;

public class ProductionApiService
{
	private readonly HttpClient _httpClient;
	private readonly ILogger<ProductionApiService> _logger;
	private readonly IConfiguration _configuration;

	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		Converters = { new TotalsDtoConverter() },
		PropertyNameCaseInsensitive = true
	};

	private UserSessionService _session;
	public UserSessionService Session 
	{
		get 
		{
			return _session;
		}
		set
		{
			_session = value;
		}
	}
	private string BaseUrl => _configuration["ProductionApi:BaseUrl"] ?? "http://192.168.0.179:3031/api";
	private int TimeoutSeconds => _configuration.GetValue<int>("ProductionApi:TimeoutSeconds", 30);
	private int RetryCount => _configuration.GetValue<int>("ProductionApi:RetryCount", 3);


	public ProductionApiService(
		HttpClient httpClient,
		ILogger<ProductionApiService> logger,
		IConfiguration configuration,
		UserSessionService session)
	{
		_httpClient = httpClient;
		_logger = logger;
		_configuration = configuration;
		_session = session;
	}

	/// <summary>
	/// Проставляет/сбрасывает Authorization-заголовок из текущей сессии.
	/// </summary>
	private async Task EnsureAuthorization()
	{
		await _session.RestoreAsync();

		if (!string.IsNullOrEmpty(_session.AccessToken))
		{
			_httpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", _session.AccessToken);
		}
		else
		{
			_httpClient.DefaultRequestHeaders.Authorization = null;
		}
	}

	/// <summary>
	/// POST запись нового заказ в бд
	/// </summary>
	/// <param name="order"></param>
	/// <param name="dto"></param>
	/// <returns></returns>
	public async Task<bool> ExportToProductionAsync(ProductionOrderExportDto dto)
	{
		await EnsureAuthorization();

		var retries = 0;

		while (retries < RetryCount)
		{
			try
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

				var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				});

				var content = new StringContent(json, Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync($"{BaseUrl}/orders/create", content, cts.Token);

				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation("Order {OrderNumber} sent to production successfully", dto.OrderNumber);
					return true;
				}

				var error = await response.Content.ReadAsStringAsync(cts.Token);
				_logger.LogWarning("Attempt {Retry}/{RetryCount} failed: {StatusCode} - {Error}",
					retries + 1, RetryCount, response.StatusCode, error);
			}
			catch (TaskCanceledException)
			{
				_logger.LogWarning("Attempt {Retry}/{RetryCount} timeout after {Timeout} seconds",
					retries + 1, RetryCount, TimeoutSeconds);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Attempt {Retry}/{RetryCount} failed", retries + 1, RetryCount);
			}

			retries++;

			if (retries < RetryCount)
			{
				await Task.Delay(1000 * retries); // экспоненциальная задержка
			}
		}

		_logger.LogError("Failed to send order {OrderNumber} to production after {RetryCount} attempts",
			dto.OrderNumber, RetryCount);

		return false;
	}

	/// <summary>
	/// GET заказов
	/// </summary>
	/// <param name="status"></param>
	/// <param name="number"></param>
	/// <param name="page"></param>
	/// <param name="limit"></param>
	/// <param name="sortBy"></param>
	/// <param name="sortOrder"></param>
	/// <returns></returns>
	public async Task<PaginatedResponse<OrderDto>> GetOrdersAsync(
		string? status = null,
		string? number = null,
		int page = 1,
		int limit = 50,
		string? sortBy = null,
		string? sortOrder = null)
	{
		try
		{
			await EnsureAuthorization();

			// Поиск по номеру
			if (!string.IsNullOrEmpty(number))
			{
				var orderUrl = $"{BaseUrl}/orders/{Uri.EscapeDataString(number)}";
				var order = await _httpClient.GetFromJsonAsync<OrderDto>(orderUrl);

				return new PaginatedResponse<OrderDto>
				{
					Data = order != null ? [order] : [],
					Pagination = new PaginationInfo { Page = 1, Limit = 1, Total = order != null ? 1 : 0, Pages = 1 }
				};
			}

			// Список с пагинацией и сортировкой
			/*var endpoint = status != null
				? $"orders/{Uri.EscapeDataString(status.ToString() ?? string.Empty)}"
				: "orders/all";*/
			var endpoint = "orders/all";

			var queryParams = new List<string>
			{
				$"page={page}",
				$"limit={limit}"
			};

			if (!string.IsNullOrEmpty(sortBy))
				queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

			if (!string.IsNullOrEmpty(sortOrder))
				queryParams.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");

			var listUrl = $"{BaseUrl}/{endpoint}?" + string.Join("&", queryParams);

			_logger.LogInformation("Fetching orders: {Url}", listUrl);

			var response = await _httpClient.GetFromJsonAsync<PaginatedResponse<OrderDto>>(listUrl);
			return response ?? new PaginatedResponse<OrderDto>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching orders from API");
			return new PaginatedResponse<OrderDto>();
		}
	}

	public async Task<bool> TestConnectionAsync()
	{
		try
		{
			await EnsureAuthorization();

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			var response = await _httpClient.GetAsync($"{BaseUrl}/health", cts.Token);
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	public async Task<PaginatedResponse<OrderDto>> GetOrdersAsync(
		Guid? workplaceId = null,
		string? orderNumber = null,
		int page = 1,
		int limit = 50,
		string? sortBy = null,
		string? sortOrder = null)
	{
		try
		{
			await EnsureAuthorization();

			var queryParams = new Dictionary<string, string>
			{
				["page"] = page.ToString(),
				["limit"] = limit.ToString()
			};

			var endpoint = "orders/all";

			if (workplaceId.HasValue && workplaceId != Guid.Empty)
			{
				endpoint = "orders";
				queryParams["workplaceId"] = workplaceId.Value.ToString();
			}

			if (!string.IsNullOrEmpty(orderNumber))
			{
				endpoint = "orders";
				queryParams["number"] = Uri.EscapeDataString(orderNumber);
			}

			if (!string.IsNullOrEmpty(sortBy))
				queryParams["sortBy"] = Uri.EscapeDataString(sortBy);

			if (!string.IsNullOrEmpty(sortOrder))
				queryParams["sortOrder"] = Uri.EscapeDataString(sortOrder);

			var query = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
			var listUrl = $"{BaseUrl}/{endpoint}?" + query;//string.Join("&", queryParams);

			_logger.LogInformation("Fetching orders: {Url}", listUrl);

			var response = await _httpClient.GetFromJsonAsync<PaginatedResponse<OrderDto>>(listUrl);
			return response ?? new PaginatedResponse<OrderDto>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching orders");
			return new PaginatedResponse<OrderDto>();
		}
	}

	public async Task<PaginatedResponse<T>> GetOrdersAsync<T>(
		string endpoint,  // "orders/all" или "orders/masters" или "orders/supply"
		//string? status = null,
		Guid? workplaceId = null,
		Guid[]? workplaceIds = null,
		string? orderNumber = null,
		int page = 1,
		int limit = 50,
		string? sortBy = null,
		string? sortOrder = null,
		List<FilterCondition>? filters = null)
	{
		var queryUrl = string.Empty;
		try
		{
			await EnsureAuthorization();

			var queryParams = new Dictionary<string, string>
				{
					["page"] = page.ToString(),
					["limit"] = limit.ToString()
				};

			//if (!string.IsNullOrEmpty(status))
			//	queryParams["status"] = status;

			if (workplaceId.HasValue && workplaceId != Guid.Empty)
				queryParams["workplaceId"] = workplaceId.Value.ToString();

			if (!string.IsNullOrEmpty(orderNumber))
				queryParams["orderNumber"] = orderNumber;

			if (!string.IsNullOrEmpty(sortBy))
				queryParams["sortBy"] = sortBy;

			if (!string.IsNullOrEmpty(sortOrder))
				queryParams["sortOrder"] = sortOrder;

			if (filters?.Count > 0)
			{
				var filtersJson = JsonSerializer.Serialize(filters);
				queryParams["filters"] = filtersJson;
			}

			var query = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));

			if (workplaceIds?.Length > 0)
			{
				foreach (var id in workplaceIds)
					query += $"&workplaceIds={id}";
			}

			var url = $"{BaseUrl}/{endpoint}?{query.TrimStart('&')}";

			queryUrl = url;

			var result = await _httpClient.GetFromJsonAsync<PaginatedResponse<T>>(url, jsonOptions)
				?? new PaginatedResponse<T>();

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error method GetOrdersAsync<T>");
			return new PaginatedResponse<T>();
		}
	}

	public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
	{
		try
		{
			await EnsureAuthorization();

			return await _httpClient.GetFromJsonAsync<OrderDto>($"{BaseUrl}/orders/{id}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching order {Id}", id);
			return null;
		}
	}

	public async Task<List<WorkplaceDto>> GetActiveWorkplacesAsync()
	{
		try
		{
			await EnsureAuthorization();

			var response = await _httpClient.GetFromJsonAsync<List<WorkplaceDto>>($"{BaseUrl}/workplaces/active");
			return response ?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching active workplaces");
			return [];
		}
	}

	// GET /api/workplaces/all
	public async Task<List<WorkplaceDto>> GetAllWorkplacesAsync()
	{
		try
		{
			await EnsureAuthorization();

			var response = await _httpClient.GetFromJsonAsync<List<WorkplaceDto>>($"{BaseUrl}/workplaces/all");
			return response ?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching all workplaces");
			return [];
		}
	}

	// GET /api/workplaces/{id}
	public async Task<WorkplaceDto?> GetWorkplaceByIdAsync(Guid id)
	{
		try
		{
			await EnsureAuthorization();

			return await _httpClient.GetFromJsonAsync<WorkplaceDto>($"{BaseUrl}/workplaces/{id}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching workplace {Id}", id);
			return null;
		}
	}

	public async Task<bool> UpdateOrderStatusAsync(Guid id, string status)
	{
		try
		{
			await EnsureAuthorization();

			var response = await _httpClient.PutAsJsonAsync(
				$"{BaseUrl}/orders/{id}/status",
				new { status });
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating status for order {Id}", id);
			return false;
		}
	}

	public async Task<bool> UpdateMasterNotesAsync(Guid id, string notes)
	{
		try
		{
			await EnsureAuthorization();

			var response = await _httpClient.PutAsJsonAsync(
				$"{BaseUrl}/orders/{id}/notes",
				new { masterNotes = notes });
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating notes for order {Id}", id);
			return false;
		}
	}

	/// <summary>
	/// Запрос к API для получекния подробной информации для конкретного заказа
	/// с указание Api endpoint
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="endpoint"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public async Task<T?> GetOrderByIdAsync<T>(string endpoint, Guid id)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/{endpoint}/{id}";
			return await _httpClient.GetFromJsonAsync<T>(url);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching order {Id}", id);
			return default;
		}
	}

	/// <summary>
	/// Получение трекинга заказа на производстве
	/// </summary>
	/// <param name="orderId"></param>
	/// <returns></returns>
	public async Task<OrderTraceDto?> GetOrderTraceAsync(Guid orderId)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/orders/{orderId}/trace";
			var response = await _httpClient.GetFromJsonAsync<OrderTraceResponse>(url);
			return response?.OrderTraces?.FirstOrDefault();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching trace for order {Id}", orderId);
			return null;
		}
	}

	/// <summary>
	/// Получение данных снабжения заказа
	/// </summary>
	/// <param name="orderId"></param>
	/// <returns></returns>
	public async Task<List<OrderSupplyDto>> GetOrderSuppliesAsync(Guid orderId)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/orders/{orderId}/supplies";
			var supplies = await _httpClient.GetFromJsonAsync<List<OrderSupplyDto>>(url)
							?? [];

			return supplies;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching supplies for order {Id}", orderId);
			return [];
		}
	}

	public async Task<bool> UpdateOrderSuppliesAsync(Guid orderId, List<object> supplies)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/orders/{orderId}/supplies";
			Console.WriteLine($"UpdateOrderSuppliesAsync url: {url}");
			var body = new { supplies };
			
			var response = await _httpClient.PutAsJsonAsync(url, body);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating supplies for order {Id}", orderId);
			return false;
		}
	}

	public async Task<List<SupplyConditionDto>> GetSupplyConditionsAsync()
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/supplies/conditions";
			return await _httpClient.GetFromJsonAsync<List<SupplyConditionDto>>(url)
					?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching supply conditions");
			return [];
		}
	}

	public async Task<List<SupplyTypeDto>> GetSupplyTypesAsync()
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/supplies/types";
			return await _httpClient.GetFromJsonAsync<List<SupplyTypeDto>>(url)
					?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching supply types");
			return [];
		}
	}

	public async Task<List<OrderCommentDto>> GetOrderCommentsAsync(Guid orderId)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/orders/{orderId}/comments";
			return await _httpClient.GetFromJsonAsync<List<OrderCommentDto>>(url)
					?? new List<OrderCommentDto>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching comments for order {Id}", orderId);
			return new List<OrderCommentDto>();
		}
	}

	public async Task<bool> SaveSupplyCommentAsync(Guid orderId, OrderCommentViewModel comment)
	{
		try
		{
			await EnsureAuthorization();

			Console.WriteLine($"SaveCommentAsync orderId:{orderId}");
			HttpResponseMessage response;
			if (comment.IsNew)
			{
				// POST /api/orders/{orderId}/comments
				response = await _httpClient.PostAsJsonAsync(
					$"{BaseUrl}/orders/{orderId}/OrderSupplyComments",
					new { content = comment.Content });

				return response.IsSuccessStatusCode;

			}
			else
			{
				return await UpdateCommentAsync(orderId, comment);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving comment for order {OrderId}", orderId);
			return false;
		}
	}

	public async Task<bool> SaveProductionOrderCommentAsync(Guid orderId, OrderCommentViewModel comment)
	{
		try
		{
			await EnsureAuthorization();

			Console.WriteLine($"SaveCommentAsync orderId:{orderId}");
			HttpResponseMessage response;
			if (comment.IsNew)
			{
				// POST /api/orders/{orderId}/comments
				response = await _httpClient.PostAsJsonAsync(
					$"{BaseUrl}/orders/{orderId}/productionOrderComments",
					new { content = comment.Content });

				return response.IsSuccessStatusCode;

			}
			else
			{
				return await UpdateCommentAsync(orderId, comment);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving comment for order {OrderId}", orderId);
			return false;
		}
	}

	public async Task<bool> SaveCommentAsync(Guid orderId, OrderCommentViewModel comment)
	{
		try
		{
			await EnsureAuthorization();

			Console.WriteLine($"SaveCommentAsync orderId:{orderId}");
			HttpResponseMessage response;
			if (comment.IsNew)
			{
				// POST /api/orders/{orderId}/comments
				response = await _httpClient.PostAsJsonAsync(
					$"{BaseUrl}/orders/{orderId}/comments",
					new { content = comment.Content });

				return response.IsSuccessStatusCode;
			}
			else
			{
				return await UpdateCommentAsync(orderId, comment);
			}

		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving comment for order {OrderId}", orderId);
			return false;
		}
	}

	public async Task<bool> UpdateCommentAsync(Guid orderId, OrderCommentViewModel comment)
	{
		try
		{
			await EnsureAuthorization();

			Console.WriteLine($"SaveCommentAsync orderId:{orderId}");
			HttpResponseMessage response;

			// PUT /api/orders/{orderId}/comments/{commentId}
			response = await _httpClient.PutAsJsonAsync(
				$"{BaseUrl}/orders/{orderId}/comments/{comment.Id}",
				new { content = comment.Content });

			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving comment for order {OrderId}", orderId);
			return false;
		}
	}

	public async Task<bool> DeleteCommentAsync(Guid orderId, Guid commentId)
	{
		try
		{
			await EnsureAuthorization();

			var response = await _httpClient.DeleteAsync(
				$"{BaseUrl}/orders/{orderId}/comments/{commentId}");
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
			return false;
		}
	}

	public async Task<List<WorkplaceDto>> GetWorkplacesAsync(string? type = null)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/workplaces";
			if (!string.IsNullOrEmpty(type))
				url += $"?type={type}";

			_logger.LogInformation("GetWorkplacesAsync: {url}", url);

			return await _httpClient.GetFromJsonAsync<List<WorkplaceDto>>(url) ?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching workplaces");
			return [];
		}
	}

	public async Task<WorkplaceStatsDto?> GetWorkplaceStatsAsync(Guid workplaceId)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/workplaces/{workplaceId}/stats";

			_logger.LogInformation("GetWorkplaceStatsAsync: {url}", url);

			return await _httpClient.GetFromJsonAsync<WorkplaceStatsDto>(url);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching stats for workplace {Id}", workplaceId);
			return null;
		}
	}

	public async Task<List<BlockedOrderDto>> GetWorkplaceBlocksAsync(Guid workplaceId)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/workplaces/{workplaceId}/blocks";

			_logger.LogInformation("GetWorkplaceBlocksAsync: {url}", url);

			return await _httpClient.GetFromJsonAsync<List<BlockedOrderDto>>(url) ?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching blocks for workplace {Id}", workplaceId);
			return [];
		}
	}

	public async Task<List<WorkplaceHistoryDto>> GetWorkplaceHistoryAsync(
		Guid workplaceId, DateTime from, DateTime to, int limit = 1000)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/workplaces/{workplaceId}/history?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&limit={limit}";

			_logger.LogInformation("GetWorkplaceHistoryAsync: {url}", url);

			return await _httpClient.GetFromJsonAsync<List<WorkplaceHistoryDto>>(url) ?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching history for workplace {Id}", workplaceId);
			return [];
		}
	}

	public async Task<bool> UpdateOrderTraceAsync<T>(Guid orderId, List<T> updates)
	{
		try
		{
			await EnsureAuthorization();

			var body = new { workplaces = updates };
			var response = await _httpClient.PutAsJsonAsync(
				$"{BaseUrl}/traces/{orderId}/workplace", body);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating trace for order {Id}", orderId);
			return false;
		}
	}

	public async Task<bool> UpdateOrderTraceAsync(Guid productionOrderId, Guid workplaceId, string status, string? notes = null)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/traces/{productionOrderId}/workplace/{workplaceId}";
			//var body = new { status, userId, notes };
			var body = new {status};

			var response = await _httpClient.PutAsJsonAsync(url, body);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating trace for order {OrderId}, workplace {WorkplaceId}", productionOrderId, workplaceId);
			return false;
		}
	}

	/// <summary>
	/// Рассчитывает дату готовности заказа с учетом производственного календаря.
	/// </summary>
	public async Task<DateTime?> CalculateReadyDateAsync(DateTime startDate, int workingDays)
	{
		if (workingDays <= 0)
		{
			return null;
		}

		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/ProductionCalendar/calculate";

			var body = new
			{
				startDate = startDate.ToString("yyyy-MM-dd"),
				workingDays = workingDays
			};

			var response = await _httpClient.PostAsJsonAsync(url, body);

			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync();
				throw new Exception($"Ошибка расчета даты: {error}");
			}

			var result = await response.Content.ReadFromJsonAsync<CalculateReadyDateDto>();

			return result?.EndDate;

		}
		catch (Exception ex)
		{
			throw new Exception($"Не удалось рассчитать дату готовности: {ex.Message}");
		}
	}

	public async Task<bool> SetOrderCompleteAsync(Guid orderId)
	{
		try
		{
			await EnsureAuthorization();

			var response = await _httpClient.PostAsync($"{BaseUrl}/orders/{orderId}/complete", null);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error completing order {Id}", orderId);
			return false;
		}
	}

	public async Task<bool> SetOrderDepartureAsync(Guid orderId)
	{
		try
		{
			await EnsureAuthorization();

			var response = await _httpClient.PostAsync($"{BaseUrl}/orders/{orderId}/departure", null);
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error departing order {Id}", orderId);
			return false;
		}
	}

	public async Task<CommerceOrderDto?> GetCommerceAsync(Guid orderId)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/orders/{orderId}/commerce";
			return await _httpClient.GetFromJsonAsync<CommerceOrderDto>(url);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching commerce data for order {Id}", orderId);
			return null;
		}
	}

	public async Task<bool> DeleteOrderAsync(Guid orderId)
	{
		await EnsureAuthorization();

		var response = await _httpClient.DeleteAsync($"{BaseUrl}/orders/{orderId}");
		return response.IsSuccessStatusCode;
	}

	public async Task<ProductionOrderExportDto?> GetOrderForEditAsync(Guid orderId)
	{
		await EnsureAuthorization();

		return await _httpClient.GetFromJsonAsync<ProductionOrderExportDto>($"{BaseUrl}/orders/{orderId}/edit");
	}

	public async Task<bool> UpdateOrderAsync(Guid orderId, ProductionOrderExportDto dto)
	{
		await EnsureAuthorization();

		var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/orders/{orderId}", dto);
		return response.IsSuccessStatusCode;
	}

	public async Task<List<WorkplaceOrderDto>> GetActiveAndPendingOrdersAsync(Guid workplaceId)
	{
		try
		{
			await EnsureAuthorization();

			var url = $"{BaseUrl}/orders/workplaces/{workplaceId}/in-work";
			return await _httpClient.GetFromJsonAsync<List<WorkplaceOrderDto>>(url) ?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching orders for workplace {Id}", workplaceId);
			return [];
		}
	}

	public async Task<FilterFacetsResponseDto?> GetFilterFacetsAsync<T>(
	FilterFacetsRequestDto request)
	{
		try
		{
			var url = $"{BaseUrl}/orders/facets";
			var response = await _httpClient.PostAsJsonAsync(url, request);

			if (!response.IsSuccessStatusCode) return null;

			return await response.Content.ReadFromJsonAsync<FilterFacetsResponseDto>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching filter facets");
			return null;
		}
	}

	public async Task<FilterFacetsResponseDto?> GetFilterFacetsAsync<TDto>(string endpoint, FilterFacetsRequestDto request)
		//List<string> fields, List<FilterCondition>? appliedFilters = null)
	{
		//await EnsureAuthorization();

		//var request = new FilterFacetsRequestDto
		//{
		//	Fields = fields,
		//	AppliedFilters = appliedFilters
		//};

		var url = $"{BaseUrl}/{endpoint}/facets";
		var response = await _httpClient.PostAsJsonAsync(url, request);

		if (!response.IsSuccessStatusCode)
			return null;

		return await response.Content.ReadFromJsonAsync<FilterFacetsResponseDto>();
			//   ?? new FilterFacetsResponseDto();
	}
}