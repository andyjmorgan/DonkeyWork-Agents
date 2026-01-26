using DonkeyWork.Agents.Actions.Contracts.Models.Schema;

namespace DonkeyWork.Agents.Actions.Contracts.Models.Api;

/// <summary>
/// Response model for GET /api/v1/actions/schemas
/// </summary>
public class GetSchemasResponseV1
{
    /// <summary>
    /// List of all available action node schemas
    /// </summary>
    public required List<ActionNodeSchema> Schemas { get; init; }

    /// <summary>
    /// Total number of schemas returned
    /// </summary>
    public required int Count { get; init; }
}
