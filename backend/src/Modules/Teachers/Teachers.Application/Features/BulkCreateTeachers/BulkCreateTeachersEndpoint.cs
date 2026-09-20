using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;
using Teachers.Application.Features.CreateTeacher;
using Teachers.Application.Services;

namespace Teachers.Application.Features.BulkCreateTeachers;

// AllOrNothing = true: if any teacher is invalid nothing is saved. Otherwise the valid ones are saved and the rest reported.
public sealed record BulkCreateTeachersRequest(
    List<CreateTeacherRequest> Teachers,
    bool AllOrNothing = false
);

public sealed class BulkCreateTeachersValidator : AbstractValidator<BulkCreateTeachersRequest>
{
    public const int MaxItems = 500;

    public BulkCreateTeachersValidator()
    {
        RuleFor(x => x.Teachers)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one teacher is required.")
            .Must(t => t.Count <= MaxItems).WithMessage($"At most {MaxItems} teachers per request.");
    }
}

public static class BulkCreateTeachersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/bulk", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanCreateTeachers))
             .WithName("BulkCreateTeachers")
             .WithSummary("Create many teachers at once; every item gets its own result")
             .Produces<BulkOperationResponse>(StatusCodes.Status200OK)
             .Produces<BulkOperationResponse>(StatusCodes.Status400BadRequest)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        BulkCreateTeachersRequest request,
        IValidator<BulkCreateTeachersRequest> requestValidator,
        TeacherBulkCreator creator,
        CancellationToken ct
    )
    {
        var requestValidation = await requestValidator.ValidateAsync(request, ct);
        if (!requestValidation.IsValid)
            return Results.ValidationProblem(requestValidation.ToDictionary());

        var outcome = await creator.CreateAsync(
            request.Teachers.Select(t => new BulkCreateItem(t)).ToList(),
            request.AllOrNothing,
            dryRun: false,
            ct);

        var response = BulkOperationResponse.From(outcome.Results);

        return outcome.Refused
            ? Results.Json(response, statusCode: StatusCodes.Status400BadRequest)
            : Results.Ok(response);
    }
}
