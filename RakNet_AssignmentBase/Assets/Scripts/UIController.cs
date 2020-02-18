using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum UISTATE
{
    NONE,
    CLIENTSERVER,
    LOGINREGISTER,
    USERPASSWORD,
    USERNAME

}
public enum REGISTERSTATE
{
    NONE,
    LOGIN,
    REGISTER
}

public enum PANELS
{
    GENERICPANEL,
    CLIENTSERVER,
    IPTEXT,
    PORTTEXT,
    USERNAME,

/* FOR INTERFACE
    CLIENTLOGINREGISTER,
    CLIENTINPUT,
    HEADERTEXT,
    LOGINREGISTERBTN,
    LOGINUSERNAME,
    LOGINPASSWORD,*/
    MAXPANELS
}
public class UIController : MonoBehaviour
{

    public GameObject[] UIPanels;
    public NetworkInits networkInit;

    private REGISTERSTATE registerState = REGISTERSTATE.NONE;
    private UISTATE currentState = UISTATE.NONE;
    
    // Start is called before the first frame update
    void Start()
    {
        this.enabled = false;
    }
    public void SetToLogin()
    {
        currentState = UISTATE.LOGINREGISTER;
        UpdateUI();
    }
    public void LoginRegisterPressed()
    {
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        Debug.Log(btnObj.GetComponentInChildren<Text>().text);

      /*FOR INTERFACE
        networkInit.LoginRegister(UIPanels[(int)PANELS.LOGINUSERNAME].GetComponent<Text>().text, UIPanels[(int)PANELS.LOGINPASSWORD].GetComponent<Text>().text, btnObj.GetComponentInChildren<Text>().text.StartsWith("L"));
        */
    }
    public void TogglePanel(bool _bool)
    {
        if (_bool)
            currentState = UISTATE.CLIENTSERVER;
        else
            currentState = UISTATE.NONE;
        UpdateUI();
    }
    public void CreateOK()
    {
        if (UIPanels[(int)PANELS.IPTEXT].GetComponent<Text>().text == "")
            InitServer(int.Parse(UIPanels[(int)PANELS.PORTTEXT].GetComponent<Text>().text));
        else
        {
            InitClient(UIPanels[(int)PANELS.IPTEXT].GetComponent<Text>().text, int.Parse(UIPanels[(int)PANELS.PORTTEXT].GetComponent<Text>().text), UIPanels[(int)PANELS.USERNAME].GetComponent<Text>().text);
        }

    }

    public void LoginRegisterClicked()
    {
        registerState = REGISTERSTATE.NONE;
        GameObject go = EventSystem.current.currentSelectedGameObject;
        if (go != null)
        {
            if (go.name == "Login")
                registerState = REGISTERSTATE.LOGIN;
            else
                registerState = REGISTERSTATE.REGISTER;

            currentState = UISTATE.USERPASSWORD;
            UpdateUI();
        }
    }

    private void InitServer(int _port)
    {
        networkInit.InitServer(_port);
        currentState = UISTATE.NONE;

        UpdateUI();
    }

    private void InitClient(string _ip, int _port, string _userName)
    {
        networkInit.InitClient(_ip, _port, _userName);     
        currentState = UISTATE.LOGINREGISTER;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        for(int i = 0; i<(int)PANELS.MAXPANELS; ++i)
        {
            UIPanels[i].SetActive(false);
        }

        switch(currentState)
        {
            case UISTATE.NONE:
                UIPanels[0].SetActive(false);
                break;

            case UISTATE.CLIENTSERVER:
                UIPanels[0].SetActive(true);
                UIPanels[(int)PANELS.CLIENTSERVER].SetActive(true);
                UIPanels[(int)PANELS.IPTEXT].SetActive(true);
                UIPanels[(int)PANELS.PORTTEXT].SetActive(true);
                UIPanels[(int)PANELS.USERNAME].SetActive(true);
                break;

             /* FOR INTERFACE
            case UISTATE.LOGINREGISTER:
                UIPanels[0].SetActive(true);
                UIPanels[(int)PANELS.CLIENTLOGINREGISTER].SetActive(true);
                break;
            case UISTATE.USERPASSWORD:
                UIPanels[0].SetActive(true);
                UIPanels[(int)PANELS.CLIENTINPUT].SetActive(true);
                UIPanels[(int)PANELS.LOGINREGISTERBTN].SetActive(true);
                UIPanels[(int)PANELS.HEADERTEXT].SetActive(true);
                UIPanels[(int)PANELS.LOGINUSERNAME].SetActive(true);
                UIPanels[(int)PANELS.LOGINPASSWORD].SetActive(true);

                switch (registerState)
                {
                    case REGISTERSTATE.LOGIN:
                        UIPanels[(int)PANELS.HEADERTEXT].GetComponent<Text>().text = "LOGIN";
                        UIPanels[(int)PANELS.LOGINREGISTERBTN].GetComponent<Text>().text = "LOGIN";

                        break;
                    case REGISTERSTATE.REGISTER:
                        UIPanels[(int)PANELS.LOGINREGISTERBTN].GetComponent<Text>().text = "REGISTER";
                        break;
                }
                break;*/
        }
    }

}
