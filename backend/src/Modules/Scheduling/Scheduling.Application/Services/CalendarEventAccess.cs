using System.Security.Claims;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Http;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.Application.Services;

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
