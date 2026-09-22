using System.Security.Claims;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Http;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.Application.Services;

// Shared by UpdateCalendarEvent and DeleteCalendarEvent: a personal event is the owner's alone to change
// (anyone else gets 404, the same as if it did not exist to them); an event visible to everyone is a
// schedule manager's to change, regardless of who originally created it.
public static class CalendarEventAccess
{
    public static IResult? CheckCanManage(CalendarEvent calendarEvent, ClaimsPrincipal principal, Guid userId)
    {
        if (calendarEvent.Visibility == CalendarEventVisibility.Personal)
            return calendarEvent.CreatedByUserId == userId
                ? null
                : Results.Problem(detail: $"Calendar event with id '{calendarEvent.Id}' not found.", statusCode: StatusCodes.Status404NotFound);

        return principal.HasClaim("permission", nameof(SystemPermission.CanManageSchedule))
            ? null
            : Results.Problem(detail: "Only a schedule manager can change an event visible to everyone.", statusCode: StatusCodes.Status403Forbidden);
    }
}
