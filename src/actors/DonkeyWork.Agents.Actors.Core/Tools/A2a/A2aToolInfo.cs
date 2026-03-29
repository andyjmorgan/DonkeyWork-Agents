using DonkeyWork.Agents.A2a.Contracts.Models;
using DonkeyWork.Agents.Actors.Core.Providers;

namespace DonkeyWork.Agents.Actors.Core.Tools.A2a;

internal sealed record A2aToolInfo(
    InternalToolDefinition Definition,
    A2aConnectionConfigV1 ConnectionConfig);
