using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Models;
using DonkeyWork.Agents.Actions.Contracts.Types;

namespace DonkeyWork.Agents.Actions.Core.Providers;

/// <summary>
/// HTTP methods supported
/// </summary>
public enum HttpMethod
{
    GET,
    POST,
    PUT,
    DELETE,
    PATCH,
    HEAD,
    OPTIONS
}

/// <summary>
/// Parameters for HTTP Request action
/// </summary>
[ActionNode(
    actionType: "http_request",
    category: "Communication",
    Group = "HTTP",
    Icon = "globe",
    Description = "Make HTTP requests to external APIs",
    DisplayName = "HTTP Request")]
public class HttpRequestParameters : BaseActionParameters
{
    [Required]
    [Display(Name = "Method", Description = "HTTP method to use")]
    [DefaultValue(HttpMethod.GET)]
    public HttpMethod Method { get; set; } = HttpMethod.GET;

    [Required]
    [Display(Name = "URL", Description = "Target URL for the request")]
    [SupportVariables]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Headers", Description = "HTTP headers as key-value pairs or a variable reference")]
    [EditorType(EditorType.KeyValueList)]
    public KeyValueCollection? Headers { get; set; }

    [Display(Name = "Body", Description = "Request body (for POST/PUT/PATCH). Use {{variable}} for dynamic content.")]
    [EditorType(EditorType.Code)]
    [SupportVariables]
    public Resolvable<string>? Body { get; set; }

    [Display(Name = "Timeout (seconds)", Description = "Request timeout in seconds")]
    [Range(1, 300)]
    [DefaultValue(30)]
    [Slider(Step = 1)]
    public Resolvable<int> TimeoutSeconds { get; set; } = 30;

    [Display(Name = "Follow Redirects", Description = "Automatically follow HTTP redirects")]
    [DefaultValue(true)]
    public bool FollowRedirects { get; set; } = true;

    public override (bool valid, List<ValidationResult> results) IsValid()
    {
        return ValidateDataAnnotations();
    }
}

/// <summary>
/// Output from HTTP Request action
/// </summary>
public class HttpRequestOutput
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response body as string
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Response headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Whether the request was successful (2xx status code)
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Duration of the request in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}
