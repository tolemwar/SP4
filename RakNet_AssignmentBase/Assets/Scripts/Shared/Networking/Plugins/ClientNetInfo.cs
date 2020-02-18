using System;

[Serializable]
public class ClientNetInfo
{
    /// <summary>
    /// player name
    /// </summary>
    public string name;

    /// <summary>
    /// local id
    /// </summary>
    public ulong local_id;//8 byte (64 bits)

    /// <summary>
    /// network id
    /// </summary>
    public ulong net_id;//8 byte (64 bits)

    /// <summary>
    /// unique client id
    /// </summary>
    public string client_hwid;

    /// <summary>
    /// client version
    /// </summary>
    public string client_version;
}
