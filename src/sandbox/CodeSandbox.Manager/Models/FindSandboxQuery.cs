using System.ComponentModel.DataAnnotations;

namespace CodeSandbox.Manager.Models;

public class FindSandboxQuery
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ConversationId { get; set; } = string.Empty;
}
