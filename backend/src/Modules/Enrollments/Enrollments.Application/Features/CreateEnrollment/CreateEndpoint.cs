using Enrollments.Application.Abstractions;
using Enrollments.Domain.Entities;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Courses.Contracts;
using Students.Contracts;

namespace Enrollments.Application.Features.CreateEnrollment;

public sealed record CreateEnrollmentRequest(
    Guid StudentId,
    Guid CourseId,
    DateOnly StartDate,
    decimal? DiscountedPrice,
    string? Comment
);

public sealed record CreateEnrollmentResponse(
    Guid Id,
    string EnrollmentNumber,
    Guid StudentId,
    Guid CourseId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal CoursePrice,
    decimal? DiscountedPrice,
    decimal EffectivePrice
);

public sealed class CreateEnrollmentValidator : AbstractValidator<CreateEnrollmentRequest>
{
    public CreateEnrollmentValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student is required.");

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.DiscountedPrice)
            .GreaterThan(0).WithMessage("Discounted price must be greater than 0.")
            .When(x => x.DiscountedPrice.HasValue);

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => x.Comment is not null);
    }
}

public static class CreateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("CreateEnrollment")
             .WithSummary("Create an enrollment")
             .Produces<CreateEnrollmentResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateEnrollmentRequest request,
        IValidator<CreateEnrollmentRequest> validator,
        IEnrollmentRepository repository,
        IEnrollmentNumberGenerator numberGenerator,
        IEnrollmentUnitOfWork unitOfWork,
        ICourseLookup courseLookup,
        IStudentVerifier studentVerifier,
        ILogger<CreateEnrollmentRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var studentExists = await studentVerifier.ExistsAsync(request.StudentId, ct);
        if (!studentExists)
            return Results.Problem(
                detail: $"Student with id '{request.StudentId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var course = await courseLookup.GetByIdAsync(request.CourseId, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{request.CourseId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var alreadyEnrolled = await repository.ExistsByStudentAndCourseAsync(request.StudentId, request.CourseId, ct);

        if (alreadyEnrolled)
            return Results.Problem(
                detail: "Student is already enrolled in this course.",
                statusCode: StatusCodes.Status409Conflict
            );

        var enrollmentNumber = await numberGenerator.GenerateAsync(request.StartDate, ct);

        var enrollment = Enrollment.Create(
            enrollmentNumber,
            request.StudentId,
            request.CourseId,
            request.StartDate,
            course.DurationMonths,
            course.Price,
            request.DiscountedPrice,
            request.Comment
        );

        await repository.AddAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment created: {EnrollmentNumber} for Student {StudentId}", enrollmentNumber, request.StudentId);

        var response = new CreateEnrollmentResponse(
            enrollment.Id,
            enrollment.EnrollmentNumber,
            enrollment.StudentId,
            enrollment.CourseId,
            enrollment.StartDate,
            enrollment.EndDate,
            enrollment.CoursePrice,
            enrollment.DiscountedPrice,
            enrollment.EffectivePrice
        );

        return Results.Created($"/api/enrollments/{enrollment.Id}", response);
    }
}
