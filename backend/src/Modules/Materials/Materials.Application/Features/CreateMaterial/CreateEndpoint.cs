using System.Security.Claims;
using Courses.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Materials.Application.Abstractions;
using Materials.Application.Features.GetMaterialById;
using Materials.Application.Services;
using Materials.Domain.Entities;
using Materials.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Materials.Application.Features.CreateMaterial;

// Video: Url is a YouTube link. Article: Body is the text. Link: Url is any http(s) address.
public sealed record CreateMaterialRequest(
    Guid CourseId,
    MaterialType Type,
    string Title,
    string? Description,
    string? Body,
    string? Url
);

public sealed class CreateMaterialValidator : AbstractValidator<CreateMaterialRequest>
{
    public CreateMaterialValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid material type.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Url)
            .Must(url => YouTubeUrl.TryGetVideoId(url) is not null)
            .WithMessage("A valid YouTube link is required.")
            .When(x => x.Type == MaterialType.Video);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Article text is required.")
            .MaximumLength(20000).WithMessage("Article text must not exceed 20000 characters.")
            .When(x => x.Type == MaterialType.Article);

        RuleFor(x => x.Url)
            .Must(MaterialContent.IsValidHttpUrl).WithMessage("A valid http(s) link is required.")
            .MaximumLength(2000).WithMessage("Link must not exceed 2000 characters.")
            .When(x => x.Type == MaterialType.Link);
    }
}

public static class CreateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageMaterials))
             .WithName("CreateMaterial")
             .WithSummary("Create a material (YouTube video, article or link)")
             .Produces<MaterialDetailResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        CreateMaterialRequest request,
        ClaimsPrincipal user,
        IValidator<CreateMaterialRequest> validator,
        IMaterialRepository repository,
        IMaterialsUnitOfWork unitOfWork,
        ICourseLookup courseLookup,
        ILogger<CreateMaterialRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var authorId = MaterialAccess.GetUserId(user);
        if (authorId is null)
            return Results.Problem(
                detail: "Invalid user identity.",
                statusCode: StatusCodes.Status401Unauthorized
            );

        var course = await courseLookup.GetByIdAsync(request.CourseId, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{request.CourseId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var (body, url, videoId) = MaterialContent.Resolve(request.Type, request.Body, request.Url);

        var material = Material.Create(
            request.CourseId,
            authorId.Value,
            request.Type,
            request.Title.Trim(),
            request.Description?.Trim(),
            body,
            url,
            videoId
        );

        await repository.AddAsync(material, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Material created: {MaterialId} ({Type}) for Course {CourseId}",
            material.Id, material.Type, material.CourseId);

        return Results.Created($"/api/materials/{material.Id}", MaterialDetailResponse.From(material));
    }
}
