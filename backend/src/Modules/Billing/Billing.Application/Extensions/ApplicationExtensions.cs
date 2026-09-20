using Billing.Application.Features.CreateInvoice;
using Billing.Contracts;
using Billing.Application.Features.CreatePayroll;
using Billing.Application.Features.GetInvoiceById;
using Billing.Application.Features.GetInvoices;
using Billing.Application.Features.GetPayrollById;
using Billing.Application.Features.GetPayrolls;
using Billing.Application.Features.MarkInvoicePaid;
using Billing.Application.Features.MarkInvoicesOverdue;
using Billing.Application.Features.MarkPayrollPaid;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        services.AddScoped<IBillingStatistics, BillingStatisticsService>();

        return services;
    }

    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var invoices = app.MapGroup("/api/billing/invoices")
                          .WithTags("Billing - Invoices");

        GetInvoicesEndpoint.Map(invoices);
        GetInvoiceByIdEndpoint.Map(invoices);
        CreateInvoiceEndpoint.Map(invoices);
        MarkInvoicePaidEndpoint.Map(invoices);
        MarkInvoicesOverdueEndpoint.Map(invoices);

        var payrolls = app.MapGroup("/api/billing/payrolls")
                          .WithTags("Billing - Payrolls");

        GetPayrollsEndpoint.Map(payrolls);
        GetPayrollByIdEndpoint.Map(payrolls);
        CreatePayrollEndpoint.Map(payrolls);
        MarkPayrollPaidEndpoint.Map(payrolls);

        return app;
    }
}
