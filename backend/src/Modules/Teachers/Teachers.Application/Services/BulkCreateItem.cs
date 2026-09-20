using Teachers.Application.Features.CreateTeacher;

namespace Teachers.Application.Services;

// One teacher to create. Errors are problems found before validation (e.g. an unreadable date in Excel);
// HandledKeys are the fields those errors are about, so validation does not complain about them a second time.
internal sealed record BulkCreateItem(
    CreateTeacherRequest? Request,
    IReadOnlyList<string>? Errors = null,
    IReadOnlySet<string>? HandledKeys = null);
