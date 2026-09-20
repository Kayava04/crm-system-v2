using Billing.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Billing.Domain.Entities;

public sealed class StudentInvoice : AuditableEntity
{
    public Guid EnrollmentId { get; private set; }
    public Guid StudentId { get; private set; }
    public string Period { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private StudentInvoice() { }

    public static StudentInvoice Create(
        Guid enrollmentId,
        Guid studentId,
        string period,
        decimal amount,
        DateOnly dueDate,
        string? notes = null
    )
    {
        return new StudentInvoice
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            Period = period,
            Amount = amount,
            DueDate = dueDate,
            Status = InvoiceStatus.Pending,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkPaid()
    {
        Status = InvoiceStatus.Paid;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkOverdue()
    {
        Status = InvoiceStatus.Overdue;
        UpdatedAt = DateTime.UtcNow;
    }
}
