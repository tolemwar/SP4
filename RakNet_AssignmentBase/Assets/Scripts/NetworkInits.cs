using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class NetworkInits : MonoBehaviour
{
    public GameObject serverObj;
    public GameObject clientObj;
    public Client_Demo currentObject;

    int numOfClients = 0;
    public void InitServer(int _port)
    {
        GameObject client = Instantiate(serverObj);
        client.GetComponent<Server_Demo>().Init(_port);
    }
    public void InitClient(string _ip, int _port, string _name)
    {
        if(numOfClients < 2)
        {
            GameObject client = Instantiate(clientObj);
            currentObject = client.GetComponent<Client_Demo>();
            currentObject.Init(_ip, _port, _name);
            ++numOfClients;
        }
    }
    public void LoginRegister(string _login, string _pw, bool _bool)
    {
        //currentObject.SendLoginRegister(_login, _pw, _bool);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
