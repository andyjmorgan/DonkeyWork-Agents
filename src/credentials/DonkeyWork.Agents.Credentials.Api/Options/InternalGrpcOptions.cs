using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Credentials.Api.Options;

public class InternalGrpcOptions
{
    public const string SectionName = "InternalGrpc";

    public bool Enabled { get; set; }

    public string ServerCertPath { get; set; } = "/certs/grpc/tls.crt";

    public string ServerKeyPath { get; set; } = "/certs/grpc/tls.key";

    public string CaCertPath { get; set; } = "/certs/grpc/ca.crt";

    public int Port { get; set; } = 8090;
}
