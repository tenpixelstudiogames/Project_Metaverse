using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using QFSW.QC;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using StarterAssets;
using Unity.Services.Vivox;
using VivoxUnity;
using System;
using UnityEngine.Video;

public class TestingRelay : NetworkBehaviour
{
    private ILoginSession LoginSession;
    private VideoPlayer vPlayer;
    public bool isParticipentSpeaking;
    public bool isStateChanged;
    private bool isClientSpawn;
    private double clientVideoPlayerTime;
    private NetworkVariable<double> currentVideoTimeNetwork = new NetworkVariable<double>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        //networkVariable = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


        // if(networkVariable.Value==1)


        currentVideoTimeNetwork.OnValueChanged += (double previousValue, double newValue) =>
        {
            clientVideoPlayerTime = currentVideoTimeNetwork.Value;
            // NetworkUIManager.instance.networkVariableText.text = currentVideoTimeNetwork.Value.ToString();

            //vPlayer.time = currentVideoTimeNetwork.Value;
            if (IsClient && isClientSpawn)
            {
                vPlayer.Play();
                vPlayer.time = currentVideoTimeNetwork.Value;               
                NetworkUIManager.instance.networkVariableText.text = vPlayer.time.ToString();
                Debug.Log("Current N_Variable Time " + currentVideoTimeNetwork.Value + "&& vPlayer.time " + vPlayer.time);
                Debug.Log("Client Spawn ");
                Invoke("SynceClientVideo",1);
                isClientSpawn = false;
            }
        };
    }

    private void SynceClientVideo()
    {
        vPlayer.time = clientVideoPlayerTime;
        Debug.Log("Invoke Run Value "+ clientVideoPlayerTime);
    }
    private void Awake()
    {
        if(vPlayer==null)
        {
            vPlayer = FindAnyObjectByType<VideoPlayer>();
        }
    }
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {

            Debug.Log("Signed in +" + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        VivoxService.Instance.Initialize();
        VivoxConfig vConfig = new VivoxConfig();
        vConfig.DefaultCodecsMask = MediaCodecType.Opus8;
        vConfig.EnableFastNetworkChangeDetection = true;
        Debug.Log("Current Docking "+vConfig.DisableAudioDucking);
        Client _client = new Client();
        _client.Initialize(vConfig);

       
    }


    private void Update()
    {
        if (!IsOwner) return;
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            vPlayer.Stop();
        }
        if (Input.GetKeyDown(KeyCode.CapsLock))
        {
            vPlayer.Play();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            vPlayer.time = 30;
        }

        if(IsHost && vPlayer.isPlaying)
        {
            currentVideoTimeNetwork.Value = vPlayer.time;
            //Debug.Log("Storing Network Variable " + currentVideoTimeNetwork.Value);
        }
        
      // Debug.Log("Current Time "+ vPlayer.time);
        /*    if(IsHost)
            {
                Debug.Log("This is no the Client");
               // vPlayer.clockResyncOccurred
            }*/
    }
    [Command]
    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "wss");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            vPlayer.clockResyncOccurred += VPlayer_clockResyncOccurred;
            vPlayer.Play();
        }
        catch (RelayServiceException e)
        {
            Debug.Log("Relalay Exception : " + e);
        }

    }

    private void VPlayer_clockResyncOccurred(VideoPlayer source, double seconds)
    {
        Debug.Log("Clock is Resyning");
    }

    [Command]
    private async void JoinRelay(string joinCode)
    {
        try
        {
         JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
           
           
            
            isClientSpawn = true;
        }
        catch (RelayServiceException e)
        {
            Debug.Log("Relalay Exception : " + e);
        }

    }

    #region Vivox


    private void BindChannelSessionHandlers(bool doBind, IChannelSession channelSession)
    {
        
        //Subscribing to the events
        if (doBind)
        {
            // Participants
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
           // channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
           
        }

        //Unsubscribing to the events
        else
        {
            // Participants
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
           // channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

        }
       
    }

    private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
    {
        
        //ValidateArgs(new object[] { sender, valueEventArg }); //see code from earlier in post

        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[valueEventArg.Key];

        string username = valueEventArg.Value.Account.Name;
        ChannelId channel = valueEventArg.Value.ParentChannelSession.Key;
        string property = valueEventArg.PropertyName;
       // Debug.Log("Property" + property);
        switch (property)
        {
          case "LocalMute":
                {
                   /* if (username != accountId.Name) //can't local mute yourself, so don't check for it
                    {
                        //update their muted image
                    }*/

                   // isParticipentSpeaking = false;
                    break;
                }
            case "SpeechDetected":
                {
                    //update speaking indicator image
                    isParticipentSpeaking = true;
                    Debug.Log("Participend Speaking");
                    break;
                }
            default:                
                break;
        }

       
    }

    private void OnParticipantAdded(object sender, KeyEventArg<string> e)
    {
        Debug.Log("Participend Added!!! ");
    }
    
    public void JoinChannel(string channelName, ChannelType channelType, bool connectAudio, bool connectText, bool transmissionSwitch = true, Channel3DProperties properties = null)
    {
        if (LoginSession.State == LoginState.LoggedIn)
        {
            Channel channel = new Channel(channelName, channelType, properties);

            IChannelSession channelSession = LoginSession.GetChannelSession(channel);

            BindChannelSessionHandlers(true, channelSession);

            channelSession.BeginConnect(connectAudio, connectText, transmissionSwitch, channelSession.GetConnectToken(), ar =>
            {
                try
                {
                    channelSession.EndConnect(ar);
                }
                catch (Exception e)
                {
                    BindChannelSessionHandlers(false, channelSession);
                    Debug.LogError($"Could not connect to channel: {e.Message}");
                    return;
                }
            });
        }
        else
        {
            Debug.LogError("Can't join a channel when not logged in.");
        }
    }


 
    [Command]
    public void Login(string displayName = null)
    {
        var account = new Account(displayName);
        bool connectAudio = true;
        bool connectText = true;
        
        LoginSession = VivoxService.Instance.Client.GetLoginSession(account);
        LoginSession.PropertyChanged += LoginSession_PropertyChanged;

        LoginSession.BeginLogin(LoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, ar =>
        {
            try
            {
                LoginSession.EndLogin(ar);
            }
            catch (Exception e)
            {
                // Unbind any login session-related events you might be subscribed to.
                // Handle error
                return;
            }
            // At this point, we have successfully requested to login. 
            // When you are able to join channels, LoginSession.State will be set to LoginState.LoggedIn.
            // Reference LoginSession_PropertyChanged()
        });
    }

    // For this example, we immediately join a channel after LoginState changes to LoginState.LoggedIn.
    // In an actual game, when to join a channel will vary by implementation.
    private void LoginSession_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        
        var loginSession = (ILoginSession)sender;
        if (e.PropertyName == "State")
        {
            if (loginSession.State == LoginState.LoggedIn)
            {
                bool connectAudio = true;
                bool connectText = true;

                // This puts you into an echo channel where you can hear yourself speaking.
                // If you can hear yourself, then everything is working and you are ready to integrate Vivox into your project.
                //JoinChannel("TestChannel", ChannelType.Echo, connectAudio, connectText);
                // To test with multiple users, try joining a non-positional channel.
                 JoinChannel("MultipleUserTestChannel", ChannelType.NonPositional, connectAudio, connectText);
                // Set VAD to automatic after login
              //  SetAutoVad();
            }
        }
    }


  /*  public void SetAutoVad()
    {
          var request = new vx_req_aux_set_vad_properties_t();
            request.account_handle = _accountHandle;
            request.vad_auto = 1;
            VxClient.Instance.BeginIssueRequest(request, null);

    }*/

    #endregion


}
