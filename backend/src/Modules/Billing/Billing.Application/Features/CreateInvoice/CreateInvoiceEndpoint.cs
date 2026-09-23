using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Enrollments.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Billing.Application.Features.CreateInvoice;

public sealed record CreateInvoiceRequest(
    Guid EnrollmentId,
    string Period,
    DateOnly DueDate,
    string? Notes
);

public sealed record CreateInvoiceResponse(
    Guid Id,
    Guid EnrollmentId,
    Guid StudentId,
    string Period,
    decimal Amount,
    DateOnly DueDate,
    InvoiceStatus Status
);

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage("Enrollment is required.");

        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("Period is required.")
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$").WithMessage("Period must be in format YYYY-MM.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => x.Notes is not null);
    }
}

public static class CreateInvoiceEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManagePayments))
             .WithName("CreateInvoice")
             .WithSummary("Create a student invoice")
             .Produces<CreateInvoiceResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateInvoiceRequest request,
        IValidator<CreateInvoiceRequest> validator,
        IStudentInvoiceRepository repository,
        IBillingUnitOfWork unitOfWork,
        IEnrollmentLookup enrollmentLookup,
        ILogger<CreateInvoiceRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var enrollment = await enrollmentLookup.GetByIdAsync(request.EnrollmentId, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{request.EnrollmentId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var exists = await repository.ExistsByEnrollmentAndPeriodAsync(request.EnrollmentId, request.Period, ct);
        if (exists)
        {
            logger.LogWarning(
                "Invoice for enrollment {EnrollmentId} and period {Period} already exists",
                request.EnrollmentId, request.Period
            );

            return Results.Problem(
                detail: "Invoice for this enrollment and period already exists.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var invoice = StudentInvoice.Create(
            enrollment.Id,
            enrollment.StudentId,
            request.Period,
            enrollment.EffectivePrice,
            request.DueDate,
            request.Notes
        );

        await repository.AddAsync(invoice, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Invoice created: {InvoiceId} for Enrollment {EnrollmentId}, period {Period}",
            invoice.Id, invoice.EnrollmentId, invoice.Period
        );

        var response = new CreateInvoiceResponse(
            invoice.Id,
            invoice.EnrollmentId,
            invoice.StudentId,
            invoice.Period,
            invoice.Amount,
            invoice.DueDate,
            invoice.Status
        );

        return Results.Created($"/api/billing/invoices/{invoice.Id}", response);
    }
}
