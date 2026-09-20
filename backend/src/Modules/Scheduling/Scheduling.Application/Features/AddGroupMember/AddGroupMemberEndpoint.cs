using Enrollments.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Features.AddGroupMember;

public sealed record AddGroupMemberRequest(Guid EnrollmentId);

public sealed class AddGroupMemberValidator : AbstractValidator<AddGroupMemberRequest>
{
    public AddGroupMemberValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage("Enrollment is required.");
    }
}

public static class AddGroupMemberEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/members", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("AddStudyGroupMember")
             .WithSummary("Add an enrollment to a study group")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        AddGroupMemberRequest request,
        IValidator<AddGroupMemberRequest> validator,
        IStudyGroupRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        IEnrollmentLookup enrollmentLookup,
        ILogger<AddGroupMemberRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var studyGroup = await repository.GetByIdAsync(id, ct);
        if (studyGroup is null)
            return Results.Problem(
                detail: $"Study group with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var enrollment = await enrollmentLookup.GetByIdAsync(request.EnrollmentId, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{request.EnrollmentId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (enrollment.CourseId != studyGroup.CourseId)
            return Results.Problem(
                detail: "Enrollment belongs to a different course than the group.",
                statusCode: StatusCodes.Status409Conflict
            );

        if (!enrollment.IsActive)
            return Results.Problem(
                detail: "Only an active enrollment can join a group.",
                statusCode: StatusCodes.Status409Conflict
            );

        if (await repository.IsInGroupOfCourseAsync(request.EnrollmentId, studyGroup.CourseId, ct))
        {
            logger.LogWarning(
                "Enrollment {EnrollmentId} is already in a group of course {CourseId}",
                request.EnrollmentId, studyGroup.CourseId
            );

            return Results.Problem(
                detail: "Enrollment is already in a study group of this course.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var member = studyGroup.AddMember(request.EnrollmentId);

        // Added explicitly so EF never treats the new member as an existing row
        await repository.AddMemberAsync(member, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment {EnrollmentId} added to group {GroupId}", request.EnrollmentId, id);

        return Results.NoContent();
    }
}
