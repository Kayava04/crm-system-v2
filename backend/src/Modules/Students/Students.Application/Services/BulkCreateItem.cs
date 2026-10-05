using Students.Application.Features.CreateStudent;

namespace Students.Application.Services;

internal sealed record BulkCreateItem(
    CreateRequest? Request,
    IReadOnlyList<string>? Errors = null,
    IReadOnlySet<string>? HandledKeys = null);
