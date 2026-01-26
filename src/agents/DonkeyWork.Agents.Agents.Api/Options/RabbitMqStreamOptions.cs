using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Agents.Api.Options;

public class RabbitMqStreamOptions
{
    public const string SectionName = "RabbitMqStream";

    [Required]
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5552;

    [Required]
    public string Username { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";
}
