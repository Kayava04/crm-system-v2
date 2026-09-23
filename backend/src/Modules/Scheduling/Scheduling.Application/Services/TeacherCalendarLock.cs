using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Services;

// A teacher's calendar is protected by one named lock, so "is the teacher free?" followed by "book the lesson"
// cannot be done by two requests at the same moment. Locks are always taken in sorted order to avoid deadlocks.
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
