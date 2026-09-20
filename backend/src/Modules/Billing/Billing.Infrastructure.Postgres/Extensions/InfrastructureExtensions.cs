using Billing.Application.Abstractions;
using Billing.Infrastructure.Postgres.Persistence;
using Billing.Infrastructure.Postgres.Repositories;
using Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddBillingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddDatabase(configuration)
            .AddRepositories();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
services.AddModuleDbContext<BillingDbContext>();

        services.AddScoped<IBillingUnitOfWork>(sp => sp.GetRequiredService<BillingDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IStudentInvoiceRepository, StudentInvoiceRepository>();
        services.AddScoped<ITeacherPayrollRepository, TeacherPayrollRepository>();

        return services;
    }
}
