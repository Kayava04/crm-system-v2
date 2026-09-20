using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;
using Students.Application.Features.CreateStudent;
using Students.Application.Services;

namespace Students.Application.Features.BulkCreateStudents;

// AllOrNothing = true: if any student is invalid nothing is saved. Otherwise the valid ones are saved and the rest reported.
public sealed record BulkCreateStudentsRequest(
    List<CreateRequest> Students,
    bool AllOrNothing = false
);

public sealed class BulkCreateStudentsValidator : AbstractValidator<BulkCreateStudentsRequest>
{
    public const int MaxItems = 500;

    public BulkCreateStudentsValidator()
    {
        RuleFor(x => x.Students)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one student is required.")
            .Must(s => s.Count <= MaxItems).WithMessage($"At most {MaxItems} students per request.");
    }
}

public static class BulkCreateStudentsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/bulk", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanCreateStudents))
             .WithName("BulkCreateStudents")
             .WithSummary("Create many students at once; every item gets its own result")
             .Produces<BulkOperationResponse>(StatusCodes.Status200OK)
             .Produces<BulkOperationResponse>(StatusCodes.Status400BadRequest)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        BulkCreateStudentsRequest request,
        IValidator<BulkCreateStudentsRequest> requestValidator,
        StudentBulkCreator creator,
        CancellationToken ct
    )
    {
        var requestValidation = await requestValidator.ValidateAsync(request, ct);
        if (!requestValidation.IsValid)
            return Results.ValidationProblem(requestValidation.ToDictionary());

        var outcome = await creator.CreateAsync(
            request.Students.Select(s => new BulkCreateItem(s)).ToList(),
            request.AllOrNothing,
            dryRun: false,
            ct);

        var response = BulkOperationResponse.From(outcome.Results);

        return outcome.Refused
            ? Results.Json(response, statusCode: StatusCodes.Status400BadRequest)
            : Results.Ok(response);
    }
}
