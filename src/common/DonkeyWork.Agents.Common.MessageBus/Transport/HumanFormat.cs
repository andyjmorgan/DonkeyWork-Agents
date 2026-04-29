namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public static class HumanFormat
{
    public static string Bytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.##} MB";
    }

    public static string Ms(double ms) => $"{ms:0.#} ms";

    public static string ShortId(string id) => id.Length > 8 ? id[..8] : id;
}
