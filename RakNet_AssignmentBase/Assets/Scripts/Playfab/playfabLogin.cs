using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class playfabLogin : MonoBehaviour
{
    float timePassed = 0;
    bool loggedIn = false;
    // Start is called before the first frame update

    string MyPlayfabID = "";
    void Start()
    {
        PlayFabSettings.TitleId = "625D6"; // change 625D6.. title settings -> API features
        Login();
    }

    // Update is called once per frame
    void Update()
    {
        if (loggedIn)
        {
            timePassed += Time.deltaTime;

            if (timePassed > 1.5f)
            {
                SetData();
                timePassed = 0;
            }
        }
    }

    public void Login()
    {
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            CreateAccount = true,
            CustomId = "123456ABCDE"
        },
        result => { Debug.Log("Logged in"); loggedIn = true; },
        error => Debug.LogError(error.GenerateErrorReport()));
    }

    public void ButtonClicked()
    {
        PlayFabClientAPI.WritePlayerEvent(new WriteClientPlayerEventRequest()
        {
            Body = new Dictionary<string, object>() {
            { "PosX", 10 },
            { "PosY", 20 }
        },
            EventName = "button_is_clicked"
        },
        result => Debug.Log("Success"),
        error => Debug.LogError(error.GenerateErrorReport()));
    }

    public void SetData()
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>() {
            {"PosX", ""+transform.position.x},
            {"PosY", ""+transform.position.y},
            {"Rotation", ""+transform.eulerAngles.z},
        }
        },
           result => Debug.Log("Successfully updated user data"),
           error => {
               Debug.Log("Got error setting user data");
               Debug.Log(error.GenerateErrorReport());
           });
    }
    
    public void GetData()
    {
        GetAccountInfoRequest request = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(request, Success, 
            error => Debug.LogError(error.GenerateErrorReport()));
    }
    void Success(GetAccountInfoResult _result)
    {
        MyPlayfabID = _result.AccountInfo.PlayFabId;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest()
        {
            PlayFabId = MyPlayfabID,
            Keys = null
        }, result => {
            Debug.Log("Got user data:");
            if (result.Data == null || !result.Data.ContainsKey("PosX")) Debug.Log("PosX");
            else
            {
                Debug.Log("PosX: " + result.Data["PosX"].Value);

               //transform.position.Set()
               //transform.rotation.SetEulerAngles()
            }
        }, (error) => {
            Debug.Log(error.GenerateErrorReport());
        });
    }
        
}
