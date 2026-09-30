using Schedule.Web.ViewModels;

namespace Schedule.Web.Services;

public interface IScheduleReferenceService
{
    Task<ReferenceDataVm> LoadReferenceDataAsync(CancellationToken cancellationToken = default);
}
