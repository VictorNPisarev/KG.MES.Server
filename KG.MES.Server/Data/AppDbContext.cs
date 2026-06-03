using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<Workplace> Workplaces { get; set; }
	public DbSet<WorkplaceTransition> WorkplaceTransitions { get; set; }
	public DbSet<Order> Orders { get; set; }
	public DbSet<ProductionOrder> ProductionOrders { get; set; }
	public DbSet<OrderFootprint> OrderFootprints { get; set; }
	public DbSet<OperationLog> OperationLogs { get; set; }
	public DbSet<OrderBlock> OrderBlocks { get; set; }
	public DbSet<SupplyType> SupplyTypes { get; set; }
	public DbSet<SupplyCondition> SupplyConditions { get; set; }
	public DbSet<OrderSupply> OrderSupplies { get; set; }
	public DbSet<SupplyItem> SupplyItems { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<User> Users { get; set; }
	public DbSet<Role> Roles { get; set; }
	public DbSet<UserWorkplace> UserWorkplaces { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// ========== Индексы ==========
		modelBuilder.Entity<Workplace>().HasIndex(w => w.LegacyId);
		modelBuilder.Entity<Order>().HasIndex(o => o.OrderNumber);
		modelBuilder.Entity<Order>().HasIndex(o => o.LegacyId);
		modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
		modelBuilder.Entity<User>().HasIndex(u => u.LegacyId);
		modelBuilder.Entity<ProductionOrder>().HasIndex(p => p.LegacyId);
		modelBuilder.Entity<SupplyItem>().HasIndex(s => s.OrderSupplyId);

		// ========== Массивы UUID ==========
		modelBuilder.Entity<Order>().Property(o => o.CommentIds).HasColumnType("uuid[]");
		modelBuilder.Entity<ProductionOrder>().Property(p => p.CommentIds).HasColumnType("uuid[]");
		modelBuilder.Entity<OrderSupply>().Property(o => o.CommentIds).HasColumnType("uuid[]");

		// ========== Уникальные ограничения ==========
		modelBuilder.Entity<WorkplaceTransition>()
			.HasIndex(wt => new { wt.FromWorkplaceId, wt.ToWorkplaceId }).IsUnique();

		modelBuilder.Entity<UserWorkplace>()
			.HasIndex(uw => new { uw.UserId, uw.WorkplaceId }).IsUnique();

		modelBuilder.Entity<OrderFootprint>()
			.HasIndex(of => new { of.ProductionOrderId, of.WorkplaceId }).IsUnique();

		modelBuilder.Entity<SupplyItem>()
			.HasIndex(si => new { si.OrderSupplyId, si.SupplyTypeId }).IsUnique();

		// ========== Связи Workplace ==========
		modelBuilder.Entity<Workplace>()
			.HasOne(w => w.PreviousWorkplace)
			.WithMany()
			.HasForeignKey(w => w.PreviousWorkplaceId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<WorkplaceTransition>()
			.HasOne(wt => wt.FromWorkplace)
			.WithMany(w => w.FromTransitions)
			.HasForeignKey(wt => wt.FromWorkplaceId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<WorkplaceTransition>()
			.HasOne(wt => wt.ToWorkplace)
			.WithMany(w => w.ToTransitions)
			.HasForeignKey(wt => wt.ToWorkplaceId)
			.OnDelete(DeleteBehavior.Cascade);

		// ========== Связи Order ==========
		modelBuilder.Entity<ProductionOrder>()
			.HasOne(po => po.Order)
			.WithOne(o => o.ProductionOrder)
			.HasForeignKey<ProductionOrder>(po => po.OrderId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<OrderSupply>()
			.HasOne(os => os.Order)
			.WithOne(o => o.OrderSupply)
			.HasForeignKey<OrderSupply>(os => os.OrderId)
			.OnDelete(DeleteBehavior.Cascade);

		// ========== Связи ProductionOrder ==========
		modelBuilder.Entity<ProductionOrder>()
			.HasOne(po => po.CurrentWorkplace)
			.WithMany(w => w.ProductionOrders)
			.HasForeignKey(po => po.CurrentWorkplaceId)
			.OnDelete(DeleteBehavior.Restrict);

		// ========== Связи OrderFootprint ==========
		modelBuilder.Entity<OrderFootprint>()
			.HasOne(of => of.ProductionOrder)
			.WithMany(po => po.OrderFootprints)
			.HasForeignKey(of => of.ProductionOrderId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<OrderFootprint>()
			.HasOne(of => of.Workplace)
			.WithMany(w => w.OrderFootprints)
			.HasForeignKey(of => of.WorkplaceId)
			.OnDelete(DeleteBehavior.Restrict);

		// ========== Связи OperationLog ==========
		modelBuilder.Entity<OperationLog>()
			.HasOne(ol => ol.ProductionOrder)
			.WithMany(po => po.OperationLogs)
			.HasForeignKey(ol => ol.ProductionOrderId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<OperationLog>()
			.HasOne(ol => ol.Workplace)
			.WithMany(w => w.OperationLogs)
			.HasForeignKey(ol => ol.WorkplaceId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<OperationLog>()
			.HasOne(ol => ol.User)
			.WithMany(u => u.OperationLogs)
			.HasForeignKey(ol => ol.UserId)
			.OnDelete(DeleteBehavior.SetNull);

		// ========== Связи OrderBlock ==========
		modelBuilder.Entity<OrderBlock>()
			.HasOne(ob => ob.ProductionOrder)
			.WithMany(po => po.OrderBlocks)
			.HasForeignKey(ob => ob.ProductionOrderId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<OrderBlock>()
			.HasOne(ob => ob.Workplace)
			.WithMany(w => w.OrderBlocks)
			.HasForeignKey(ob => ob.WorkplaceId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<OrderBlock>()
			.HasOne(ob => ob.User)
			.WithMany(u => u.OrderBlocks)
			.HasForeignKey(ob => ob.UserId)
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<OrderBlock>()
			.HasOne(ob => ob.ResolvedByUser)
			.WithMany()
			.HasForeignKey(ob => ob.ResolvedBy)
			.OnDelete(DeleteBehavior.SetNull);

		// ========== Связи Supply ==========
		modelBuilder.Entity<SupplyItem>()
			.HasOne(si => si.OrderSupply)
			.WithMany(os => os.SupplyItems)
			.HasForeignKey(si => si.OrderSupplyId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<SupplyItem>()
			.HasOne(si => si.SupplyType)
			.WithMany(st => st.SupplyItems)
			.HasForeignKey(si => si.SupplyTypeId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<SupplyItem>()
			.HasOne(si => si.Condition)
			.WithMany(sc => sc.SupplyItems)
			.HasForeignKey(si => si.ConditionId)
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<SupplyItem>()
			.HasOne(si => si.CommentEntity)
			.WithOne()
			.HasForeignKey<SupplyItem>(si => si.CommentId)
			.OnDelete(DeleteBehavior.SetNull);

		// ========== Связи Comment ==========
		modelBuilder.Entity<Comment>()
			.HasOne(c => c.Order)
			.WithMany()
			.HasForeignKey(c => c.OrderId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Comment>()
			.HasOne(c => c.User)
			.WithMany(u => u.Comments)
			.HasForeignKey(c => c.UserId)
			.OnDelete(DeleteBehavior.SetNull);

		// ========== Связи User ==========
		modelBuilder.Entity<User>()
			.HasOne(u => u.Role)
			.WithMany(r => r.Users)
			.HasForeignKey(u => u.RoleId)
			.OnDelete(DeleteBehavior.SetNull);

		// ========== Связи UserWorkplace ==========
		modelBuilder.Entity<UserWorkplace>()
			.HasOne(uw => uw.User)
			.WithMany(u => u.UserWorkplaces)
			.HasForeignKey(uw => uw.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<UserWorkplace>()
			.HasOne(uw => uw.Workplace)
			.WithMany(w => w.UserWorkplaces)
			.HasForeignKey(uw => uw.WorkplaceId)
			.OnDelete(DeleteBehavior.Cascade);

		base.OnModelCreating(modelBuilder);
	}
}