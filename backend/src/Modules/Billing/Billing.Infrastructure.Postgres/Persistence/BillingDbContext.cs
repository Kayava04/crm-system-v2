using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Postgres.Persistence;

internal sealed class BillingDbContext(DbContextOptions<BillingDbContext> options)
    : DbContext(options), IBillingUnitOfWork
{
    public DbSet<StudentInvoice> StudentInvoices => Set<StudentInvoice>();
    public DbSet<TeacherPayroll> TeacherPayrolls => Set<TeacherPayroll>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("billing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
