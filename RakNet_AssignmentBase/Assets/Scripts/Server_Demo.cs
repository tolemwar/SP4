using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Peer;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class playerObject
{
    public uint id;
    public float m_x;
    public float m_y;
    public float m_z;
    public float velocity_X;
    public float velocity_Y;
    public float velocity_Z;
    public int playerNum;
    public string name;
    public float rotation_x;
    public float rotation_y;
    public float rotation_z;

    public playerObject(uint _id)
    {
        this.playerNum = 1;
        m_x = 0.0f;
        m_y = 0.0f;
        m_z = 0.0f;
        id = _id;
        name = "";
        velocity_X = velocity_Y = velocity_Z = 0.0f;
        rotation_x = rotation_y = rotation_z = 0.0f;
    }
}

public class Server_Demo : MonoBehaviour
{
    public Peer peer { get; private set; }
    private NetworkReader m_NetworkReader;
    private NetworkWriter m_NetworkWriter;
    public static Server_Demo Instance;
    public Text Info;

    private Dictionary<ulong, playerObject> clients = new Dictionary<ulong, playerObject>();

    private uint playerID;
    private uint enemyID;

    const int MAX_PLAYERS = 2;
    private void Awake()
    {
        Instance = this;
    }

    public void Init(int _port)
    {
        StartServer("127.0.0.1", _port,MAX_PLAYERS);
    }

    public void StopServer()
    {
        if (peer != null)
        {
            peer.Close();
            Debug.LogError("[Server] Shutting down...");
        }
    }

    private void OnDestroy()
    {
        StopServer();
    }

    public int MaxConnections { get; private set; } = -1;

    public bool StartServer(string ip, int port, int maxConnections)
    {
        if (peer == null)
        {
            peer = new Peer();
            peer = CreateServer(ip, port, maxConnections);

            if (peer != null)
            {
                MaxConnections = maxConnections;
                Debug.Log("[Server] Server initialized on port " + port);

                Debug.Log("-------------------------------------------------");
                Debug.Log("|     Max connections: " + maxConnections);
                Debug.Log("|     Max FPS: " + (Application.targetFrameRate != -1 ? Application.targetFrameRate : 1000) + "(" + Time.deltaTime.ToString("f3") + " ms)");
                Debug.Log("|     Tickrate: " + (1 / Time.fixedDeltaTime) + "(" + Time.fixedDeltaTime.ToString("f3") + " ms)");
                Debug.Log("-------------------------------------------------");

                m_NetworkReader = new NetworkReader(peer);
                m_NetworkWriter = new NetworkWriter(peer);

                return true;
            }
            else
            {
                Debug.LogError("[Server] Starting failed...");

                return false;
            }
        }
        else
        {
            return true;
        }
    }

    private void FixedUpdate()
    {
        if (peer != null)
        {
            while (peer.Receive())
            {
                m_NetworkReader.StartReading();
                byte b = m_NetworkReader.ReadByte();

                OnReceivedPacket(b);
            }
        }

        //if(spawnPickUpTimer < 0.0f)
        //{
        //    spawnPickUpTimer = spawnPickUpCoolDown;
        //    SpawnPickUp();
        //}
        //else
        //{
        //    spawnPickUpTimer -= Time.deltaTime;
        //}

        if (Input.GetKeyDown("space"))
        {
            ChangeScene("Scene2");
        }
    }


    private void OnReceivedPacket(byte packet_id)
    {
        bool IsInternalNetworkPackets = packet_id <= 134;

        if (IsInternalNetworkPackets)
        {
            if (packet_id == (byte)RakNet_Packets_ID.NEW_INCOMING_CONNECTION)
            {
                OnConnected();//добавляем соединение
            }

            if (packet_id == (byte)RakNet_Packets_ID.CONNECTION_LOST || packet_id == (byte)RakNet_Packets_ID.DISCONNECTION_NOTIFICATION)
            {
                Connection conn = FindConnection(peer.incomingGUID);

                if (conn != null)
                {
                    OnDisconnected(FindConnection(peer.incomingGUID));
                }
            }
        }
        else
        {
            switch(packet_id)
            {
                case (byte)Packets_ID.CL_INFO:
                    OnReceivedClientNetInfo(peer.incomingGUID);
                    break;
                case (byte)Packets_ID.ID_INITIALSTATS:
                    OnReceivedClientInitialStats(peer.incomingGUID);
                    break;
                case (byte)Packets_ID.ID_MOVEMENT:
                    OnReceivedClientMovementData(peer.incomingGUID);
                    break;
            }
           

        }
    }



    #region Connections
    public List<Connection> connections = new List<Connection>();
    private Dictionary<ulong, Connection> connectionByGUID = new Dictionary<ulong, Connection>();

    public List<ulong> guids = new List<ulong>();

    public Connection FindConnection(ulong guid)
    {
        if (connectionByGUID.TryGetValue(guid, out Connection value))
        {
            return value;
        }
        return null;
    }

    private void AddConnection(Connection connection)
    {
        connections.Add(connection);
        connectionByGUID.Add(connection.guid, connection);
        guids.Add(connection.guid);
    }

    private void RemoveConnection(Connection connection)
    {
        clients.Remove(connection.guid);
        connectionByGUID.Remove(connection.guid);
        connections.Remove(connection);
        guids.Remove(connection.guid);
    }

    public static Connection[] Connections
    {
        get
        {
            return Instance.connections.ToArray();
        }
    }

    public static Connection GetByID(int id)
    {
        if (Connections.Length > 0)
        {
            return Connections[id];
        }

        return null;
    }

    public static Connection GetByIP(string ip)
    {
        foreach (Connection c in Connections)
        {
            if (c.ipaddress == ip)
            {
                return c;
            }
        }

        return null;
    }

    public static Connection GetByName(string name)
    {
        foreach (Connection c in Connections)
        {
            if (c.Info.name == name)
            {
                return c;
            }
        }

        return null;
    }

    public static Connection GetByHWID(string hwid)
    {
        foreach (Connection c in Connections)
        {
            if (c.Info.client_hwid == hwid)
            {
                return c;
            }
        }

        return null;
    }

    #endregion

    #region Events
    private void OnConnected()
    {
        Connection connection = new Connection(peer, peer.incomingGUID, connections.Count);

        //добавляем в список соединений
        AddConnection(connection);

        Debug.Log("[Server] Connection established " + connection.ipaddress);
        //peer.SendData(guid, Peer.Reliability.Reliable, 0, m_NetworkWriter);
      
        peer.SendPacket(connection, Packets_ID.CL_INFO, m_NetworkWriter);
    }

    private void OnDisconnected(Connection connection)
    {
        if (connection != null)
        {
            try
            {
                Debug.LogError("[Server] " + connection.Info.name + " disconnected [IP: " + connection.ipaddress + "]");

                RemoveConnection(connection);
            }
            catch
            {
                Debug.LogError("[Server] Unassgigned connection destroyed!");
            }
        }
    }
    

    private void OnReceivedClientMovementData(ulong guid)
    {
        playerObject tempObj = clients[guid];
        tempObj.m_x = m_NetworkReader.ReadFloat();
        tempObj.m_y = m_NetworkReader.ReadFloat();
        tempObj.m_z = m_NetworkReader.ReadFloat();
        tempObj.rotation_x = m_NetworkReader.ReadFloat();
        tempObj.rotation_y = m_NetworkReader.ReadFloat();
        tempObj.rotation_z = m_NetworkReader.ReadFloat();
        tempObj.velocity_X = m_NetworkReader.ReadFloat();
        tempObj.velocity_Y = m_NetworkReader.ReadFloat();
        tempObj.velocity_Z = m_NetworkReader.ReadFloat();

        clients[guid] = tempObj;

        if (m_NetworkWriter.StartWritting())
        {
            m_NetworkWriter.WritePacketID((byte)Packets_ID.ID_MOVEMENT);
            m_NetworkWriter.Write(tempObj.id);
            m_NetworkWriter.Write(tempObj.m_x);
            m_NetworkWriter.Write(tempObj.m_y);
            m_NetworkWriter.Write(tempObj.m_z);
            m_NetworkWriter.Write(tempObj.rotation_x);
            m_NetworkWriter.Write(tempObj.rotation_y);
            m_NetworkWriter.Write(tempObj.rotation_z);
            m_NetworkWriter.Write(tempObj.velocity_X);
            m_NetworkWriter.Write(tempObj.velocity_Y);
            m_NetworkWriter.Write(tempObj.velocity_Z);

            SendToAll(guid, m_NetworkWriter, true);
        }
    }

    private void OnReceivedClientInitialStats(ulong guid)
    {
        playerObject tempObj = clients[guid];
        tempObj.name = m_NetworkReader.ReadString();
        tempObj.m_x = m_NetworkReader.ReadFloat();
        tempObj.m_y = m_NetworkReader.ReadFloat();
        tempObj.m_z = m_NetworkReader.ReadFloat();
        tempObj.rotation_x = m_NetworkReader.ReadFloat();
        tempObj.rotation_y = m_NetworkReader.ReadFloat();
        tempObj.rotation_z = m_NetworkReader.ReadFloat();

        clients[guid] = tempObj;

        if (m_NetworkWriter.StartWritting())
        {
            m_NetworkWriter.WritePacketID((byte)Packets_ID.ID_NEWPLAYER);
            m_NetworkWriter.Write(tempObj.id);
            m_NetworkWriter.Write(tempObj.name);
            m_NetworkWriter.Write(tempObj.m_x);
            m_NetworkWriter.Write(tempObj.m_y);
            m_NetworkWriter.Write(tempObj.m_z);
            m_NetworkWriter.Write(tempObj.rotation_x);
            m_NetworkWriter.Write(tempObj.rotation_y);
            m_NetworkWriter.Write(tempObj.rotation_z);
            m_NetworkWriter.Write(tempObj.playerNum);

            SendToAll(guid, m_NetworkWriter, true);
            // peer.SendBroadcast(Peer.Priority.Immediate, Peer.Reliability.Reliable, 0);

        }
    }
    private void SendToAll(ulong guid, NetworkWriter _writer, bool broadcast)
    {
        foreach(ulong guids in clients.Keys)
        {
            if(broadcast)
            {
                if (guids == guid)
                    continue;
            }

            peer.SendData(guids, Peer.Reliability.Reliable, 0, _writer);
        }
    }

    void ChangeScene(string sceneName)
    {
        if (m_NetworkWriter.StartWritting())
        {

            Debug.Log("Changing Scenes");
            m_NetworkWriter.WritePacketID((byte)Packets_ID.ID_CHANGESCENE);
            m_NetworkWriter.Write(sceneName);

            foreach (ulong guids in clients.Keys)
            {
                peer.SendData(guids, Peer.Reliability.Reliable, 0, m_NetworkWriter);
            }
        }
    }

    private void OnReceivedClientNetInfo(ulong guid)
    {
        Debug.Log("server received data");
        Connection connection = FindConnection(guid);

        if (clients.Count == MAX_PLAYERS)
            return;

        if (connection != null)
        {
            if (connection.Info == null)
            {
                connection.Info = new ClientNetInfo();
                connection.Info.net_id = guid;
                connection.Info.name = m_NetworkReader.ReadString();
                connection.Info.local_id = m_NetworkReader.ReadPackedUInt64();
                connection.Info.client_hwid = m_NetworkReader.ReadString();
                connection.Info.client_version = m_NetworkReader.ReadString();
                ++playerID;

                Debug.Log("Sent");

                if (m_NetworkWriter.StartWritting())
                {
                    m_NetworkWriter.WritePacketID((byte)Packets_ID.ID_WELCOME);
                    m_NetworkWriter.Write(playerID);
                    m_NetworkWriter.Write(clients.Count);

                    foreach (playerObject playerObj in clients.Values)
                    {
                        m_NetworkWriter.Write(playerObj.id);
                        m_NetworkWriter.Write(playerObj.m_x);
                        m_NetworkWriter.Write(playerObj.m_y);
                        m_NetworkWriter.Write(playerObj.m_z);
                        m_NetworkWriter.Write(playerObj.rotation_x);
                        m_NetworkWriter.Write(playerObj.rotation_y);
                        m_NetworkWriter.Write(playerObj.rotation_z);
                        m_NetworkWriter.Write(playerObj.playerNum);
                        m_NetworkWriter.Write(playerObj.name);
                        //m_NetworkWriter.Write
                    }
                    peer.SendData(guid, Peer.Reliability.Reliable, 0, m_NetworkWriter);
                    //  m_NetworkWriter.Send//sending
                    //m_NetworkWriter.Reset();

                    playerObject newObj = new playerObject(playerID);
                    newObj.playerNum = m_NetworkReader.ReadInt32();
                    clients.Add(guid, newObj);

                    Debug.Log("Added new guy : " + newObj.id);
                }
                //peer.SendPacket(connection, Packets_ID.NET_LOGIN, Reliability.Reliable, m_NetworkWriter);
            }
            else
            {
                peer.SendPacket(connection, Packets_ID.CL_FAKE, Reliability.Reliable, m_NetworkWriter);
                peer.Kick(connection, 1);
            }
        }
    }
    #endregion

    /*
    public InputField Guid;

    public void OnBanClicked()
    {
        Connection connection = FindConnection(ulong.Parse(Guid.text));

        if (connection != null)
        {
            peer.SendPacket(connection, Packets_ID.CL_BANNED, Reliability.Reliable, m_NetworkWriter);
            peer.Kick(connection, 1);
        }
    }
    */
}