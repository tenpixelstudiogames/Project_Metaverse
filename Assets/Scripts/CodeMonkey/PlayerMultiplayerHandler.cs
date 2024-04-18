using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Voice.Unity;
public class PlayerMultiplayerHandler : NetworkBehaviour {

    public const int MAX_PLAYER_AMOUNT = 100;


    public static PlayerMultiplayerHandler Instance { get; private set; }


    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler<NetworkListEvent<PlayerData>> OnPlayerDataNetworkListChanged;

    private NetworkList<PlayerData> playerDataNetworkList;


    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent) {
        OnPlayerDataNetworkListChanged?.Invoke(this, changeEvent);
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

        playerDataNetworkList.Add(new PlayerData {
            clientId = clientId
        });

#if !DEDICATED_SERVER
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
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {

            Debug.Log("Id Matched for MultiplayerClass " + clientId);
            if (!DedicatedServerLogic.Instance.videoPlayer.isPlaying)
            {
                DedicatedServerLogic.Instance.videoPlayer.Play();
                Debug.Log("Client Video Played");

            }
        }
              
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

    public void KickPlayer(ulong clientId) {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }

}

