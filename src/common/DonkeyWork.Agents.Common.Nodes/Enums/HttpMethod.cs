using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Nodes.Enums;

/// <summary>
/// HTTP methods supported by the HttpRequest node.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete,
    Head,
    Options
}
