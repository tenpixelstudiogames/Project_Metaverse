using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Matchmaker;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;

#if UNITY_EDITOR
using ParrelSync;
#endif
public class MatchMakerClient : NetworkBehaviour
{
 

    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private GameObject emptyNameText;
    [SerializeField] private GameObject loginScreenBackground;
    [SerializeField] private GameObject loginScreenUI;
    [SerializeField] private Transform playerNameTransform;
  //  [SerializeField] private GameObject virtualButtons;
   // [SerializeField] private GameObject tempCamera;
    

    private const string PLAYER_NAME_UI_TAG = "PlayerName";

  

    private string ticketID;
    private float pollTicketTimer;
    private float pollTicketTimerMax = 1.1f;
    private CreateTicketResponse ticketResponse;
    public string playerName;
    /*  private NetworkVariable<MyCustomData> playerNameNetworkVariable = new NetworkVariable<MyCustomData>(new MyCustomData {
          playerName = ""
      }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
  */

    private NetworkVariable<FixedString32Bytes> playerNameNetVariable = new NetworkVariable<FixedString32Bytes>();
    private void Awake()
    {

      //  virtualButtons.SetActive(false);
    }
    private void Start()
    {
        loginScreenUI.SetActive(false);

        Invoke("DelayLoginScreen", 1.5f);
    }

    public struct MyCustomData : INetworkSerializable
    {
        public FixedString32Bytes playerName;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerName);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

      /*  playerNameNetworkVariable.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
         {
             Debug.Log("Inside NetworkVariable");
             Debug.Log("Is Serverrrrrrrr ValueChanged" + IsServer);
             Invoke("DelayPlayerNameSetter", 3);
         };*/
    }

    private void Client_OnClientConnectedCallback(ulong clientId)
    {
          if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("Id Matched for MatchMaker " + clientId);
            // Invoke("DelayPlayerNameSetter", 3);
          //  virtualButtons.SetActive(true);
            loginScreenBackground.SetActive(false);

        }

           // UpdatePlayerNameClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { clientId } } });

    }
/*
    [ServerRpc(RequireOwnership =false)]

    private void UpdatePlayerNameServerRpc()
    {
        Debug.Log("MatchMaker Client Rpc");
        UpdatePlayerNameClientRpc();
    }

    [ClientRpc]
    private void UpdatePlayerNameClientRpc()
    {
        Debug.Log("MatchMaker Client Rpc");
        Debug.Log("MatchMacker client Connected Callback");
        // if (!IsClient || !IsOwner) return;
        Debug.Log("MatchMacker client Connected Callback is Owner True");
        // GameObject playerNameUI = GameObject.FindGameObjectWithTag(PLAYER_NAME_UI_TAG);
        GameObject playerCharacter = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        GameObject playerNameUI = playerCharacter.transform.Find("PlayerName").gameObject;
        TextMeshPro playerNameText = playerNameUI.GetComponent<TextMeshPro>();
        if (!string.IsNullOrEmpty(playerName))
        {
            playerNameText.text = playerName;
            Debug.Log("MatchMacker client Connected Callback PlayerNameSet");
        }
        loginScreenBackground.SetActive(false);
        // Invoke("DelayPlayerNameSetter", 3);
    }
*/
    private void OnEnable()
    {
        ServerStartUp.ClientInstance += SignIn;
       
    }
    private void OnDisable()
    {
        ServerStartUp.ClientInstance -= SignIn;
    }
    private  void DelayLoginScreen()
    {
        loginScreenUI.SetActive(true);
    }
    private async void SignIn()
    {
        await ClientSignin("TestPlayer");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async Task ClientSignin( string serviceProfileName=null)
    {
        if(serviceProfileName!=null)
        {
            #if UNITY_EDITOR
            serviceProfileName = $"{serviceProfileName}{GetCloneNumberSuffix()}";
            #endif
            var initOptions = new InitializationOptions();
            initOptions.SetProfile(serviceProfileName);
            await UnityServices.InitializeAsync(initOptions);
        }

        else
        {
            await UnityServices.InitializeAsync();
        }
        Debug.Log($"SignedIn Anonymously as {serviceProfileName}({PlayerId()})");

    }

    private string PlayerId()
    {
        return AuthenticationService.Instance.PlayerId;
    }
#if UNITY_EDITOR
    private string GetCloneNumberSuffix()
    {
        {
            string projectPath = ClonesManager.GetCurrentProjectPath();
            int lastUnderscore = projectPath.LastIndexOf("_");
            string projectCloneSuffix = projectPath.Substring(lastUnderscore + 1);
            if (projectCloneSuffix.Length != 1)
                projectCloneSuffix = "";
            return projectCloneSuffix;

        }

    }
#endif

    public void StartClient()
    {
        if (string.IsNullOrEmpty(playerNameInputField.text))
        {
            emptyNameText.SetActive(true);
            return;
        }
        playerName = playerNameInputField.text;
        loginScreenUI.SetActive(false);
        CreateATicket();
    }

    private async void CreateATicket()
    {
        var options = new CreateTicketOptions("FFA");

        var players = new List<Player>
        {
            new Player(PlayerId())
        };

         ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
        ticketID = ticketResponse.Id;
        Debug.Log($"Ticket ID: {ticketID}");

        pollTicketTimer = pollTicketTimerMax;
        // PollTicketStatus();
    }

    private void Update()
    {
        if(ticketResponse!=null)
        {
            pollTicketTimer -= Time.deltaTime;
            if(pollTicketTimer<=0f)
            {
                pollTicketTimer = pollTicketTimerMax;
                Debug.Log("PollTicketStatus Update");
                PollTicketStatus();
            }
        }

 /*       if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Is Serverrrrrrrr space" + IsServer);
            int random = UnityEngine.Random.Range(0, 100);
            playerNameNetworkVariable.Value = new MyCustomData { playerName = random.ToString() };
        }*/
    }

    private async void PollTicketStatus()
    {
        Debug.Log("PollTicketStatus Update");
        MultiplayAssignment multiplayAssignment = null;
        var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketID);

       if(ticketStatus ==null)
        {
            Debug.Log("Null means no updates to this tickey, keep waiting");
            return;
        }

        if (ticketStatus.Type == typeof(MultiplayAssignment))
        {
            multiplayAssignment = ticketStatus.Value as MultiplayAssignment;

            switch (multiplayAssignment.Status)
            {
                case StatusOptions.Found:

                    TicketAssigned(multiplayAssignment);
                    break;
                case StatusOptions.InProgress:
                    break;
                case StatusOptions.Failed:

                    Debug.Log($"Failed to get the Status. Error {multiplayAssignment.Message}");
                    ticketResponse = null;
                    break;
                case StatusOptions.Timeout:
                    ticketResponse = null;
                    Debug.Log($"Failed to get the ticket status. Ticket Time Out ");
                    break;
                default:
                    throw new InvalidOperationException();
            }

        }

       

     
    }

    /* private async void PollTicketStatus()
     {
         MultiplayAssignment multiplayAssignment = null;
         bool gotAssignment = false;
         do
         {
             await Task.Delay(TimeSpan.FromSeconds(1f));
             var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketID);
             if (ticketStatus == null) continue;
             if (ticketStatus.Type == typeof(MultiplayAssignment))
             {
                 multiplayAssignment = ticketStatus.Value as MultiplayAssignment;

             }

             switch(multiplayAssignment.Status)
             {
                 case StatusOptions.Found:
                     gotAssignment = true;
                     TicketAssigned(multiplayAssignment);
                     break;
                 case StatusOptions.InProgress:
                     break;
                 case StatusOptions.Failed:
                     gotAssignment = true;
                     Debug.Log($"Failed to get the Status. Error {multiplayAssignment.Message}");
                     break;
                 case StatusOptions.Timeout:
                     gotAssignment = true;
                     Debug.Log($"Failed to get the ticket status. Ticket Time Out ");
                     break;
                 default:
                     throw new InvalidOperationException();
             }
         }
         while (!gotAssignment);
     }*/

    private void TicketAssigned(MultiplayAssignment assignment)
    {
        Debug.Log($"Ticket Assigned :{assignment.Ip }:{assignment.Port}");
        ticketResponse = null;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(assignment.Ip, (ushort)assignment.Port);
        NetworkManager.Singleton.OnClientConnectedCallback += Client_OnClientConnectedCallback;
        // Invoke("UpdatePlayerNameServerRpc", 3);
        //   Invoke("SpawnPlayerNameTextServerRpc", 3);

        KitchenGameMultiplayer.Instance.StartClient();

        //tempCamera.SetActive(false);

        //  if (!IsClient) return;
        // Invoke("DelayPlayerNameSetter", 3);
        // NetworkManager.Singleton.StartClient();
        //   NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        //  NetworkManager.Singleton.OnClientDisconnectCallback += DisClientConnected;

    }

    

    private void DelayPlayerNameSetter()
    {
      //  virtualButtons.SetActive(true);
        Debug.Log("MatchMacker client Connected Callback");
        // if (!IsClient || !IsOwner) return;
        Debug.Log("MatchMacker client Connected Callback is Owner True");
       // GameObject playerNameUI = GameObject.FindGameObjectWithTag(PLAYER_NAME_UI_TAG);
        GameObject playerCharacter = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        GameObject playerNameUI = playerCharacter.transform.Find("PlayerName").gameObject;
        TextMeshPro playerNameText = playerNameUI.GetComponent<TextMeshPro>();
        if (!string.IsNullOrEmpty(playerName))
        {
          //  playerNameText.text = playerNameNetworkVariable.Value.ToString();
            Debug.Log("MatchMacker client Connected Callback PlayerNameSet");
        }
        loginScreenBackground.SetActive(false);
      //  tempCamera.SetActive(false);
    }
    private async void ClientConnected(ulong obj)
    {
        UnityEngine.Debug.Log("Client Connected!!!");
    }
    private async void DisClientConnected(ulong obj)
    {
        UnityEngine.Debug.Log("Client Connected!!!");
    }

    [System.Serializable]
    public class MatchMakingPlayerData
    {
      
    }
}