using Courses.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Teachers.Contracts;

namespace Scheduling.Application.Features.CreateGroup;

public sealed record CreateGroupRequest(
    Guid CourseId,
    Guid TeacherId,
    string Name
);

public sealed record CreateGroupResponse(
    Guid Id,
    Guid CourseId,
    Guid TeacherId,
    string Name
);

public sealed class CreateGroupValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course is required.");

        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("Teacher is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}

public static class CreateGroupEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("CreateStudyGroup")
             .WithSummary("Create a study group for a group course")
             .Produces<CreateGroupResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateGroupRequest request,
        IValidator<CreateGroupRequest> validator,
        IStudyGroupRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ICourseLookup courseLookup,
        ITeacherVerifier teacherVerifier,
        ILogger<CreateGroupRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var course = await courseLookup.GetByIdAsync(request.CourseId, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{request.CourseId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!course.IsGroup)
        {
            logger.LogWarning("Study group requested for non-group course {CourseId}", request.CourseId);

            return Results.Problem(
                detail: "Study groups can only be created for group courses.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var teacherExists = await teacherVerifier.ExistsAsync(request.TeacherId, ct);
        if (!teacherExists)
            return Results.Problem(
                detail: $"Teacher with id '{request.TeacherId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var studyGroup = StudyGroup.Create(request.CourseId, request.TeacherId, request.Name.Trim());

        await repository.AddAsync(studyGroup, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Study group created: {GroupId} for Course {CourseId}", studyGroup.Id, studyGroup.CourseId);

        var response = new CreateGroupResponse(studyGroup.Id, studyGroup.CourseId, studyGroup.TeacherId, studyGroup.Name);

        return Results.Created($"/api/study-groups/{studyGroup.Id}", response);
    }
}
