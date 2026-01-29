using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Models;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Actions.Contracts.Types;
using Scriban;

namespace DonkeyWork.Agents.Actions.Core.Providers;

/// <summary>
/// Parameters for Message Formatter action
/// </summary>
[ActionNode(
    actionType: "message_formatter",
    category: "Utilities",
    Group = "Text Processing",
    Icon = "file-text",
    Description = "Format messages using Scriban templates with access to previous node outputs",
    DisplayName = "Message Formatter")]
public class MessageFormatterParameters : BaseActionParameters
{
    [Required]
    [Display(Name = "Template", Description = "Scriban template. Use {{Input}}, {{Steps.nodeName}}, {{ExecutionId}}, {{UserId}} for variables.")]
    [EditorType(EditorType.Code)]
    [SupportVariables]
    public Resolvable<string> Template { get; set; }

    public override (bool valid, List<ValidationResult> results) IsValid()
    {
        return ValidateDataAnnotations();
    }
}

/// <summary>
/// Output from Message Formatter action
/// </summary>
public class MessageFormatterOutput
{
    /// <summary>
    /// The formatted message after template rendering
    /// </summary>
    public string FormattedMessage { get; set; } = string.Empty;
}

/// <summary>
/// Provider for message formatting using Scriban templates
/// </summary>
[ActionProvider]
public class MessageFormatterActionProvider
{
    private readonly IParameterResolver _parameterResolver;

    public MessageFormatterActionProvider(IParameterResolver parameterResolver)
    {
        _parameterResolver = parameterResolver;
    }

    [ActionMethod("message_formatter")]
    public async Task<MessageFormatterOutput> ExecuteAsync(
        MessageFormatterParameters parameters,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        var templateString = _parameterResolver.Resolve(parameters.Template, context);

        string formattedMessage;
        try
        {
            var template = Template.Parse(templateString);

            if (template.HasErrors)
            {
                var errors = string.Join("; ", template.Messages.Select(m => m.Message));
                throw new InvalidOperationException($"Template parsing errors: {errors}");
            }

            formattedMessage = await template.RenderAsync(context);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to render message formatter template: {ex.Message}", ex);
        }

        return new MessageFormatterOutput
        {
            FormattedMessage = formattedMessage
        };
    }
}
