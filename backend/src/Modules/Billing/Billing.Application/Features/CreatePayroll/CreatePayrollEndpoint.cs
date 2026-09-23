using System.Globalization;
using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using FluentValidation;
using Identity.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Teachers.Contracts;

namespace Billing.Application.Features.CreatePayroll;

// Exactly one of TeacherId (lesson-based pay) and UserId (an administrator or manager's flat Salary) is set
public sealed record CreatePayrollRequest(
    Guid? TeacherId,
    Guid? UserId,
    string Period
);

public sealed record CreatePayrollResponse(
    Guid Id,
    Guid? TeacherId,
    Guid? UserId,
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
        RuleFor(x => x)
            .Must(x => x.TeacherId is null != x.UserId is null)
            .WithName("TeacherId")
            .WithMessage("Specify either a teacher or a staff member.");

        // NotEmpty() on a nullable Guid compares against null, not Guid.Empty, so an explicit
        // comparison is needed here to actually catch an empty-but-present id.
        RuleFor(x => x.TeacherId)
            .Must(id => id != Guid.Empty).WithMessage("Teacher must not be empty.")
            .When(x => x.TeacherId is not null);

        RuleFor(x => x.UserId)
            .Must(id => id != Guid.Empty).WithMessage("Staff member must not be empty.")
            .When(x => x.UserId is not null);

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
             .WithSummary("Calculate and create a payroll for a period: a teacher's from lessons taught, a staff member's from their Salary")
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
        IUserDirectory userDirectory,
        ILogger<CreatePayrollRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        TeacherPayroll payroll;

        if (request.TeacherId is { } teacherId)
        {
            var salary = await teacherLookup.GetCurrentSalaryAsync(teacherId, ct);
            if (salary is null)
                return Results.Problem(
                    detail: $"Teacher with id '{teacherId}' not found or has no salary rate.",
                    statusCode: StatusCodes.Status404NotFound);

            if (await repository.ExistsByTeacherAndPeriodAsync(teacherId, request.Period, ct))
            {
                logger.LogWarning("Payroll for teacher {TeacherId} and period {Period} already exists", teacherId, request.Period);

                return Results.Problem(
                    detail: "Payroll for this teacher and period already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var periodStart = DateTime.SpecifyKind(
                DateTime.ParseExact(request.Period + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTimeKind.Utc);

            var completedLessons = await scheduleLookup.GetCompletedLessonsCountAsync(
                teacherId, periodStart, periodStart.AddMonths(1), ct);

            payroll = TeacherPayroll.CreateForTeacher(teacherId, request.Period, salary.BaseSalary, salary.LessonsRate, completedLessons);
        }
        else
        {
            var userId = request.UserId!.Value;

            var staff = await userDirectory.GetStaffSalaryAsync(userId, ct);
            if (staff is null)
                return Results.Problem(
                    detail: $"Staff member with id '{userId}' not found.",
                    statusCode: StatusCodes.Status404NotFound);

            if (staff.Salary is null)
                return Results.Problem(
                    detail: "This staff member has no salary set yet.",
                    statusCode: StatusCodes.Status409Conflict);

            if (await repository.ExistsByUserAndPeriodAsync(userId, request.Period, ct))
            {
                logger.LogWarning("Payroll for staff member {UserId} and period {Period} already exists", userId, request.Period);

                return Results.Problem(
                    detail: "Payroll for this staff member and period already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            payroll = TeacherPayroll.CreateForStaff(userId, request.Period, staff.Salary.Value);
        }

        await repository.AddAsync(payroll, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Payroll created: {PayrollId} for Teacher {TeacherId} / Staff {UserId}, period {Period}",
            payroll.Id, payroll.TeacherId, payroll.UserId, payroll.Period
        );

        var response = new CreatePayrollResponse(
            payroll.Id,
            payroll.TeacherId,
            payroll.UserId,
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
