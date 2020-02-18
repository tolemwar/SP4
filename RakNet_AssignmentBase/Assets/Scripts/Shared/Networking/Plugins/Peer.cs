using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;

[SuppressUnmanagedCodeSecurity]
public class Peer
{

    public IntPtr ptr;

    private Timer update_timer = new Timer();

    public Peer()
    {
        update_timer.Elapsed += UpdateStats;
        update_timer.Interval = 1000;
        update_timer.Start();
    }

    public int TOTAL_SENDED_BYTES = 0;
    public int TOTAL_RECEIVED_BYTES = 0;
    public float LOSS = 0;
    public int BYTES_IN = 0;
    public int BYTES_OUT = 0;

    public void UpdateStats(object e, ElapsedEventArgs ar)
    {
        BYTES_IN = TOTAL_RECEIVED_BYTES - OLD_RECV;
        BYTES_OUT = TOTAL_SENDED_BYTES - OLD_SND;

        OLD_RECV = TOTAL_RECEIVED_BYTES;
        OLD_SND = TOTAL_SENDED_BYTES;

        if(Client_Demo.Instance.m_ClientNetInfo.local_id != 0)
        {
            LOSS = GetStatistics(Client_Demo.Instance.m_ClientNetInfo.local_id).packetlossLastSecond * 100;
        }
    }

    private int OLD_SND = 0;
    private int OLD_RECV = 0;

    public virtual ulong incomingGUID
    {
        get
        {
            Check();
            return RakNet_Native.NETRCV_GUID(ptr);
        }
    }

    public virtual uint incomingAddressInt
    {
        get
        {
            Check();
            return RakNet_Native.NETRCV_Address(ptr);
        }
    }

    public virtual uint incomingPort
    {
        get
        {
            Check();
            return RakNet_Native.NETRCV_Port(ptr);
        }
    }

    public string incomingAddress => GetAddress(incomingGUID);

    public virtual int incomingBits
    {
        get
        {
            Check();
            return RakNet_Native.NETRCV_LengthBits(ptr);
        }
    }

    public virtual int incomingBitsUnread
    {
        get
        {
            Check();
            return RakNet_Native.NETRCV_UnreadBits(ptr);
        }
    }

    public virtual int incomingBytes => incomingBits / 8;

    public virtual int incomingBytesUnread => incomingBitsUnread / 8;

    public static Peer CreateServer(string ip, int port, int maxConnections)
    {
        Peer peer = new Peer();
        peer.ptr = RakNet_Native.NET_Create();

        if (RakNet_Native.NET_StartServer(peer.ptr, ip, port, maxConnections) == 0)
        {
            return peer;
        }
        peer.Close();
        string text = StringFromPointer(RakNet_Native.NET_LastStartupError(peer.ptr));
        Debug.LogWarning("[RakNet] Couldn't create server on port " + port + " (" + text + ")");
        return null;
    }

    public static Peer CreateConnection(string hostname, int port, int retries, int retryDelay, int timeout)
    {
        Peer peer = new Peer();
        peer.ptr = RakNet_Native.NET_Create();

        if (RakNet_Native.NET_StartClient(peer.ptr, hostname, port, retries, retryDelay*100, timeout*100) == 0)
        {
            Debug.Log("[RakNet] Peer created connection to "+hostname+":"+port+" with "+retries+" retry count [delay: "+retryDelay+"] [Timeout: "+timeout+"]");
            return peer;
        }

        string text = StringFromPointer(RakNet_Native.NET_LastStartupError(peer.ptr));
        Debug.LogWarning("[RakNet] Couldn't connect to server " + hostname + ":" + port + " (" + text + ")");
        peer.Close();
        peer = null;
        return null;
    }

    public void Close()
    {
        if (ptr != IntPtr.Zero)
        {
            RakNet_Native.NET_Close(ptr);
            ptr = IntPtr.Zero;
        }
    }

    public bool Receive()
    {
        if (ptr == IntPtr.Zero)
        {
            return false;
        }
        return RakNet_Native.NET_Receive(ptr);
    }

    public virtual void SetReadPos(int bitsOffset)
    {
        Check();
        RakNet_Native.NETRCV_SetReadPointer(ptr, bitsOffset);
    }

    protected unsafe virtual bool Read(byte* data, int length,bool compressed)
    {
        Check();
        return RakNet_Native.NETRCV_ReadBytes(ptr, data, length);
    }
    private static readonly byte[] ReadBuffer = new byte[1024];
    private static readonly byte[] ByteBuffer = new byte[512];

    public unsafe byte ReadByte()
    {
        Check();
        fixed (byte* data = ByteBuffer)
        {
            if (!RakNet_Native.NETRCV_ReadBytes(ptr, data, 1))
            {
                Debug.LogWarning("[RakNet] NETRCV_ReadBytes returned false");
                return 0;
            }
            return ByteBuffer[0];
        }
    }

    public byte ReadUInt8()
    {
        return ReadByte();
    }

    public bool ReadBit()
    {
        return ReadUInt8() > 0U;
    }

    public sbyte ReadInt8()
    {
        return (sbyte)ReadUInt8();
    }

    private unsafe byte* Read(int size, byte* data)
    {
        Check();
        if (size > ReadBuffer.Length)
        {
            throw new Exception("[RakNet] Size > ReadBuffer.Length");
        }

        if (RakNet_Native.NETRCV_ReadBytes(ptr, data, size))
            return data;

        Debug.LogWarning("[RakNet] NETRCV_ReadBytes returned false");
        return null;
    }

    public unsafe int ReadBytes(byte[] buffer, int offset, int length,bool compressed)
    {
        fixed (byte* ptr = &((buffer != null && buffer.Length != 0) ? ref buffer[0] : ref *(byte*)null))
        {
            if (!Read((byte*)((long)ptr + offset), length,compressed))
            {
                return 0;
            }
        }
        return length;
    }

    public unsafe MemoryStream ReadBytes(int length,bool compressed)
    {
        Check();
        MemoryStream memoryStream = new MemoryStream();

        if (length == -1)
            length = incomingBytesUnread;
        if (memoryStream.Capacity < length)
            memoryStream.Capacity = length + 32;
        fixed (byte* data = memoryStream.GetBuffer())
        {
            memoryStream.SetLength(memoryStream.Capacity);

            if (!RakNet_Native.NETRCV_ReadBytes(ptr, data, length))
            {
                Debug.LogWarning("[RakNet] NETRCV_ReadBytes returned false");
                return null;
            }
            memoryStream.SetLength(length);
            return memoryStream;
        }
    }

    public unsafe int ReadBytes(MemoryStream memoryStream, int length)
    {
        Check();
        if (length == -1)
            length = incomingBytesUnread;
        if (memoryStream.Capacity < length)
            memoryStream.Capacity = length + 32;
        fixed (byte* data = memoryStream.GetBuffer())
        {
            memoryStream.SetLength(memoryStream.Capacity);
            if (!RakNet_Native.NETRCV_ReadBytes(ptr, data, length))
            {
                Debug.LogWarning("[RakNet] NETRCV_ReadBytes returned false");
                return 0;
            }
            memoryStream.SetLength(length);
            return length;
        }
    }

    public virtual void SendStart()
    {
        Check();
        RakNet_Native.NETSND_Start(ptr);
    }

    public unsafe void WriteByte(byte val)
    {
        Write(&val, 1);
    }

    public unsafe void WriteBytes(byte[] val, int offset, int length)
    {
        fixed (byte* ptr = &((val != null && val.Length != 0) ? ref val[0] : ref *(byte*)null))
        {
            Write((byte*)((long)ptr + offset), length);
        }
    }

    public unsafe void WriteBytes(byte[] val)
    {
        fixed (byte* data = &((val != null && val.Length != 0) ? ref val[0] : ref *(byte*)null))
        {
            Write(data, val.Length);
        }
    }

    public void WriteBytes(MemoryStream stream)
    {
        WriteBytes(stream.ToArray(), 0, (int)stream.Length);
    }

    protected unsafe virtual void Write(byte* data, int size)
    {
        if (size != 0 && data != null)
        {
            Check();
            RakNet_Native.NETSND_WriteBytes(ptr, data, size);
        }
    }

    //COMPRESSED READ/WRITE START
    public void WriteCompressedFloat(float value)
    {
        Check();
        RakNet_Native.NETSND_WriteCompressedFloat(ptr,value);
    }

    public void WriteCompressedInt32(int value)
    {
        Check();
        RakNet_Native.NETSND_WriteCompressedInt32(ptr, value);
    }

    public void WriteCompressedInt64(long value)
    {
        Check();
        RakNet_Native.NETSND_WriteCompressedInt64(ptr, value);
    }

    public float ReadCompressedFloat
    {
        get
        {
            Check();
            return RakNet_Native.NETSND_ReadCompressedFloat(ptr);
        }
    }

    public float ReadCompressedInt32
    {
        get
        {
            Check();
            return RakNet_Native.NETSND_ReadCompressedInt32(ptr);
        }
    }

    public float ReadCompressedInt64
    {
        get
        {
            Check();
            return RakNet_Native.NETSND_ReadCompressedInt64(ptr);
        }
    }
    //COMPRESSED READ/WRITE END

    public virtual uint SendBroadcast(Priority priority, Reliability reliability, sbyte channel)
    {
        Check();
        return RakNet_Native.NETSND_Broadcast(ptr, ToRaknetPriority(priority), ToRaknetPacketReliability(reliability), channel);
    }

    public virtual uint SendTo(ulong guid, Priority priority, Reliability reliability, sbyte channel)
    {
        Check();
        return RakNet_Native.NETSND_Send(ptr, guid, ToRaknetPriority(priority), ToRaknetPacketReliability(reliability), channel);
    }

    public unsafe void SendUnconnectedMessage(byte* data, int length, uint adr, ushort port)
    {
        Check();
        RakNet_Native.NET_SendMessage(ptr, data, length, adr, port);
    }

    public string GetAddress(ulong guid)
    {
        Check();
        return StringFromPointer(RakNet_Native.NET_GetAddress(ptr, guid));
    }

    public void SetBandwidth(Peer peer, int maxBytes)
    {
        RakNet_Native.NET_LimitBandwidth(peer.ptr, (uint)maxBytes / 8);
    }

    private static string StringFromPointer(IntPtr p)
    {
        if (p == IntPtr.Zero)
        {
            return string.Empty;
        }
        return Marshal.PtrToStringAnsi(p);
    }

    public int ToRaknetPriority(Priority priority)
    {
        switch (priority)
        {
            case Priority.Immediate:
                return 0;
            case Priority.High:
                return 1;
            case Priority.Medium:
                return 2;
            default:
                return 3;
        }
    }

    public int ToRaknetPacketReliability(Reliability priority)
    {
        switch (priority)
        {
            case Reliability.ReliableUnordered:
                return 2;
            case Reliability.Reliable:
                return 3;
            case Reliability.ReliableSequenced:
                return 4;
            case Reliability.Unreliable:
                return 0;
            case Reliability.UnreliableSequenced:
                return 1;
            default:
                return 3;
        }
    }

    public void Kick(Connection connection)
    {
        Check();
        RakNet_Native.NET_CloseConnection(ptr, connection.guid);
    }

    public void Kick(ulong guid)
    {
        Check();
        RakNet_Native.NET_CloseConnection(ptr, guid);
    }

    public void Kick(Connection connection, float delay)
    {
        Task.Delay((int)(delay / 1000)).ContinueWith(t => Kick(connection.guid));
    }

    public void Kick(ulong guid, float delay)
    {
        Task.Delay((int)(delay / 1000)).ContinueWith(t => Kick(guid));
    }


    protected virtual void Check()
    {
        if (ptr == IntPtr.Zero)
        {
            throw new NullReferenceException("[RakNet] Peer is not active!");
        }
    }

    public virtual string GetStatisticsString(ulong guid)
    {
        Check();
        return $"Average Ping:\t\t{GetPingAverage(guid)}\nLast Ping:\t\t{GetPingLast(guid)}\nLowest Ping:\t\t{GetPingLowest(guid)}\n{StringFromPointer(RakNet_Native.NET_GetStatisticsString(ptr, guid))}";
    }

    public virtual string GetWriterReaderStats()
    {
        Check();
        return $"Total received: "+TOTAL_RECEIVED_BYTES+" bytes\nTotal sended: "+TOTAL_SENDED_BYTES+" bytes";
    }

    public virtual int GetPingAverage(ulong guid)
    {
        Check();
        return RakNet_Native.NET_GetAveragePing(ptr, guid);
    }

    public virtual int GetPingLast(ulong guid)
    {
        Check();
        return RakNet_Native.NET_GetLastPing(ptr, guid);
    }

    public virtual int GetPingLowest(ulong guid)
    {
        Check();
        return RakNet_Native.NET_GetLowestPing(ptr, guid);
    }

    public unsafe virtual RakNet_Native.RaknetStats GetStatistics(ulong guid)
    {
        Check();
        RakNet_Native.RaknetStats data = default;
        int num = sizeof(RakNet_Native.RaknetStats);
        if (!RakNet_Native.NET_GetStatistics(ptr, guid, ref data, num))
        {
            Debug.Log("[RakNet] NET_GetStatistics:  Wrong size " + num);
        }
        return data;
    }

    public unsafe virtual ulong GetStat(Connection connection, StatTypeLong type)
    {
        Check();
        RakNet_Native.RaknetStats raknetStats = (connection != null) ? GetStatistics(connection.guid) : GetStatistics(0uL);
        switch (type)
        {
            case StatTypeLong.BytesReceived:
                return (ulong)raknetStats.runningTotal;
            case StatTypeLong.BytesReceived_LastSecond:
                return (ulong)raknetStats.valueOverLastSecond;
            case StatTypeLong.BytesSent:
                return (ulong)raknetStats.runningTotal;
            case StatTypeLong.BytesSent_LastSecond:
                return (ulong)raknetStats.valueOverLastSecond;
            case StatTypeLong.BytesInSendBuffer:
                return (ulong)raknetStats.bytesInSendBuffer;
            case StatTypeLong.BytesInResendBuffer:
                return raknetStats.bytesInResendBuffer;
            case StatTypeLong.PacketLossAverage:
                return (ulong)raknetStats.packetlossTotal * 10000;
            case StatTypeLong.PacketLossLastSecond:
                return (ulong)raknetStats.packetlossLastSecond * 10000;
            case StatTypeLong.ThrottleBytes:
                if (raknetStats.isLimitedByCongestionControl == 0)
                {
                    return 0uL;
                }
                return raknetStats.BPSLimitByCongestionControl;
            default:
                return 0uL;
        }
    }

    public unsafe virtual ulong GetStat(ulong guid, StatTypeLong type)
    {
        Check();
        RakNet_Native.RaknetStats raknetStats = GetStatistics(guid);
        switch (type)
        {
            case StatTypeLong.BytesReceived:
                return (ulong)raknetStats.runningTotal;
            case StatTypeLong.BytesReceived_LastSecond:
                return (ulong)raknetStats.valueOverLastSecond;
            case StatTypeLong.BytesSent:
                return (ulong)raknetStats.runningTotal;
            case StatTypeLong.BytesSent_LastSecond:
                return (ulong)raknetStats.valueOverLastSecond;
            case StatTypeLong.BytesInSendBuffer:
                return (ulong)raknetStats.bytesInSendBuffer;
            case StatTypeLong.BytesInResendBuffer:
                return raknetStats.bytesInResendBuffer;
            case StatTypeLong.PacketLossAverage:
                return (ulong)raknetStats.packetlossTotal * 10000;
            case StatTypeLong.PacketLossLastSecond:
                return (ulong)raknetStats.packetlossLastSecond * 10000;
            case StatTypeLong.ThrottleBytes:
                if (raknetStats.isLimitedByCongestionControl == 0)
                {
                    return 0uL;
                }
                return raknetStats.BPSLimitByCongestionControl;
            default:
                return 0uL;
        }
    }


    public enum StatTypeLong
    {
        BytesSent,
        BytesSent_LastSecond,
        BytesReceived,
        BytesReceived_LastSecond,
        MessagesInSendBuffer,
        BytesInSendBuffer,
        MessagesInResendBuffer,
        BytesInResendBuffer,
        PacketLossAverage,
        PacketLossLastSecond,
        ThrottleBytes
    }

    /// <summary>
    /// канал пакета
    /// </summary>
    public enum Reliability
    {
        Reliable,
        ReliableUnordered,
        ReliableSequenced,//гарантированная доставка по порядку
        Unreliable,//нет гарантии что пакет придёт
        UnreliableSequenced//нет гарантии что пакет придёт, но отпарвляется по очереди
    }

    /// <summary>
    /// приоритет пакета
    /// </summary>
    public enum Priority
    {
        Immediate,
        High,
        Medium,
        Low
    }

    /// <summary>
    /// номера пакетов (номера пакетов с 0 до 134 являются нативными! Их нельзя вызвать из кода)
    /// </summary>
    public enum RakNet_Packets_ID : byte
    {
        //start native packet ids
        CONNECTED_PING,
        UNCONNECTED_PING,
        UNCONNECTED_PING_OPEN_CONNECTIONS,
        CONNECTED_PONG,
        DETECT_LOST_CONNECTIONS,
        OPEN_CONNECTION_REQUEST_1,
        OPEN_CONNECTION_REPLY_1,
        OPEN_CONNECTION_REQUEST_2,
        OPEN_CONNECTION_REPLY_2,
        CONNECTION_REQUEST,
        REMOTE_SYSTEM_REQUIRES_PUBLIC_KEY,
        OUR_SYSTEM_REQUIRES_SECURITY,
        PUBLIC_KEY_MISMATCH,
        OUT_OF_BAND_INTERNAL,
        SND_RECEIPT_ACKED,
        SND_RECEIPT_LOSS,
        CONNECTION_REQUEST_ACCEPTED,
        CONNECTION_ATTEMPT_FAILED,
        ALREADY_CONNECTED,
        NEW_INCOMING_CONNECTION,
        NO_FREE_INCOMING_CONNECTIONS,
        DISCONNECTION_NOTIFICATION,
        CONNECTION_LOST,
        CONNECTION_BANNED,
        INVALID_PASSWORD,
        INCOMPATIBLE_PROTOCOL_VERSION,
        IP_RECENTLY_CONNECTED,
        TIMESTAMP,
        UNCONNECTED_PONG,
        ADVERTISE_SYSTEM,
        DOWNLOAD_PROGRESS,
        REMOTE_DISCONNECTION_NOTIFICATION,
        REMOTE_CONNECTION_LOST,
        REMOTE_NEW_INCOMING_CONNECTION,
        FILE_LIST_TRANSFER_HEADER,
        FILE_LIST_TRANSFER_FILE,
        FILE_LIST_REFERENCE_PUSH_ACK,
        DDT_DOWNLOAD_REQUEST,
        TRANSPORT_STRING,
        REPLICA_MANAGER_CONSTRUCTION,
        REPLICA_MANAGER_SCOPE_CHANGE,
        REPLICA_MANAGER_SERIALIZE,
        REPLICA_MANAGER_DOWNLOAD_STARTED,
        REPLICA_MANAGER_DOWNLOAD_COMPLETE,
        RAKVOICE_OPEN_CHANNEL_REQUEST,
        RAKVOICE_OPEN_CHANNEL_REPLY,
        RAKVOICE_CLOSE_CHANNEL,
        RAKVOICE_DATA,
        AUTOPATCHER_GET_CHANGELIST_SINCE_DATE,
        AUTOPATCHER_CREATION_LIST,
        AUTOPATCHER_DELETION_LIST,
        AUTOPATCHER_GET_PATCH,
        AUTOPATCHER_PATCH_LIST,
        AUTOPATCHER_REPOSITORY_FATAL_ERROR,
        AUTOPATCHER_CANNOT_DOWNLOAD_ORIGINAL_UNMODIFIED_FILES,
        AUTOPATCHER_FINISHED_INTERNAL,
        AUTOPATCHER_FINISHED,
        AUTOPATCHER_RESTART_APPLICATION,
        NAT_PUNCHTHROUGH_REQUEST,
        NAT_CONNECT_AT_TIME,
        NAT_GET_MOST_RECENT_PORT,
        NAT_CLIENT_READY,
        NAT_TARGET_NOT_CONNECTED,
        NAT_TARGET_UNRESPONSIVE,
        NAT_CONNECTION_TO_TARGET_LOST,
        NAT_ALREADY_IN_PROGRESS,
        NAT_PUNCHTHROUGH_FAILED,
        NAT_PUNCHTHROUGH_SUCCEEDED,
        READY_EVENT_SET,
        READY_EVENT_UNSET,
        READY_EVENT_ALL_SET,
        READY_EVENT_QUERY,
        LOBBY_GENERAL,
        RPC_REMOTE_ERROR,
        RPC_PLUGIN,
        FILE_LIST_REFERENCE_PUSH,
        READY_EVENT_FORCE_ALL_SET,
        ROOMS_EXECUTE_FUNC,
        ROOMS_LOGON_STATUS,
        ROOMS_HANDLE_CHANGE,
        LOBBY2_SEND_MESSAGE,
        LOBBY2_SERVER_ERROR,
        FCM2_NEW_HOST,
        FCM2_REQUEST_FCMGUID,
        FCM2_RESPOND_CONNECTION_COUNT,
        FCM2_INFORM_FCMGUID,
        FCM2_UPDATE_MIN_TOTAL_CONNECTION_COUNT,
        FCM2_VERIFIED_JOIN_START,
        FCM2_VERIFIED_JOIN_CAPABLE,
        FCM2_VERIFIED_JOIN_FAILED,
        FCM2_VERIFIED_JOIN_ACCEPTED,
        FCM2_VERIFIED_JOIN_REJECTED,
        UDP_PROXY_GENERAL,
        SQLite3_EXEC,
        SQLite3_UNKNOWN_DB,
        SQLLITE_LOGGER,
        NAT_TYPE_DETECTION_REQUEST,
        NAT_TYPE_DETECTION_RESULT,
        ROUTER_2_INTERNAL,
        ROUTER_2_FORWARDING_NO_PATH,
        ROUTER_2_FORWARDING_ESTABLISHED,
        ROUTER_2_REROUTED,
        TEAM_BALANCER_INTERNAL,
        TEAM_BALANCER_REQUESTED_TEAM_FULL,
        TEAM_BALANCER_REQUESTED_TEAM_LOCKED,
        TEAM_BALANCER_TEAM_REQUESTED_CANCELLED,
        TEAM_BALANCER_TEAM_ASSIGNED,
        LIGHTSPEED_INTEGRATION,
        XBOX_LOBBY,
        TWO_WAY_AUTHENTICATION_INCOMING_CHALLENGE_SUCCESS,
        TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_SUCCESS,
        TWO_WAY_AUTHENTICATION_INCOMING_CHALLENGE_FAILURE,
        TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_FAILURE,
        TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_TIMEOUT,
        TWO_WAY_AUTHENTICATION_NEGOTIATION,
        CLOUD_POST_REQUEST,
        CLOUD_RELEASE_REQUEST,
        CLOUD_GET_REQUEST,
        CLOUD_GET_RESPONSE,
        CLOUD_UNSUBSCRIBE_REQUEST,
        CLOUD_SERVER_TO_SERVER_COMMAND,
        CLOUD_SUBSCRIPTION_NOTIFICATION,
        LIB_VOICE,
        RELAY_PLUGIN,
        NAT_REQUEST_BOUND_ADDRESSES,
        NAT_RESPOND_BOUND_ADDRESSES,
        FCM2_UPDATE_USER_CONTEXT,
        RESERVED_3,
        RESERVED_4,
        RESERVED_5,
        RESERVED_6,
        RESERVED_7,
        RESERVED_8,
        RESERVED_9,
        USER_PACKET_ENUM
    }


    public void SendPacket(Connection conn, Packets_ID id, NetworkWriter write)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            write.Send(conn.guid, Priority.Immediate, Reliability.Reliable, 0);
        }
    }

    public void SendPacket(Connection conn, Packets_ID id, Reliability reliability, NetworkWriter write)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            write.Send(conn.guid, Priority.Immediate, reliability, 0);
        }
    }

    public void SendPacket(Connection conn, Packets_ID id, NetworkWriter write, object value)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            if (value.GetType() == typeof(ulong)) { write.Write((ulong)value); }
            if (value.GetType() == typeof(int)) { write.Write((int)value); }
            if (value.GetType() == typeof(float)) { write.Write((float)value); }
            if (value.GetType() == typeof(double)) { write.Write((double)value); }
            if (value.GetType() == typeof(string)) { write.Write((string)value); }
            if (value.GetType() == typeof(byte)) { write.Write((byte)value); }
            if (value.GetType() == typeof(short)) { write.Write((short)value); }
            if (value.GetType() == typeof(char)) { write.Write((char)value); }
            if (value.GetType() == typeof(uint)) { write.Write((uint)value); }
            if (value.GetType() == typeof(long)) { write.Write((long)value); }
            if (value.GetType() == typeof(sbyte)) { write.Write((sbyte)value); }
            if (value.GetType() == typeof(decimal)) { write.Write((decimal)value); }
            if (value.GetType() == typeof(Vector4)) { write.Write((Vector4)value); }
            if (value.GetType() == typeof(Vector3)) { write.Write((Vector3)value); }
            if (value.GetType() == typeof(Vector2)) { write.Write((Vector2)value); }
            if (value.GetType() == typeof(Matrix4x4)) { write.Write((Matrix4x4)value); }
            if (value.GetType() == typeof(Quaternion)) { write.Write((Quaternion)value); }
            if (value.GetType() == typeof(Color)) { write.Write((Color)value); }
            if (value.GetType() == typeof(Rect)) { write.Write((Rect)value); }
            if (value.GetType() == typeof(Ray)) { write.Write((Ray)value); }
            write.Send(conn.guid, Priority.Immediate, Reliability.Reliable, 0);
        }
    }

    public void SendPacket(ulong guid, Packets_ID id, NetworkWriter write)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            write.Send(guid, Priority.Immediate, Reliability.Reliable, 0);
        }
    }

    public void SendPacket(ulong guid, Packets_ID id, Reliability reliability, NetworkWriter write)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            write.Send(guid, Priority.Immediate, reliability, 0);
        }
    }

    public void SendPacket(ulong guid, Packets_ID id, NetworkWriter write, object value)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            if (value.GetType() == typeof(ulong)) { write.Write((ulong)value); }
            if (value.GetType() == typeof(int)) { write.Write((int)value); }
            if (value.GetType() == typeof(float)) { write.Write((float)value); }
            if (value.GetType() == typeof(double)) { write.Write((double)value); }
            if (value.GetType() == typeof(string)) { write.Write((string)value); }
            if (value.GetType() == typeof(byte)) { write.Write((byte)value); }
            if (value.GetType() == typeof(short)) { write.Write((short)value); }
            if (value.GetType() == typeof(char)) { write.Write((char)value); }
            if (value.GetType() == typeof(uint)) { write.Write((uint)value); }
            if (value.GetType() == typeof(long)) { write.Write((long)value); }
            if (value.GetType() == typeof(sbyte)) { write.Write((sbyte)value); }
            if (value.GetType() == typeof(decimal)) { write.Write((decimal)value); }
            if (value.GetType() == typeof(Vector4)) { write.Write((Vector4)value); }
            if (value.GetType() == typeof(Vector3)) { write.Write((Vector3)value); }
            if (value.GetType() == typeof(Vector2)) { write.Write((Vector2)value); }
            if (value.GetType() == typeof(Matrix4x4)) { write.Write((Matrix4x4)value); }
            if (value.GetType() == typeof(Quaternion)) { write.Write((Quaternion)value); }
            if (value.GetType() == typeof(Color)) { write.Write((Color)value); }
            if (value.GetType() == typeof(Rect)) { write.Write((Rect)value); }
            if (value.GetType() == typeof(Ray)) { write.Write((Ray)value); }
            write.Send(guid, Priority.Immediate, Reliability.Reliable, 0);
        }
    }

    public void SendPacket(ulong guid, Packets_ID id, Reliability realibility, NetworkWriter write, object value)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            if (value.GetType() == typeof(ulong)) { write.Write((ulong)value); }
            if (value.GetType() == typeof(int)) { write.Write((int)value); }
            if (value.GetType() == typeof(float)) { write.Write((float)value); }
            if (value.GetType() == typeof(double)) { write.Write((double)value); }
            if (value.GetType() == typeof(string)) { write.Write((string)value); }
            if (value.GetType() == typeof(byte)) { write.Write((byte)value); }
            if (value.GetType() == typeof(short)) { write.Write((short)value); }
            if (value.GetType() == typeof(char)) { write.Write((char)value); }
            if (value.GetType() == typeof(uint)) { write.Write((uint)value); }
            if (value.GetType() == typeof(long)) { write.Write((long)value); }
            if (value.GetType() == typeof(sbyte)) { write.Write((sbyte)value); }
            if (value.GetType() == typeof(decimal)) { write.Write((decimal)value); }
            if (value.GetType() == typeof(Vector4)) { write.Write((Vector4)value); }
            if (value.GetType() == typeof(Vector3)) { write.Write((Vector3)value); }
            if (value.GetType() == typeof(Vector2)) { write.Write((Vector2)value); }
            if (value.GetType() == typeof(Matrix4x4)) { write.Write((Matrix4x4)value); }
            if (value.GetType() == typeof(Quaternion)) { write.Write((Quaternion)value); }
            if (value.GetType() == typeof(Color)) { write.Write((Color)value); }
            if (value.GetType() == typeof(Rect)) { write.Write((Rect)value); }
            if (value.GetType() == typeof(Ray)) { write.Write((Ray)value); }
            write.Send(guid, Priority.Immediate, realibility, 0);
        }
    }
    public void SendData(ulong guid, Reliability reliability, sbyte channel, NetworkWriter write)
    {
        write.Send(guid, Priority.Immediate, reliability, channel);
    }
    public void SendPacket(ulong guid, Packets_ID id, Reliability realibility,sbyte channel, NetworkWriter write, object value)
    {
        if (write.StartWritting())
        {
            write.Write((byte)id);
            if (value.GetType() == typeof(ulong)) { write.Write((ulong)value); }
            if (value.GetType() == typeof(int)) { write.Write((int)value); }
            if (value.GetType() == typeof(float)) { write.Write((float)value); }
            if (value.GetType() == typeof(double)) { write.Write((double)value); }
            if (value.GetType() == typeof(string)) { write.Write((string)value); }
            if (value.GetType() == typeof(byte)) { write.Write((byte)value); }
            if (value.GetType() == typeof(short)) { write.Write((short)value); }
            if (value.GetType() == typeof(char)) { write.Write((char)value); }
            if (value.GetType() == typeof(uint)) { write.Write((uint)value); }
            if (value.GetType() == typeof(long)) { write.Write((long)value); }
            if (value.GetType() == typeof(sbyte)) { write.Write((sbyte)value); }
            if (value.GetType() == typeof(decimal)) { write.Write((decimal)value); }
            if (value.GetType() == typeof(Vector4)) { write.Write((Vector4)value); }
            if (value.GetType() == typeof(Vector3)) { write.Write((Vector3)value); }
            if (value.GetType() == typeof(Vector2)) { write.Write((Vector2)value); }
            if (value.GetType() == typeof(Matrix4x4)) { write.Write((Matrix4x4)value); }
            if (value.GetType() == typeof(Quaternion)) { write.Write((Quaternion)value); }
            if (value.GetType() == typeof(Color)) { write.Write((Color)value); }
            if (value.GetType() == typeof(Rect)) { write.Write((Rect)value); }
            if (value.GetType() == typeof(Ray)) { write.Write((Ray)value); }
            write.Send(guid, Priority.Immediate, realibility, channel);
        }
    }
}
