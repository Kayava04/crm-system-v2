using System.Globalization;
using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Teachers.Contracts;

namespace Billing.Application.Features.CreatePayroll;

public sealed record CreatePayrollRequest(
    Guid TeacherId,
    string Period
);

public sealed record CreatePayrollResponse(
    Guid Id,
    Guid TeacherId,
    string Period,
    decimal BaseSalary,
    decimal LessonsRate,
    int CompletedLessonsCount,
    decimal TotalAmount,
    PayrollStatus Status
);

public sealed class CreatePayrollValidator : AbstractValidator<CreatePayrollRequest>
{
    public CreatePayrollValidator()
    {
        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("Teacher is required.");

        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("Period is required.")
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$").WithMessage("Period must be in format YYYY-MM.");
    }
}

public static class CreatePayrollEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManagePayments))
             .WithName("CreatePayroll")
             .WithSummary("Calculate and create a teacher payroll for a period")
             .Produces<CreatePayrollResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreatePayrollRequest request,
        IValidator<CreatePayrollRequest> validator,
        ITeacherPayrollRepository repository,
        IBillingUnitOfWork unitOfWork,
        ITeacherLookup teacherLookup,
        IScheduleLookup scheduleLookup,
        ILogger<CreatePayrollRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var salary = await teacherLookup.GetCurrentSalaryAsync(request.TeacherId, ct);
        if (salary is null)
        {
            // Either the teacher does not exist or has no salary rate in effect yet
            return Results.Problem(
                detail: $"Teacher with id '{request.TeacherId}' not found or has no salary rate.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        var exists = await repository.ExistsByTeacherAndPeriodAsync(request.TeacherId, request.Period, ct);
        if (exists)
        {
            logger.LogWarning(
                "Payroll for teacher {TeacherId} and period {Period} already exists",
                request.TeacherId, request.Period
            );

            return Results.Problem(
                detail: "Payroll for this teacher and period already exists.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var periodStart = DateTime.SpecifyKind(
            DateTime.ParseExact(request.Period + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var completedLessons = await scheduleLookup.GetCompletedLessonsCountAsync(
            request.TeacherId, periodStart, periodEnd, ct);

        var payroll = TeacherPayroll.Create(
            request.TeacherId,
            request.Period,
            salary.BaseSalary,
            salary.LessonsRate,
            completedLessons
        );

        await repository.AddAsync(payroll, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Payroll created: {PayrollId} for Teacher {TeacherId}, period {Period}",
            payroll.Id, payroll.TeacherId, payroll.Period
        );

        var response = new CreatePayrollResponse(
            payroll.Id,
            payroll.TeacherId,
            payroll.Period,
            payroll.BaseSalary,
            payroll.LessonsRate,
            payroll.CompletedLessonsCount,
            payroll.TotalAmount,
            payroll.Status
        );

        return Results.Created($"/api/billing/payrolls/{payroll.Id}", response);
    }
}
