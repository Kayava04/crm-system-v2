using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Services;

internal static class TeacherCalendarLock
{
    public static async Task AcquireTeacherCalendarLocksAsync(
        this ITransactionCoordinator transaction,
        IEnumerable<Guid> teacherIds,
        CancellationToken ct)
    {
        foreach (var teacherId in teacherIds.Distinct().Order())
            await transaction.AcquireLockAsync($"teacher-calendar:{teacherId}", ct);
    }
}
