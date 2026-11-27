using System.ComponentModel.DataAnnotations;

using GuardPost.Common.Extensions;
using GuardPost.Data.Models.Enums;
using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.Incidents;
using GuardPost.Web.ViewModels.LeaveRequests;
using GuardPost.Web.ViewModels.Shifts;

namespace GuardPost.Services.Tests;

public class CommonAndValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("2026-10-12", "2026-10-12")]
    [InlineData("2026-10-14", "2026-10-12")]
    [InlineData("2026-10-18", "2026-10-12")]
    [InlineData("2026-10-19", "2026-10-19")]
    public void StartOfWeek_ReturnsMonday(string date, string expectedMonday)
    {
        Assert.Equal(DateTime.Parse(expectedMonday), DateTime.Parse(date).AddHours(15).StartOfWeek());
    }

    [Fact]
    public void OverlapHours_ClipsIntervalToRange()
    {
        var rangeStart = new DateTime(2026, 10, 1);
        var rangeEnd = new DateTime(2026, 11, 1);

        Assert.Equal(8, DateTimeExtensions.OverlapHours(new DateTime(2026, 9, 30, 20, 0, 0), new DateTime(2026, 10, 1, 8, 0, 0), rangeStart, rangeEnd));
        Assert.Equal(0, DateTimeExtensions.OverlapHours(new DateTime(2026, 9, 1), new DateTime(2026, 9, 2), rangeStart, rangeEnd));
        Assert.Equal(new DateTime(2026, 10, 1), new DateTime(2026, 10, 25).StartOfMonth());
    }

    [Fact]
    public void GetDisplayName_UsesDisplayAttributeOrName()
    {
        Assert.Equal("Старши охранител", GuardRank.SeniorGuard.GetDisplayName());
        Assert.Equal("В процес", IncidentStatus.InProgress.GetDisplayName());
        Assert.Equal("Monday", DayOfWeek.Monday.GetDisplayName());
    }

    [Fact]
    public void DateAfter_RejectsEndBeforeOrEqualToStart()
    {
        var start = new DateTime(2026, 10, 20, 8, 0, 0);
        var invalid = new ShiftFormViewModel { SiteId = 1, GuardId = 1, StartTime = start, EndTime = start };
        var valid = new ShiftFormViewModel { SiteId = 1, GuardId = 1, StartTime = start, EndTime = start.AddHours(8) };

        Assert.Contains(Validate(invalid), r => r.MemberNames.Contains(nameof(ShiftFormViewModel.EndTime)));
        Assert.Empty(Validate(valid));
    }

    [Fact]
    public void DateAfter_WithAllowEqual_AcceptsOneDayLeave()
    {
        var oneDay = new LeaveRequestFormViewModel { StartDate = new DateTime(2026, 11, 2), EndDate = new DateTime(2026, 11, 2), Type = LeaveType.Paid };
        var reversed = new LeaveRequestFormViewModel { StartDate = new DateTime(2026, 11, 3), EndDate = new DateTime(2026, 11, 2), Type = LeaveType.Paid };

        Assert.Empty(Validate(oneDay));
        Assert.Single(Validate(reversed));
    }

    [Fact]
    public void IncidentForm_ResolvedWithoutNotes_IsInvalid_AndFutureDateIsRejected()
    {
        var model = new IncidentFormViewModel
        {
            Title = "Счупен прозорец",
            Description = "Задният вход е повреден.",
            OccurredOn = DateTime.Now.AddDays(2),
            Severity = IncidentSeverity.Low,
            Status = IncidentStatus.Resolved,
            SiteId = 1,
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(IncidentFormViewModel.OccurredOn)));

        model.OccurredOn = DateTime.Now.AddHours(-1);
        var resolutionErrors = model.Validate(new ValidationContext(model)).ToList();
        Assert.Single(resolutionErrors);

        model.ResolutionNotes = "Подменен.";
        Assert.Empty(model.Validate(new ValidationContext(model)));
    }

    [Fact]
    public void PagedResultAndOperationResult_ComputeTheirState()
    {
        var page = new PagedResult<int>(new[] { 1, 2 }, totalCount: 5, page: 2, pageSize: 2);
        var empty = new PagedResult<int>(Array.Empty<int>(), totalCount: 0, page: 1, pageSize: 10);

        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal(1, empty.TotalPages);
        Assert.False(empty.HasNextPage);

        Assert.True(OperationResult.Success().Succeeded);
        var failure = OperationResult.Failure("грешка");
        Assert.False(failure.Succeeded);
        Assert.Equal("грешка", failure.Error);
    }
}
