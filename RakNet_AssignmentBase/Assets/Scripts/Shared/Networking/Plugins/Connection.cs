using System;
using System.Timers;

[Serializable]
public class Connection
{
    private Peer peer;
    private Timer timer = new Timer();

    public Connection(Peer peer, ulong guid, int id)
    {
        this.peer = peer;
        this.guid = guid;
        this.ID = id;
        ipaddress = this.peer.GetAddress(this.guid).Split(':')[0];
        connectedTime = DateTime.Now;
        NetStats = new NetStats();
        NetStats.averagePing = this.peer.GetPingAverage(this.guid);
        NetStats.lastPing = this.peer.GetPingLast(this.guid);
        timer.Elapsed += UpdatePing;
        timer.Interval = 100;
        timer.Start();
    }

    public void UpdatePing(object x, ElapsedEventArgs yu)
    {
        NetStats.averagePing = peer.GetPingAverage(guid);
        NetStats.lastPing = peer.GetPingLast(guid);
	}
	
    public TimeSpan GetConnectionTime()
    {
        TimeSpan diff = connectedTime - DateTime.Now;

        return diff;
    }

    public int ID;
    public ulong guid;
    public string ipaddress;
    public DateTime connectedTime;

    public ClientNetInfo Info;

    public NetStats NetStats;
}