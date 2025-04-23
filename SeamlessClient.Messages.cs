using ProtoBuf;

namespace SeamlessClient.Messages
{
    public enum ClientMessageType
    {
        FirstJoin,
        TransferServer,
        OnlinePlayers,
    }

    [ProtoContract]
    public class ClientMessage
    {
        [ProtoMember(1)] public ClientMessageType MessageType;
        [ProtoMember(2)] public byte[] MessageData;
        [ProtoMember(3)] public long IdentityID;
        [ProtoMember(4)] public ulong SteamID;
        [ProtoMember(5)] public string PluginVersion = "0";
        [ProtoMember(6)] public string NexusVersion;
    }
}