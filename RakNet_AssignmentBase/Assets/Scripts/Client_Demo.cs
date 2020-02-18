using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
public class Client_Demo : MonoBehaviour
{
    public ClientNetInfo m_ClientNetInfo = new ClientNetInfo();
    public enum ClientState { DISCONNECTED, CONNECTED }
    public ClientState m_State = ClientState.DISCONNECTED;
    public Text Info;
    public Peer peer { get; private set; }

    [SerializeField] private GameObject playerReference;
    [SerializeField] private GameObject enemyReference;

    private List<PlayerManager> playersList = new List<PlayerManager>();
    //private List<ShipManager> shipList = new List<ShipManager>();
    //private List<MissileManager> missileList = new List<MissileManager>();
    //private List<PickUpsManager> pickUpList = new List<PickUpsManager>();
    private NetworkReader m_NetworkReader;
    private NetworkWriter m_NetworkWriter;
    public static Client_Demo Instance;
    private bool sendMsg = false;
    private ulong serveruid = 0;
    private float delta = 0.0f;
    private string userName;

    private void Awake()
    {
        Instance = this;
    }

    public void Init(string _ip, int _port, string _name)
    {
        userName = _name;
        Connect(_ip, _port);

    }

    public bool IsRunning
    {
        get
        {
            return peer != null;
        }
    }


    protected void Update()
    {
        delta += Time.deltaTime;
        if (Input.GetKeyDown("space"))
        {

        }

        if (delta > 0.5f)
        {
            SendMovement();
            delta = 0;
        }

    }

    private void SendMovement()
    {
        if (m_NetworkWriter.StartWritting())
        {
            PlayerManager me = playersList[0];
            if (me == null)
                return;
            m_NetworkWriter.WritePacketID((byte)Packets_ID.ID_MOVEMENT);

            // step 9 : Instead of sending x,y,w ..... , send the server version instead (x,y,w,velocity, angular velocity)


            m_NetworkWriter.Write(me.position.x);
            m_NetworkWriter.Write(me.position.y);
            m_NetworkWriter.Write(me.position.z);
            m_NetworkWriter.Write(me.pRotation.x);
            m_NetworkWriter.Write(me.pRotation.y);
            m_NetworkWriter.Write(me.pRotation.z);
            m_NetworkWriter.Write(me.velocity.x);
            m_NetworkWriter.Write(me.velocity.y);
            m_NetworkWriter.Write(me.velocity.z);
            m_NetworkWriter.Send(serveruid, Peer.Priority.Immediate, Peer.Reliability.Reliable, 0);
        }
    }
    #region Connect/Disconnect
    public void Connect(string ip, int port, int retries, int retry_delay, int timeout)
    {
    CREATE_PEER:
        tmp_Banned = tmp_Fake = false;
        if (peer == null)
        {
            peer = Peer.CreateConnection(ip, port, retries, retry_delay, timeout);

            if (peer != null)
            {
                Debug.Log("[Client] Preparing to receiving...");
                m_NetworkReader = new NetworkReader(peer);
                m_NetworkWriter = new NetworkWriter(peer);
            }
        }
        else
        {
            peer.Close();
            peer = null;

            goto CREATE_PEER;
        }
    }

    public void Connect(string ip, int port)
    {
        Connect(ip, port, 30, 500, 30);
    }
    

    public void Disconnect()
    {
        if (m_State == ClientState.CONNECTED)
        {
            OnDisconnected("");
            peer.Close();
            peer = null;
        }
    }
    #endregion

    private unsafe void FixedUpdate()
    {
        m_State = peer != null ? ClientState.CONNECTED : ClientState.DISCONNECTED;

        if (peer != null)
        {
            while (peer.Receive())
            {
                m_NetworkReader.StartReading();
                byte b = m_NetworkReader.ReadByte();

                OnReceivedPacket(b);
            }
        }
    }


    private bool tmp_Banned = false, tmp_Fake = false;


    /// <summary>
    /// Parsing packet
    /// </summary>
    /// <param name="packet_id">PACKET ID  - SEE Packets_ID.cs</param>

    private void OnReceivedPacket(byte packet_id)
    {
        bool IsInternalNetworkPackets = packet_id <= 134;

        if (IsInternalNetworkPackets)
        {
            if (packet_id == (byte)Peer.RakNet_Packets_ID.CONNECTION_REQUEST_ACCEPTED)
            {
                OnConnected(peer.incomingAddress);
            }

            if (packet_id == (byte)Peer.RakNet_Packets_ID.CONNECTION_ATTEMPT_FAILED)
            {
                OnDisconnected("Connection attempt failed");
            }

            if (packet_id == (byte)Peer.RakNet_Packets_ID.INCOMPATIBLE_PROTOCOL_VERSION)
            {
                OnDisconnected("Incompatible protocol version");
            }

            if (packet_id == (byte)Peer.RakNet_Packets_ID.CONNECTION_LOST)
            {
                OnDisconnected("Time out");
            }

            if (packet_id == (byte)Peer.RakNet_Packets_ID.NO_FREE_INCOMING_CONNECTIONS)
            {
                OnDisconnected("Server is full.");
            }

            if (packet_id == (byte)Peer.RakNet_Packets_ID.DISCONNECTION_NOTIFICATION && !tmp_Banned && !tmp_Fake)
            {
                OnDisconnected("You are kicked!");
            }
        }
        else
        {
            switch(packet_id)
            { 
                case (byte)Packets_ID.CL_INFO:
                    if (m_NetworkWriter.StartWritting())
                    {
                        GameObject playerObj = Instantiate(playerReference);
                        int playerNum = playerObj.GetComponentInChildren<PlayerMovement>().Init(true);
                        playerObj.GetComponent<PlayerManager>().SetPlayer(true); ;
                        playersList.Add(playerObj.GetComponent<PlayerManager>());

                        m_NetworkWriter.WritePacketID((byte)Packets_ID.CL_INFO);
                        m_NetworkWriter.Write(m_ClientNetInfo.name);
                        m_NetworkWriter.WritePackedUInt64(m_ClientNetInfo.local_id);
                        m_NetworkWriter.Write(m_ClientNetInfo.client_hwid);
                        m_NetworkWriter.Write(m_ClientNetInfo.client_version);
                        m_NetworkWriter.Write(playerNum);
                        serveruid = peer.incomingGUID;
                        m_NetworkWriter.Send(peer.incomingGUID, Peer.Priority.Immediate, Peer.Reliability.Reliable, 0);//sending
                    }
                    break;
                case (byte)Packets_ID.NET_REGISTER:
                    {
                        bool success = m_NetworkReader.ReadBoolean();

                        if (success)
                        {
                            GameObject obj = GameObject.FindGameObjectWithTag("UIMaster");
                            obj.GetComponent<UIController>().SetToLogin();
                        }
                        else
                            Debug.Log("Username Already Exists");
                    }
                    break;
                case (byte)Packets_ID.CL_ACCEPTED:
                        m_ClientNetInfo.net_id = m_NetworkReader.ReadPackedUInt64();
                        Debug.Log("[Client] Accepted connection by server... [ID: " + m_ClientNetInfo.net_id + "]");
                    break;
                case (byte)Packets_ID.ID_WELCOME:
                    Debug.Log("welcome!!");
                    uint id = m_NetworkReader.ReadUInt32();
                    PlayerManager mgr = playersList[0];
                    mgr.pid = id;
                    int playerCount = m_NetworkReader.ReadInt32();

                    for (int i = 0; i < playerCount; ++i)
                    {
                        GameObject otherPlayer = Instantiate(playerReference);
                        PlayerManager otherManager = otherPlayer.GetComponent<PlayerManager>();
                        otherManager.pid = m_NetworkReader.ReadUInt32();
                        otherManager.position = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat());
                        otherManager.pRotation = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat());
                        otherManager.pName = m_NetworkReader.ReadString();
                        playersList.Add(otherManager);
                    }
                    SendInitialStats();

                    break;
                case (byte)Packets_ID.ID_MOVEMENT:
                    uint playerID = m_NetworkReader.ReadUInt32();

                    foreach (PlayerManager player in playersList)
                    {
                        if (player.pid == playerID)
                        {
                            //  ship.position = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), 0);
                            //  ship.pRotation = m_NetworkReader.ReadFloat();

                            // Step 8 : Instead of using ship.position, use server_pos and serverRotation
                            // set server position, server rotation
                            player.server_pos = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat());
                            player.serverRotation = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat());
                            Debug.Log("Server rotation: " + player.serverRotation);
                            // Lab 7 Task 1 : Read Extrapolation Data velocity x, velocity y & angular velocity
                            // set velocity and rotation velocity of ship (look at ship Manager)

                            player.velocity = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat());
                        }
                    }
                    break;
                case (byte)Packets_ID.ID_NEWPLAYER:
                    {
                        GameObject playerObj = Instantiate(playerReference);
                        PlayerManager otherManager = playerObj.GetComponent<PlayerManager>();
                        otherManager.pid = m_NetworkReader.ReadUInt32();
                        playerObj.GetComponentInChildren<PlayerMovement>().Init(false);
                        otherManager.pName = m_NetworkReader.ReadString();
                        otherManager.position = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat());
                        otherManager.pRotation = new Vector3(m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat(), m_NetworkReader.ReadFloat());

                        playersList.Add(otherManager);
                    }
                    break;

                case (byte)Packets_ID.ID_CHANGESCENE:
                    {
                        string sceneName = m_NetworkReader.ReadString();
                        if (Application.CanStreamedLevelBeLoaded(sceneName))
                        {
                            Scene nextScene = SceneManager.GetSceneByName(sceneName);
                            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                            SceneManager.MoveGameObjectToScene(this.gameObject, nextScene);

                            //foreach (PlayerManager player in playersList)
                            //{
                            //    SceneManager.MoveGameObjectToScene(player.gameObject, nextScene);
                            //}
                        }
                        else
                        {
                            // load another scene?
                        }
                    }
                    break;
            }
        }
    }

    private void SendInitialStats()
    {
        if (m_NetworkWriter.StartWritting())
        {
            PlayerManager me = playersList[0];
            me.pName = userName;
            m_NetworkWriter.WritePacketID((byte)Packets_ID.ID_INITIALSTATS);
            m_NetworkWriter.Write(me.pName);
            m_NetworkWriter.Write(me.position.x);
            m_NetworkWriter.Write(me.position.y);
            m_NetworkWriter.Write(me.position.z);
            m_NetworkWriter.Write(me.pRotation.x);
            m_NetworkWriter.Write(me.pRotation.y);
            m_NetworkWriter.Write(me.pRotation.z);

            m_NetworkWriter.Send(serveruid, Peer.Priority.Immediate, Peer.Reliability.Reliable, 0);
        }


    }
    private void OnConnected(string address)
    {
        Debug.Log("[Client] Connected to " + address);

        //формируем/готовим информацию клиента
        m_ClientNetInfo.name = "Player_"+Environment.MachineName;
        m_ClientNetInfo.local_id = peer.incomingGUID;
        m_ClientNetInfo.client_hwid = SystemInfo.deviceUniqueIdentifier;
        m_ClientNetInfo.client_version = Application.version;
    }

    private void OnDisconnected(string reason)
    {
        Debug.LogError("[Client] Disconnected" + (reason.Length > 0 ? " with reason: " + reason : "..."));
    }
}
