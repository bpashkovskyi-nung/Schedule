using Microsoft.AspNetCore.Mvc;
using Schedule.Api.Services;
using Schedule.Contracts.ReferenceData;

namespace Schedule.Api.Controllers;

[Route("api/v1/reference")]
public sealed class ReferenceController(IScheduleApiService scheduleService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ReferenceDataResponse>(StatusCodes.Status200OK)]
    public Task<ReferenceDataResponse> Get(CancellationToken cancellationToken) =>
        scheduleService.GetReferenceDataAsync(cancellationToken);
}
