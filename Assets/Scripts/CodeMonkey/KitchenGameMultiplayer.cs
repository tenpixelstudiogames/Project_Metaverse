using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Voice.Unity;
public class KitchenGameMultiplayer : NetworkBehaviour {

    //[SerializeField] private Transform playerNameTransform;
  
    private ulong myPlayerClientId=99999999;
    public const int MAX_PLAYER_AMOUNT = 4;
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";

    private UnityVoiceClient unityVoiceClient;
    public static KitchenGameMultiplayer Instance { get; private set; }


    public static bool playMultiplayer = true;


    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;


    [SerializeField] private List<Color> playerColorList;


    private NetworkList<PlayerData> playerDataNetworkList;
    private string playerName;

    private GameObject playerSoundIndicator;
    private const string PLAYER_SOUND_INDICATOR_TAG = "PlayerSoundIndicator";
  //  private VoiceConnection voiceConnection = new VoiceConnection();

    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(100, 1000));

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
    }

    private void Start() {
        if (!playMultiplayer) {
            // Singleplayer
           // StartHost();
        }  
    }

    private void VoiceConnection_OnplayerSoundIndicatorStateChange(object sender, bool e)
    {
        Debug.Log("VoiceConnection_OnplayerSoundIndicatorStateChange Invoke");
       // if (myPlayerClientId == 99999999) return;
     //   Debug.Log("myPlayerClientId BEFORE If " + myPlayerClientId);
        /*if (NetworkManager.Singleton.LocalClientId==myPlayerClientId)
        {
            Debug.Log("myPlayerClientId inside If");
            if (playerSoundIndicator != null)
            {
                playerSoundIndicator.SetActive(e);
                Debug.Log("Indicator Updated Through Event");
            }

        }*/

          //  UpdatePlayerSoundIndicatorClientRpc(e,new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { myPlayerClientId } } });

    }
    [ClientRpc]
    private void UpdatePlayerSoundIndicatorClientRpc(bool e,ClientRpcParams clientParams)
    {
        Debug.Log("Kitchen Multiplayer Client Rpc");
        if (playerSoundIndicator != null)
        {
            playerSoundIndicator.SetActive(e);
            Debug.Log("Indicator Updated Through Event");
        }
    }

    public string GetPlayerName() {
        return playerName;
    }

    public void SetPlayerName(string playerName) {
        this.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent) {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StartHost() {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    public void StartServer() {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartServer();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId) {
        Debug.Log("Client Disconnected " + clientId);

        for (int i = 0; i < playerDataNetworkList.Count; i++) {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId) {
                // Disconnected!
                playerDataNetworkList.RemoveAt(i);
            }
        }

#if DEDICATED_SERVER
        Debug.Log("playerDataNetworkList.Count " + playerDataNetworkList.Count);
      /*  if (SceneManager.GetActiveScene().name == "TestingMultiplayer")
        {
            // Player leaving during GameScene
            if (playerDataNetworkList.Count <= 0)
            {
                // All players left the game
                Debug.Log("All players left the game");
                Debug.Log("Shutting Down Network Manager");
                NetworkManager.Singleton.Shutdown();
                Application.Quit();
                //Debug.Log("Going Back to Main Menu");
                //Loader.Load(Loader.Scene.MainMenuScene);
            }
        }

        */
#endif
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId) {
        Debug.Log("Client Connected " + " " + clientId);

     /*   if (IsClient && !KitchenGameLobby.Instance.videoPlayer.isPlaying)
        {
            KitchenGameLobby.Instance.videoPlayer.Play();
            Debug.Log("Client Video Played");
        }*/

        playerDataNetworkList.Add(new PlayerData {
            clientId = clientId
        });

#if !DEDICATED_SERVER
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
#endif
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse) {


        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT) {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        connectionApprovalResponse.Approved = true;
        connectionApprovalResponse.CreatePlayerObject = true;
        connectionApprovalResponse.PlayerPrefabHash = null;
        connectionApprovalResponse.Position = Vector3.zero;
        connectionApprovalResponse.Rotation = Quaternion.identity;
       
    }

    public bool HasAvailablePlayerSlots() {
        return NetworkManager.Singleton.ConnectedClientsIds.Count < MAX_PLAYER_AMOUNT;
    }

    public void StartClient() {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId) {
        Debug.Log("Client Owner ID " + OwnerClientId);
        Debug.Log("LocalClientId ID " + NetworkManager.Singleton.LocalClientId);

        Debug.Log("CallBack Client ID  " + clientId);
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            myPlayerClientId = clientId;
            Debug.Log("Id Matched for MultiplayerClass " + clientId);
            if (IsClient && !KitchenGameLobby.Instance.videoPlayer.isPlaying)
            {
                KitchenGameLobby.Instance.videoPlayer.Play();
                Debug.Log("Client Video Played");

            }
        }
                Debug.Log("Inside Client of PlayerIndicator");
                if (playerSoundIndicator == null)
                {
                    GameObject playerCharacter = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
                     playerSoundIndicator = playerCharacter.transform.Find("Image").gameObject;
                    // playerSoundIndicator = GameObject.FindGameObjectWithTag(PLAYER_SOUND_INDICATOR_TAG);
                    playerSoundIndicator.SetActive(false);
                    Debug.Log("PlayerSpeakIndicator Name " + playerSoundIndicator.name + " And Player Name " + playerSoundIndicator.transform.root.name);

       /*             if (unityVoiceClient == null)
                    {
                        Debug.Log("Enter UnityVoiceClient ");

                       unityVoiceClient = GameObject.FindGameObjectWithTag("UnityVoiceClient").GetComponent<UnityVoiceClient>();
                        //unityVoiceClient.OnplayerSoundIndicatorStateChange += VoiceConnection_OnplayerSoundIndicatorStateChange;
                        Debug.Log("UnityVoiceClient " + unityVoiceClient.gameObject.name);
                    }*/

                    /*  VoiceConnection voiceConnection = new VoiceConnection();

                      voiceConnection.OnplayerSoundIndicatorStateChange += VoiceConnection_OnplayerSoundIndicatorStateChange;*/
                }
            
        
       
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId) {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

 

  

    public NetworkList<PlayerData> GetPlayerDataNetworkList() {
        return playerDataNetworkList;
    }

    public bool IsPlayerIndexConnected(int playerIndex) {
        return playerIndex < playerDataNetworkList.Count;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId) {
        for (int i=0; i< playerDataNetworkList.Count; i++) {
            if (playerDataNetworkList[i].clientId == clientId) {
                return i;
            }
        }
        return -1;
    }

    public PlayerData GetPlayerDataFromClientId(ulong clientId) {
        foreach (PlayerData playerData in playerDataNetworkList) {
            if (playerData.clientId == clientId) {
                return playerData;
            }
        }
        return default;
    }

    public PlayerData GetPlayerData() {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex) {
        return playerDataNetworkList[playerIndex];
    }

    public Color GetPlayerColor(int colorId) {
        return playerColorList[colorId];
    }

    public void ChangePlayerColor(int colorId) {
        ChangePlayerColorServerRpc(colorId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerColorServerRpc(int colorId, ServerRpcParams serverRpcParams = default) {
        if (!IsColorAvailable(colorId)) {
            // Color not available
            return;
        }

        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.colorId = colorId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private bool IsColorAvailable(int colorId) {
        foreach (PlayerData playerData in playerDataNetworkList) {
            if (playerData.colorId == colorId) {
                // Already in use
                return false;
            }
        }
        return true;
    }

    private int GetFirstUnusedColorId() {
        for (int i = 0; i<playerColorList.Count; i++) {
            if (IsColorAvailable(i)) {
                return i;
            }
        }
        return -1;
    }

    public void KickPlayer(ulong clientId) {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }

}

