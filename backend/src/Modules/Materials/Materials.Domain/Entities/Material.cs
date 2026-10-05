using Materials.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Materials.Domain.Entities;

public sealed class Material : AuditableEntity
{
    public Guid CourseId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public MaterialType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public string? Body { get; private set; }

    public string? Url { get; private set; }

    public string? YouTubeVideoId { get; private set; }

    private Material() { }

    public static Material Create(
        Guid courseId,
        Guid authorUserId,
        MaterialType type,
        string title,
        string? description,
        string? body,
        string? url,
        string? youTubeVideoId
    )
    {
        return new Material
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            AuthorUserId = authorUserId,
            Type = type,
            Title = title,
            Description = description,
            Body = body,
            Url = url,
            YouTubeVideoId = youTubeVideoId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        MaterialType type,
        string title,
        string? description,
        string? body,
        string? url,
        string? youTubeVideoId
    )
    {
        Type = type;
        Title = title;
        Description = description;
        Body = body;
        Url = url;
        YouTubeVideoId = youTubeVideoId;
        UpdatedAt = DateTime.UtcNow;
    }
}
