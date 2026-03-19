using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class CreateSandboxCustomVariableRequestV1
{
    [Required]
    [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "Key must contain only uppercase letters, numbers, and underscores.")]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public bool IsSecret { get; set; }
}
