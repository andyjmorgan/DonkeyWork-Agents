namespace Orleans.Persistence.SeaweedFs.Configuration;

public sealed class SeaweedFsStorageOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8888";

    public string BasePath { get; set; } = "/orleans/grain-state";

    public bool IndentJson { get; set; }
}
