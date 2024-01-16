using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;
using System.Collections;

public class MatchmakerTicketer : MonoBehaviour
{
    public Text AuthIdText;
    public Text TicketIdText;
    public Text InfoPaneText;
    public Button AuthButton;
    public Button FindMatchButton;
    public Text FindMatchButtonText;
    public string QueueName = "default-queue";
    private string ticketId = "";
    private bool searching = false;
    private IEnumerator pollingCoroutine = null;
    private TicketStatusResponse ticketStatusResponse = null;

    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();
    }

    public async void OnSignIn() 
    {
        Debug.Log("OnSignIn");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        AuthIdText.text = AuthenticationService.Instance.PlayerId;
        Debug.Log(AuthenticationService.Instance.AccessToken);
        AuthButton.interactable = false;
        FindMatchButton.interactable = true;
    }

    public async void OnFindMatch() 
    {
        Debug.Log("OnFindMatch");
        ClearInfoPane();

        try
        {
            // Check toggle
            if (!searching)
            {
                if (ticketId.Length > 0)
                {
                    LogToInfoViewAndConsole($"A Matchmaking ticket is already active for this client!");
                    return;
                }

                FindMatchButtonText.text = "Quit Queue";
                searching = true;

                await StartSearch();
            }
            else
            {
                if (ticketId.Length == 0)
                {
                    LogToInfoViewAndConsole("Cannot delete ticket as no ticket is currently active for this client!");
                    return;
                }

                await StopSearch();
                FindMatchButtonText.text = "Find Match";
                searching = false;
            }
        }
        catch (Exception e)
        {
            LogToInfoViewAndConsole(e.Message);
        }
    }

    private async Task StartSearch() 
    {
        var attributes = new Dictionary<string, object>();
        var players = new List<Player>
        { 
            new Player(AuthenticationService.Instance.PlayerId, new Dictionary<string, object>{ {"skill", 455.6} }), 
        };

        // Set options for matchmaking
        var options = new CreateTicketOptions(QueueName, attributes);

        // Create ticket
        var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
        TicketIdText.text = ticketId = ticketResponse.Id;
        LogToInfoViewAndConsole($"Ticket '{ticketResponse.Id}' created!");
        
        //Poll ticket status
        pollingCoroutine = PollTicketStatus();
        StartCoroutine(pollingCoroutine);
    }

    private async Task StopSearch()
    {
        //Stop any active coroutines
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
        }

        //Delete ticket
        await MatchmakerService.Instance.DeleteTicketAsync(TicketIdText.text);
        ClearInfoPane();
        LogToInfoViewAndConsole("Ticket deleted!");
        TicketIdText.text = "N/A";
        ticketId = "";
    }

    //This async task call is wrapped in a Coroutine to ensure WebGL compatibility.
    private IEnumerator GetTicket()
    {
        async Task GetTicketAsync()
        {
            ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(TicketIdText.text);
        }

        var task = GetTicketAsync();
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            if (task.Exception != null)
            {
                Debug.LogException(task.Exception);

                // Note that exceptions on IEnumerators / coroutines are generally not handled
                throw task.Exception;
            }
        }
    }

    private IEnumerator PollTicketStatus()
    {
        Debug.Log("PollTicketStatus");

        string waitingMessage = "Finding match...";
        string preMessagePaneText = Environment.NewLine + InfoPaneText.text;
        InfoPaneText.text = waitingMessage + preMessagePaneText;

        ticketStatusResponse = null;
        MultiplayAssignment assignment = null;
        bool gotAssignment = false;

        while (!gotAssignment)
        {
            waitingMessage += ".";
            InfoPaneText.text = waitingMessage + preMessagePaneText;

            StartCoroutine(GetTicket());
            yield return new WaitForSeconds(2f);

            if (ticketStatusResponse != null)
            {
                if (ticketStatusResponse.Type == typeof(MultiplayAssignment))
                {
                    assignment = ticketStatusResponse.Value as MultiplayAssignment;
                }

                if (assignment == null)
                {
                    var message = $"GetTicketStatus returned a type that was not a {nameof(MultiplayAssignment)}. This operation is not supported.";
                    throw new InvalidOperationException(message);
                }

                switch (assignment.Status)
                {
                    case StatusOptions.Found:
                        gotAssignment = true;
                        break;
                    case StatusOptions.InProgress:
                        //Do nothing
                        break;
                    case StatusOptions.Failed:
                        ClearInfoPane();
                        LogToInfoViewAndConsole("Failed to get ticket status. See logged exception for more details.");
                        throw new MatchmakerServiceException(MatchmakerExceptionReason.Unknown, assignment.Message);
                    case StatusOptions.Timeout:
                        gotAssignment = true;
                        ClearInfoPane();
                        LogToInfoViewAndConsole("Failed to get ticket status. Ticket timed out.");
                        break;
                    default:
                        throw new InvalidOperationException("Assignment status was a value other than 'In Progress', 'Found', 'Timeout' or 'Failed'! " +
                            $"Mismatch between Matchmaker SDK expected responses and service API values! Status value: '{assignment.Status}'");
                }
            }
        }

        ClearInfoPane();
        object toSerialize = assignment != null ? (object)assignment : (object)ticketStatusResponse;
        string jsonOutput = JsonConvert.SerializeObject(toSerialize, Formatting.Indented);
        LogToInfoViewAndConsole(jsonOutput);
    }

    private void LogToInfoViewAndConsole(string output) 
    {
        Debug.Log(output);
        InfoPaneText.text = output + Environment.NewLine + InfoPaneText.text;
    }

    private void ClearInfoPane() 
    {
        InfoPaneText.text = "";
    }
}
