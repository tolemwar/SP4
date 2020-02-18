//packet ids
public enum Packets_ID : byte
{ 
    NULL = 135,
    CL_ACCEPTED,
    CL_INFO,
    CL_FAKE,
    CL_BANNED,

    NET_LOGIN,
    NET_REGISTER,
    NET_CHAT,

    ID_MOVEMENT,
    ID_NEWPLAYER,
    ID_WELCOME,
    ID_INITIALSTATS,
    ID_CHANGESCENE


    /* unused in demo
    NET_SERVER_INFO,
    NET_CHAT_MESSAGE,

    //player
    NET_PLAYER_ARRAY,//sync player entities
    NET_PLAYER_CREATE,//call create player (only client)
    NET_PLAYER_DESTROY,//destroy player
    NET_PLAYER_CMD,//receiving *UserCmd's from client (only client)
    NET_PLAYER_UPDATE,//receiving *Snapshots from server


    //timecyc
    NET_GAME_TIMECYC//sync day time
    */
}
