using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using Unity.Services.Core;
#if DEDICATED_SERVER
using Unity.Services.Multiplay;
#endif
using System;
using Unity.Services.Matchmaker.Models;
using QFSW.QC.Utilities;
using System.Diagnostics;
using Newtonsoft.Json;
using Unity.Services.Matchmaker;
using UnityEngine.Video;

public class ServerStartUp : NetworkBehaviour
{
    public static event System.Action ClientInstance;
    private VideoPlayer videoPlayer;
    private double currentVideoTime;
#if DEDICATED_SERVER

    private string internalServerIP = "0.0.0.0";
    private string externalServerIP = "0.0.0.0";
    private ushort serverPort = 7777;
    private string externalConnectionString => $"{externalServerIP}:{serverPort}";

    
    const int multiplayerServiceTimeout = 20000;

    private string allocationId;
   
   

   // private BackfillTicket localBackFillTicket;
    private string localBackFillTicket;
    private CreateBackfillTicketOptions createBackfillTicketOptions;
    
    private const int ticketCheckMs = 1000;
    private MatchmakingResults matchmakingPayload;

    private ushort maxPlayers =10;
    private bool backfiling = false;

   
  


    private IMultiplayService multiplayService;
 private MultiplayEventCallbacks serverCallbacks;
  private IServerEvents serverEvents;
   private IServerQueryHandler m_ServerQueryHandler;
#endif
    private void Awake()
    {
        /*if (videoPlayer == null)
        {
            GameObject videoPlayerObj = GameObject.FindGameObjectWithTag("MainTV");
            videoPlayer = videoPlayerObj.GetComponent<VideoPlayer>();
            UnityEngine.Debug.Log("VideoPlayerGet!!!");
        }*/
    }
    async void Start()
    {

#if DEDICATED_SERVER

        bool server = false;
        var args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-dedicatedServer")
            {
               UnityEngine.Debug.Log("Its Server");
                server = true;
            }
            if(args[i]=="-port" && (i+1<args.Length))
            {
                serverPort = (ushort)int.Parse(args[i + 1]);
            }

            if(args[i]=="-ip" && (i + 1 < args.Length))
            {
                externalServerIP = args[i + 1];
            }
        }


#endif
#if DEDICATED_SERVER
        if (server)
        {
            StartServer();
            await StartServerServices();
        }
#endif
#if !DEDICATED_SERVER
        ClientInstance?.Invoke();
#endif
    }

#if DEDICATED_SERVER
    private void Update()
    {

        if (m_ServerQueryHandler == null) return;
        
    if(NetworkManager.Singleton.IsServer)
        {
            m_ServerQueryHandler.CurrentPlayers = (ushort)NetworkManager.Singleton.ConnectedClients.Count;
        }
        m_ServerQueryHandler.UpdateServerCheck();
       /* if(videoPlayer.isPlaying)
        {
            currentVideoTime = videoPlayer.time;
        }*/
    }


    private void StartServer()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(internalServerIP, serverPort);
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
      //  NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        
       //videoPlayer.Play();
      // UnityEngine.Debug.Log("VideoISPLaying!!!");
    }

    private void ClientConnected(ulong obj)
    {
      //throw new NotImplementedException();
     // VideoSyncClientRpc();
       
    }

    private  void ClientDisconnected(ulong obj)
    {
       if(!backfiling && NetworkManager.Singleton.ConnectedClients.Count>0 && NeedsPlayers())
        {
            UnityEngine.Debug.Log("ClientDisconnected Before Backfilling!!!");
             BeginBackfiling(matchmakingPayload);
        }
    }

    private async Task StartServerServices()
    {
       
        await UnityServices.InitializeAsync();

        try
        {

            multiplayService = MultiplayService.Instance;

            m_ServerQueryHandler = await multiplayService.StartServerQueryHandlerAsync(maxPlayers, "n/a", "n/a", "0", "n/a");

        } 
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Something went wrong in SQP Service " + e);
        }

        try
        {
          
            matchmakingPayload = await GetMatchMakerPayload(multiplayerServiceTimeout);
            if(matchmakingPayload != null)
            {
                UnityEngine.Debug.Log($"Got Payload: {matchmakingPayload}");
                await StartBackFill(matchmakingPayload);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Getting the Matchmaker Payload time out, starting with defaults");
            }
        }
        catch(Exception e)
        {
            UnityEngine.Debug.Log("Something went wrong in Allocation & Backfill Service " + e);

        }

    }


    private async Task<MatchmakingResults> GetMatchMakerPayload(int timeout)
    {
        var matchmakerPayloadTask = SubscribeAndWaitMatchmakerAllocation();
        
        // Define a Stopwatch to measure the elapsed time
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeout && !matchmakerPayloadTask.IsCompleted)
        {
            await Task.Yield(); // Allow other tasks to execute
        }
        stopwatch.Stop();
        if (matchmakerPayloadTask.IsCompleted)
        {
           
            return matchmakerPayloadTask.Result;
        }
        else
        {
            return null;
        }

    }

    private async Task<MatchmakingResults> SubscribeAndWaitMatchmakerAllocation()
    {

        if (multiplayService == null) return null;
        UnityEngine.Debug.Log("SubscribeAndWaitMatchmakerAllocation ");
        allocationId = null;
        serverCallbacks = new MultiplayEventCallbacks();
        serverCallbacks.Allocate += OnMultiplayAllocation;
        serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks);

        allocationId = await AwaitAllocationId();

        var mmPayload = await GetMatchMatcherAllocationPayloadAsyn();
        return mmPayload;
    }

    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        UnityEngine.Debug.Log($"OnAllocation: {allocation.AllocationId}");
        if (string.IsNullOrEmpty(allocation.AllocationId)) return;
        allocationId = allocation.AllocationId;
    }

    private async Task<string> AwaitAllocationId()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        var config = multiplayService.ServerConfig;

        UnityEngine.Debug.Log($" Awaiting Allocation. Server Config is:\n" +
            $"-ServerID: {config.ServerId}\n" +
            $"-AllocationID: {config.AllocationId}\n" +
            $"-Port: {config.Port}\n" +
            $"-QPort: {config.QueryPort}\n" +
            $"-logs: {config.ServerLogDirectory}");
        while(string.IsNullOrEmpty(allocationId))
        {
            var configId = config.AllocationId;
            if(!string.IsNullOrEmpty(configId)&& string.IsNullOrEmpty(allocationId))
            {
                allocationId = configId;
                stopwatch.Stop();
                
                break;
            }

            while(stopwatch.ElapsedMilliseconds<100)
            {
                await Task.Yield();
            }
            stopwatch.Restart();
        }
        return allocationId;
    }

    private async Task<MatchmakingResults> GetMatchMatcherAllocationPayloadAsyn()
    {
        try
        {
            var payLoadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

            var modelAsJson = JsonConvert.SerializeObject(payLoadAllocation, Formatting.Indented);
            UnityEngine.Debug.Log($"{nameof(GetMatchMatcherAllocationPayloadAsyn)}:\n" +
                $"{modelAsJson}");
            return payLoadAllocation;
        }
         catch(Exception e)
        {
            UnityEngine.Debug.Log($"Something went wrong to get the MatchMaker Payload in GetMatchMatcherAllocationPayloadAsyn: \n{e}");
        }
        return null;
    }

    private async Task StartBackFill(MatchmakingResults payload)
    {
        UnityEngine.Debug.Log("StartBackfilling!!!");
        var backFillProperties = new BackfillTicketProperties(payload.MatchProperties);
        //  localBackFillTicket = new BackfillTicket { Id = payload.MatchProperties.BackfillTicketId, Properties = backFillProperties };
        localBackFillTicket = payload.BackfillTicketId;

          await BeginBackfiling(payload);
    }


    private async Task BeginBackfiling(MatchmakingResults payload)
    {
        UnityEngine.Debug.Log("BeginBackfillingStart!!!");
        var matchProperties = payload.MatchProperties;

        UnityEngine.Debug.Log($"QueName {payload.QueueName}!!!");
 
            createBackfillTicketOptions = new CreateBackfillTicketOptions
            {
                Connection = externalConnectionString,
                QueueName = payload.QueueName,
                Properties = new BackfillTicketProperties(matchProperties)
            };

            localBackFillTicket = await MatchmakerService.Instance.CreateBackfillTicketAsync(createBackfillTicketOptions);
            UnityEngine.Debug.Log("BeginBackfillTicketCreated!!!");

        backfiling = true;
#pragma warning disable 4014
        BackFillLoop();
#pragma warning restore 4014
    }


    private async Task BackFillLoop()
    {
        UnityEngine.Debug.Log("BackFillLoopBefore!!!");
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (backfiling && NeedsPlayers())
        {
            UnityEngine.Debug.Log("BackFillLoopInside!!!");
          BackfillTicket  backFillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(localBackFillTicket);
            localBackFillTicket = backFillTicket.Id;
                if(!NeedsPlayers())
            {
                UnityEngine.Debug.Log("BackFillLoopNotNeedsPlayers!!!");
                await MatchmakerService.Instance.DeleteBackfillTicketAsync(localBackFillTicket);
                    localBackFillTicket = null;
                backfiling = false;
                stopwatch.Stop();
                    return;
                }


            while(stopwatch.ElapsedMilliseconds<ticketCheckMs)
            {
                await Task.Yield();
            }

            stopwatch.Restart();
        }
        backfiling = false;
    }

    private bool NeedsPlayers()
    {
         
        if(NetworkManager.Singleton.ConnectedClients.Count<maxPlayers)
        {
            UnityEngine.Debug.Log("NeedsPlayerTrue!!!");
            return true;
        }
        else
        {
            UnityEngine.Debug.Log("NeedsPlayerFalse!!!");
            return false;
        }
    }


    private void Dispose()
    {

        serverCallbacks.Allocate -= OnMultiplayAllocation;
        serverEvents?.UnsubscribeAsync();

    }

/*    [ClientRpc]
    private void VideoSyncClientRpc()
    {
        UnityEngine.Debug.Log("Client Rpc Run!!!");
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            videoPlayer.time = currentVideoTime;
        }

    }*/

#endif



}
