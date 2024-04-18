using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
public class VivoxVoiceChat : MonoBehaviour
{

    private void Awake()
    {
       
    }
 
    // Start is called before the first frame update
    private async void  Start()
    {
        await VivoxService.Instance.InitializeAsync();
        VivoxService.Instance.LoggedIn += OnLoggedIn;
        VivoxService.Instance.LoggedOut += OnLoggedOut;
        LoginUserAsyn();

    }


    private void JoinGroupChannel()
    {
        VivoxService.Instance.JoinEchoChannelAsync("Haris",ChatCapability.AudioOnly,null);
    }
    private async void LoginUserAsyn()
    {
        var loginOptions = new LoginOptions()
        {
            PlayerId = AuthenticationService.Instance.PlayerId
        };
        await VivoxService.Instance.LoginAsync(loginOptions);
        JoinGroupChannel();
    }
    private void OnLoggedIn()
    {
        Debug.Log("Player Login Vivox");
    }

    private void OnLoggedOut()
    {
        Debug.Log("Player Logout Vivox");
    }

    private void OnDestroy()
    {
        VivoxService.Instance.LoggedIn -= OnLoggedIn;
        VivoxService.Instance.LoggedOut -= OnLoggedOut;
    }
}
