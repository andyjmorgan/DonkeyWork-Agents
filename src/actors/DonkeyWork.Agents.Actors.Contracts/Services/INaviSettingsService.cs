using DonkeyWork.Agents.Actors.Contracts.Models;

namespace DonkeyWork.Agents.Actors.Contracts.Services;

public interface INaviSettingsService
{
    Task<NaviSettingsV1> GetAsync(CancellationToken cancellationToken = default);

    Task<NaviSettingsV1> UpdateAsync(
        UpdateNaviSettingsRequestV1 request,
        CancellationToken cancellationToken = default);
}
