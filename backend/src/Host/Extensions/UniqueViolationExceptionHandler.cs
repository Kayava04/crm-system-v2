using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Host.Extensions;

internal sealed class UniqueViolationExceptionHandler(ILogger<UniqueViolationExceptionHandler> logger) : IExceptionHandler
{
    private const string UniqueViolation = "23505";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException { SqlState: UniqueViolation } violation)
            {
                logger.LogWarning("Unique constraint {Constraint} violated: the same record was created twice", violation.ConstraintName);

                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = "A record with the same unique value already exists. It was probably created at the same moment by another request."
                }, ct);

                return true;
            }

            if (current.InnerException is null)
                break;
        }

        return false;
    }
}
