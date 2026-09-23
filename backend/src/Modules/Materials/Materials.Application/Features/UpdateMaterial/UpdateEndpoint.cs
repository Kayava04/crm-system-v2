using System.Security.Claims;
using FluentValidation;
using Identity.Contracts.Enums;
using Materials.Application.Abstractions;
using Materials.Application.Services;
using Materials.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Materials.Application.Features.UpdateMaterial;

public sealed record UpdateMaterialRequest(
    MaterialType Type,
    string Title,
    string? Description,
    string? Body,
    string? Url
);

public sealed class UpdateMaterialValidator : AbstractValidator<UpdateMaterialRequest>
{
    public UpdateMaterialValidator()
    {
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

public static class UpdateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageMaterials))
             .WithName("UpdateMaterial")
             .WithSummary("Update a material (author or admin only)")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateMaterialRequest request,
        ClaimsPrincipal user,
        IValidator<UpdateMaterialRequest> validator,
        IMaterialRepository repository,
        IMaterialsUnitOfWork unitOfWork,
        ILogger<UpdateMaterialRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var material = await repository.GetByIdAsync(id, ct);
        if (material is null)
            return Results.Problem(
                detail: $"Material with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!MaterialAccess.CanModify(user, material))
        {
            logger.LogWarning("User {UserId} tried to update foreign material {MaterialId}",
                MaterialAccess.GetUserId(user), id);

            return Results.Problem(
                detail: "Only the author or an administrator can change this material.",
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        var (body, url, videoId) = MaterialContent.Resolve(request.Type, request.Body, request.Url);

        material.Update(
            request.Type,
            request.Title.Trim(),
            request.Description?.Trim(),
            body,
            url,
            videoId
        );

        await repository.UpdateAsync(material, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Material updated: {MaterialId}", id);

        return Results.NoContent();
    }
}
