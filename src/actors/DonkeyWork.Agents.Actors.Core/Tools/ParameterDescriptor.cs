namespace DonkeyWork.Agents.Actors.Core.Tools;

internal sealed class ParameterDescriptor
{
    public required string Name { get; init; }

    public required Type ClrType { get; init; }

    public required string JsonType { get; init; }

    public string? Description { get; init; }

    public string[]? AllowedValues { get; init; }

    public required bool IsRequired { get; init; }
}
