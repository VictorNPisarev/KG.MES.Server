using KG.MES.Server.Constants;
using KG.MES.Server.Data;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public static class OrderServiceHelper
{
	public static bool CanTransitionStatus(string from, string to)
	{
		return OrderStatus.WorkplaceStatus.CanTransition(from, to);
	}

	public static async Task<Guid?> GetNoneWorkplaceIdAsync(AppDbContext context)
	{
		var workplace = await context.Workplaces
			.FirstOrDefaultAsync(w => w.Name == "none");
		return workplace?.Id;
	}

	public static async Task<bool> IsStartWorkplaceAsync(AppDbContext context, Guid workplaceId)
	{
		var noneId = await GetNoneWorkplaceIdAsync(context);

		var hasPrevious = await context.WorkplaceTransitions
			.AnyAsync(wt => wt.ToWorkplaceId == workplaceId && wt.FromWorkplaceId != noneId);

		return !hasPrevious;
	}

	public static async Task<List<Guid>> GetNextWorkplacesAsync(AppDbContext context, Guid workplaceId)
	{
		return await context.WorkplaceTransitions
			.Where(wt => wt.FromWorkplaceId == workplaceId)
			.Select(wt => wt.ToWorkplaceId)
			.ToListAsync();
	}

	public static async Task<List<Guid>> GetParallelWorkplacesAsync(AppDbContext context, Guid workplaceId)
	{
		var noneId = await GetNoneWorkplaceIdAsync(context);

		var previous = await context.WorkplaceTransitions
			.Where(wt => wt.ToWorkplaceId == workplaceId)
			.Select(wt => wt.FromWorkplaceId)
			.FirstOrDefaultAsync();

		if (previous == Guid.Empty || previous == noneId)
			return new List<Guid>();

		return await context.WorkplaceTransitions
			.Where(wt => wt.FromWorkplaceId == previous && wt.ToWorkplaceId != workplaceId)
			.Select(wt => wt.ToWorkplaceId)
			.ToListAsync();
	}

	public static async Task<bool> IsJoineryWorkplaceAsync(AppDbContext context, Guid workplaceId)
	{
		var workplace = await context.Workplaces
			.FirstOrDefaultAsync(w => w.Id == workplaceId);
		return workplace?.Name == "Столярка";
	}
}