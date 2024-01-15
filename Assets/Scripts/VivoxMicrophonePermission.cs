using UnityEngine;
using UnityEngine.Android;

public class VivoxMicrophonePermission : MonoBehaviour
{
    private const string MicrophonePermission = Permission.Microphone;
   
    private void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(MicrophonePermission))
        {
            Permission.RequestUserPermission(MicrophonePermission);
        }
    }

    private void OnGUI()
    {
        if (!Permission.HasUserAuthorizedPermission(MicrophonePermission))
        {
            GUI.Label(new Rect(10, 10, 200, 50), "Microphone permission is required for voice chat.");
        }
    }
}
