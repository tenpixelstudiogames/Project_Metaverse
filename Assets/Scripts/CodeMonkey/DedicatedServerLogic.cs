using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;

using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
#if DEDICATED_SERVER
using Unity.Services.Multiplay;
#endif
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class DedicatedServerLogic : NetworkBehaviour {


    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private const string MAIN_VIDEO_PLAYER_NAME = "MainTV";


    public static DedicatedServerLogic Instance { get; private set; }


    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;




    private float heartbeatTimer;
    private float listLobbiesTimer;

    private bool isVideoPlayerTimeSet = false;

    public double syncedTime; // Variable that will update similar to the VideoPlayer's time
    public double playbackSpeed = 1.0; // Speed at which the variable updates
    public VideoPlayer videoPlayer;
    private double currentTime = 0.0;
    private double videoDuration;

    private NetworkVariable<double> videoPlayerCurrentTime = new NetworkVariable<double>();

#if DEDICATED_SERVER
    private float autoAllocateTimer = 9999999f;
    private bool alreadyAutoAllocated;
    private static IServerQueryHandler serverQueryHandler; // static so it doesn't get destroyed when this object is destroyed
    private string backfillTicketId;
    private float acceptBackfillTicketsTimer;
    private float acceptBackfillTicketsTimerMax = 1.1f;
    private PayloadAllocation payloadAllocation;
    private bool localDirty = false;
    private bool videoPlayOnServer = false;
    private MatchProperties matchProperties;
#endif


    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();

        if (videoPlayer == null)
        {
            //vPlayer = FindObjectOfType<VideoPlayer>();
            GameObject vPlayerGameobject = GameObject.FindGameObjectWithTag(MAIN_VIDEO_PLAYER_NAME);
            videoPlayer = vPlayerGameobject.GetComponent<VideoPlayer>();
        }
    }

    private void Start() {
        PlayerMultiplayerHandler.Instance.OnPlayerDataNetworkListChanged += KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
    }


    public override void OnNetworkSpawn()
    {
        videoPlayerCurrentTime.OnValueChanged += (double previousValue, double currentValue) =>
        {
            if (IsClient)
            {
                // Debug.Log("Network Variable Changed on Client");
                if (videoPlayer.isPlaying && !isVideoPlayerTimeSet)
                {
                    videoPlayer.time = videoPlayerCurrentTime.Value;
                    Invoke("SyncClientVideo", 1f);
                    isVideoPlayerTimeSet = true;
                }

            }
        };
    }

    private void SyncClientVideo()
    {
        videoPlayer.time = videoPlayerCurrentTime.Value;
        Debug.Log("Invoke Run Value " + videoPlayerCurrentTime.Value);
    }



    private async void KitchenGameMultiplayer_OnPlayerDataNetworkListChanged(object sender, NetworkListEvent<PlayerData> playerData) {
#if DEDICATED_SERVER
        Debug.Log("Player Data index " + playerData.Index);
        Debug.Log("Player Data PreviousValue " + playerData.PreviousValue);
        Debug.Log("Player Data Value " + playerData.Value);
        Debug.Log("Player Data Type " + playerData.Type);
        HandleUpdateBackfillTickets();

        if (KitchenGameMultiplayer.Instance.HasAvailablePlayerSlots()) {
            await MultiplayService.Instance.ReadyServerForPlayersAsync();
        } else {
            await MultiplayService.Instance.UnreadyServerAsync();
        }
#endif
    }

    private async void InitializeUnityAuthentication() {
        if (UnityServices.State != ServicesInitializationState.Initialized) {
            InitializationOptions initializationOptions = new InitializationOptions();
#if !DEDICATED_SERVER
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());
#endif

            await UnityServices.InitializeAsync(initializationOptions);

#if !DEDICATED_SERVER
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
#endif

#if DEDICATED_SERVER
            Debug.Log("DEDICATED_SERVER LOBBY");

            MultiplayEventCallbacks multiplayEventCallbacks = new MultiplayEventCallbacks();
            multiplayEventCallbacks.Allocate += MultiplayEventCallbacks_Allocate;
            multiplayEventCallbacks.Deallocate += MultiplayEventCallbacks_Deallocate;
            multiplayEventCallbacks.Error += MultiplayEventCallbacks_Error;
            multiplayEventCallbacks.SubscriptionStateChanged += MultiplayEventCallbacks_SubscriptionStateChanged;
            IServerEvents serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(multiplayEventCallbacks);

            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(100, "MyServerName", "KitchenChaos", "5.6", "Default");

            var serverConfig = MultiplayService.Instance.ServerConfig;
            if (serverConfig.AllocationId != "") {
                // Already Allocated
                MultiplayEventCallbacks_Allocate(new MultiplayAllocation("", serverConfig.ServerId, serverConfig.AllocationId));
            }
#endif
        } else {
            // Already Initialized
#if DEDICATED_SERVER
            Debug.Log("DEDICATED_SERVER LOBBY - ALREADY INIT");

            var serverConfig = MultiplayService.Instance.ServerConfig;
            if (serverConfig.AllocationId != "") {
                // Already Allocated
                MultiplayEventCallbacks_Allocate(new MultiplayAllocation("", serverConfig.ServerId, serverConfig.AllocationId));
            }
#endif
        }
    }

#if DEDICATED_SERVER
    private void MultiplayEventCallbacks_SubscriptionStateChanged(MultiplayServerSubscriptionState obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_SubscriptionStateChanged");
        Debug.Log(obj);
    }

    private void MultiplayEventCallbacks_Error(MultiplayError obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_Error");
        Debug.Log(obj.Reason);
    }

    private void MultiplayEventCallbacks_Deallocate(MultiplayDeallocation obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_Deallocate");
    }

    private void MultiplayEventCallbacks_Allocate(MultiplayAllocation obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_Allocate");

        if (alreadyAutoAllocated) {
            Debug.Log("Already auto allocated!");
            return;
        }

        SetupBackfillTickets();

        alreadyAutoAllocated = true;

        var serverConfig = MultiplayService.Instance.ServerConfig;
        Debug.Log($"Server ID[{serverConfig.ServerId}]");
        Debug.Log($"AllocationID[{serverConfig.AllocationId}]");
        Debug.Log($"Port[{serverConfig.Port}]");
        Debug.Log($"QueryPort[{serverConfig.QueryPort}]");
        Debug.Log($"LogDirectory[{serverConfig.ServerLogDirectory}]");

        string ipv4Address = "0.0.0.0";
        ushort port = serverConfig.Port;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipv4Address, port, "0.0.0.0");

        KitchenGameMultiplayer.Instance.StartServer();
        videoDuration = videoPlayer.length;
        Debug.Log("Video Duration Assigned!!!");
        videoPlayOnServer = true;

    }

    private async void SetupBackfillTickets() {
        Debug.Log("SetupBackfillTickets");
        payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();

        backfillTicketId = payloadAllocation.BackfillTicketId;
        Debug.Log("backfillTicketId: " + backfillTicketId);

        acceptBackfillTicketsTimer = acceptBackfillTicketsTimerMax;
    }

    private async void HandleUpdateBackfillTickets() {
        if (backfillTicketId != null && payloadAllocation != null && KitchenGameMultiplayer.Instance.HasAvailablePlayerSlots()) {
            Debug.Log("HandleUpdateBackfillTickets");

            List<Unity.Services.Matchmaker.Models.Player> playerList = new List<Unity.Services.Matchmaker.Models.Player>();

            foreach (PlayerData playerData in KitchenGameMultiplayer.Instance.GetPlayerDataNetworkList()) {
                playerList.Add(new Unity.Services.Matchmaker.Models.Player(playerData.playerId.ToString()));
            }

            matchProperties = new MatchProperties(
              payloadAllocation.MatchProperties.Teams,
               playerList,
               payloadAllocation.MatchProperties.Region,
               payloadAllocation.MatchProperties.BackfillTicketId
           );
            localDirty = true;
            /*         try {
                         await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId,
                             new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(matchProperties))
                         );
                     } catch (MatchmakerServiceException e) {
                         Debug.Log("Error: " + e);
                     }*/
        }
    }

    private async void HandleBackfillTickets() {
        if (KitchenGameMultiplayer.Instance.HasAvailablePlayerSlots()) {
            BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
            backfillTicketId = backfillTicket.Id;
        }

    }

    private async Task StopBackFilling()
    {
        await MatchmakerService.Instance.DeleteBackfillTicketAsync(backfillTicketId);
        backfillTicketId = null;
    }

    [Serializable]
    public class PayloadAllocation {
        public Unity.Services.Matchmaker.Models.MatchProperties MatchProperties;
        public string GeneratorName;
        public string QueueName;
        public string PoolName;
        public string EnvironmentId;
        public string BackfillTicketId;
        public string MatchId;
        public string PoolId;
    }


    private async Task UpdatedBackfillTicket()
    {
        try {
            await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId,
        new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(matchProperties))
    );
            localDirty = false;
        }
        catch (MatchmakerServiceException e)
        {
            Debug.Log("Error: " + e);
            localDirty = false;
        }


    }
#endif
    private void Update() {


#if DEDICATED_SERVER
        autoAllocateTimer -= Time.deltaTime;
        if (autoAllocateTimer <= 0f) {
            autoAllocateTimer = 999f;
            MultiplayEventCallbacks_Allocate(null);
        }

        if (serverQueryHandler != null) {
            if (NetworkManager.Singleton.IsServer) {
                serverQueryHandler.CurrentPlayers = (ushort)NetworkManager.Singleton.ConnectedClientsIds.Count;
            }
            serverQueryHandler.UpdateServerCheck();
        }

        if (backfillTicketId != null) {
            acceptBackfillTicketsTimer -= Time.deltaTime;
            if (acceptBackfillTicketsTimer <= 0f) {

                if (localDirty)
                {
                    UpdatedBackfillTicket();
                }
                else if (!localDirty)
                {
                    HandleBackfillTickets();
                }

                if (!KitchenGameMultiplayer.Instance.HasAvailablePlayerSlots())
                {
                    StopBackFilling();
                    return;
                }


                acceptBackfillTicketsTimer = acceptBackfillTicketsTimerMax;
            }
        }

        if (!videoPlayOnServer || NetworkManager.Singleton.ConnectedClients.Count <= 0)
        {
            currentTime = 0;
            return;
        } 

        if (currentTime >= videoDuration)
        {
            currentTime = 0;
        }
        // Increase the current time based on the elapsed time since the last frame
        currentTime += Time.deltaTime * playbackSpeed;

        // Update the syncedTime variable with the current time
        syncedTime = currentTime;
        videoPlayerCurrentTime.Value = syncedTime;
     
#endif
    }

  


 
  

    private async Task<Allocation> AllocateRelay() {
        try {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(PlayerMultiplayerHandler.MAX_PLAYER_AMOUNT - 1);

            return allocation;
        } catch (RelayServiceException e) {
            Debug.Log(e);

            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation) {
        try {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return relayJoinCode;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode) {
        try {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        }
    }


    
}