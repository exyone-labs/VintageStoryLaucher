namespace VSL.Domain;

public sealed class ServerCommonSettings
{
    public string ServerName { get; set; } = "Vintage Story Server";

    public string? Ip { get; set; }

    public int Port { get; set; } = 42420;

    public int MaxClients { get; set; } = 16;

    public string? Password { get; set; }

    public bool AdvertiseServer { get; set; }

    public int WhitelistMode { get; set; }

    public bool AllowPvP { get; set; } = true;

    public bool AllowFireSpread { get; set; } = true;

    public bool AllowFallingBlocks { get; set; } = true;
}
