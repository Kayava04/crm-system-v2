using Teachers.Application.Features.CreateTeacher;

namespace Teachers.Application.Services;

internal sealed record BulkCreateItem(
    CreateTeacherRequest? Request,
    IReadOnlyList<string>? Errors = null,
    IReadOnlySet<string>? HandledKeys = null);
