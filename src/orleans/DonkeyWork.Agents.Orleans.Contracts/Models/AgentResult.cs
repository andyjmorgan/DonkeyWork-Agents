using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orleans.Contracts.Models;

[GenerateSerializer]
public sealed record AgentResult([property: Id(0)] List<AgentResultPart> Parts)
{
    public static AgentResult FromText(string text) => new([new AgentTextPart(text)]);
    public static AgentResult Empty => new([]);
}

[GenerateSerializer]
public enum AgentResultPartType
{
    Text,
    Citation,
}

[GenerateSerializer]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AgentTextPart), "text")]
[JsonDerivedType(typeof(AgentCitationPart), "citation")]
public abstract record AgentResultPart([property: Id(0)] AgentResultPartType Type);

[GenerateSerializer]
public sealed record AgentTextPart([property: Id(1)] string Text) : AgentResultPart(AgentResultPartType.Text);

[GenerateSerializer]
public sealed record AgentCitationPart(
    [property: Id(1)] string Title,
    [property: Id(2)] string Url,
    [property: Id(3)] string CitedText) : AgentResultPart(AgentResultPartType.Citation);
