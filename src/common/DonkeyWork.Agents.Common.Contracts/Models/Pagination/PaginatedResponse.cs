namespace DonkeyWork.Agents.Common.Contracts.Models.Pagination;

public sealed class PaginatedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Offset { get; init; }

    public required int Limit { get; init; }

    public required int TotalCount { get; init; }

    public bool HasMore => Offset + Items.Count < TotalCount;
}
