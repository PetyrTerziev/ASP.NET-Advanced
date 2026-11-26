using GuardPost.Web.ViewModels.Home;

namespace GuardPost.Services.Core.Contracts;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync();

    /// <summary>Lightweight data for the AJAX refresh of the coverage widget.</summary>
    Task<IReadOnlyList<SiteCoverageViewModel>> GetCoverageAsync();
}
