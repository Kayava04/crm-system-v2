using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Teachers.Application.Abstractions;
using Teachers.Domain.Enums;

namespace Teachers.Application.Features.GetTeacherById;

public sealed record TeacherSalaryRateResponse(
    Guid Id,
    decimal BaseSalary,
    decimal LessonsRate,
    DateTime EffectiveFrom
);

public sealed record TeacherDetailResponse(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    string City,
    string Country,
    TeacherStatus Status,
    string? Comment,
    bool HasAccount,
    TeacherSalaryRateResponse? CurrentSalaryRate,
    IReadOnlyList<TeacherSalaryRateResponse> SalaryRates
);

public static class GetByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewTeachers))
             .WithName("GetTeacherById")
             .WithSummary("Get teacher by id")
             .Produces<TeacherDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ITeacherRepository repository,
        ILogger<TeacherDetailResponse> logger,
        CancellationToken ct
    )
    {
        var teacher = await repository.GetByIdAsync(id, ct);
        if (teacher is null)
            return Results.Problem(
                detail: $"Teacher with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var salaryRates = teacher.SalaryRates
            .OrderByDescending(sr => sr.EffectiveFrom)
            .Select(sr => new TeacherSalaryRateResponse(
                sr.Id,
                sr.BaseSalary,
                sr.LessonsRate,
                sr.EffectiveFrom)
            )
            .ToList();

        var currentRate = teacher.CurrentSalaryRate is not null
            ? new TeacherSalaryRateResponse(
                teacher.CurrentSalaryRate.Id,
                teacher.CurrentSalaryRate.BaseSalary,
                teacher.CurrentSalaryRate.LessonsRate,
                teacher.CurrentSalaryRate.EffectiveFrom
            )
            : null;

        var response = new TeacherDetailResponse(
            teacher.Id,
            teacher.UserId,
            teacher.FirstName,
            teacher.LastName,
            teacher.MiddleName,
            teacher.DateOfBirth,
            teacher.PhoneNumber,
            teacher.Email,
            teacher.City,
            teacher.Country,
            teacher.Status,
            teacher.Comment,
            teacher.UserId is not null,
            currentRate,
            salaryRates
        );

        return Results.Ok(response);
    }
}
