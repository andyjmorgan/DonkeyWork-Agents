namespace DonkeyWork.Agents.Common.Contracts.Models.Pagination;

using Microsoft.AspNetCore.Mvc;

public sealed class PaginationRequest
{
    [FromQuery(Name = "offset")]
    public int Offset { get; init; } = 0;

    [FromQuery(Name = "limit")]
    public int Limit { get; init; } = 50;

    public const int MaxLimit = 100;

    public int GetLimit() => Math.Min(Limit, MaxLimit);
}
