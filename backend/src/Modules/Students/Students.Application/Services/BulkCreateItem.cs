using Students.Application.Features.CreateStudent;

namespace Students.Application.Services;

// One student to create. Errors are problems found before validation (e.g. an unreadable date in Excel);
// HandledKeys are the fields those errors are about, so validation does not complain about them a second time.
internal sealed record BulkCreateItem(
    CreateRequest? Request,
    IReadOnlyList<string>? Errors = null,
    IReadOnlySet<string>? HandledKeys = null);
