using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class PlayerNameUISetter : NetworkBehaviour
{
    [SerializeField] private TextMeshPro playerNameText;
    private NetworkVariable<FixedString32Bytes> playerNameNetworkVariable = new NetworkVariable<FixedString32Bytes>("",NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    private MatchMakerClient matchmakerClientScript;
    private void Awake()
    {
        if(matchmakerClientScript==null)
        {
            matchmakerClientScript = GameObject.FindAnyObjectByType<MatchMakerClient>();
        }
    }

    public override void OnNetworkSpawn()
    {
      
       // PlayerNameServerRpc();
        Debug.Log("IsOwner " + IsOwner);
        playerNameNetworkVariable.Value = matchmakerClientScript.playerName;
        Debug.Log("matchmakerClientScript.playerName " + matchmakerClientScript.playerName);
        playerNameText.text = playerNameNetworkVariable.Value.ToString();
        playerNameNetworkVariable.OnValueChanged += (FixedString32Bytes previousValue, FixedString32Bytes newValue) =>
        {
            Debug.Log("ValueChangeCalled!!!!!");
            playerNameText.text = newValue.ToString();
        };
    }
 
    [ServerRpc(RequireOwnership =false)]
     private void PlayerNameServerRpc()
    {
        Debug.Log("Enter Server Rpccc");
        playerNameNetworkVariable.Value = matchmakerClientScript.playerName;
        Debug.Log("matchmakerClientScript.playerName " + matchmakerClientScript.playerName);
        playerNameText.text = playerNameNetworkVariable.Value.ToString();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Key Downnnnnn!!!");
            playerNameNetworkVariable.Value = Random.Range(0, 100).ToString();
        }
    }
}
