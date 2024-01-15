# Matchmaker SDK Unity - Event Polling Sample

Unity project to exercise the Matchmaker SDK. The project is intended to be used for manual testing of the SDK. It provides a simple UI with buttons.
You are required to link your project to a Matchmaker-enabled project on [Unity Dashboard](https://dashboard.unity3d.com/) to use this sample.

## Using the project

1. Open the project with the Unity editor (2020.3+).
2. Load the Sample scene 'EventPollingManualTestScene'.
3. Enter Play mode.
4. Click 'Sign In' to get an authentication id, the text label will update with the returned value.
5. Click 'Find Match' to send the default configured Matchmaking Ticket.
6. A matched ticket will return an assignment object that will be logged out to the UI console with `Status: Found`.
7. Click 'Quit Queue' to delete the existing ticket (this sample only allows 1 matchmaking ticket at any one time and does not auto cleanup on exit).

The text fields will be updated with the data returned by the SDK. Keep an eye on the console for errors.

### Gotchas
- If you only see a blue background, make sure that you have selected the proper scene before pressing play.
- If you are unable to get an assignment, ensure you have setup and linked your project (Edit -> Project Settings -> Services) to a Matchmaker-enabled project on [Unity Dashboard](https://dashboard.unity3d.com/).
- The default behaviour of the sample attempts to assign a ticket to the default matchmaking queue. If one is not setup on the linked project, the sample will be unable to match the ticket.
- If you get 404s with 'INVALID_SESSION_TOKEN' errors when attempting to authenticate for the first time, add a call to `AuthenticationService.Instance.ClearSessionToken()` to clear this once before attempting to authenticate.
