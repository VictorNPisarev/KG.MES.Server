// KG.MES.Server/Services/Interfaces/IWorkplaceService.cs
using KG.MES.Shared.Models.Dto;

namespace KG.MES.Server.Services.Interfaces;

public interface IWorkplaceService
{
	Task<List<WorkplaceDto>> GetActiveWorkplacesAsync();
	Task<List<WorkplaceDto>> GetAllWorkplacesAsync();
	Task<WorkplaceStatsDto> GetWorkplaceStatsAsync(Guid workplaceId);
	Task<List<WorkplaceHistoryDto>> GetWorkplaceHistoryAsync(Guid workplaceId, DateTime? from, DateTime? to, int limit = 50);
	Task<List<WorkplaceBlockDto>> GetWorkplaceBlocksAsync(Guid workplaceId);
}