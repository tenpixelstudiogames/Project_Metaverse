using System;
using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

using Photon.Voice.Unity;

namespace Release.Scripts.Player
{
    public class PlayerName : NetworkBehaviour
    {
        [SerializeField] private TMP_Text nameText;
      //  [SerializeField] private GameObject soundIcon;
        private MatchmakerUI matchMakerUIClientScript;
        private UnityVoiceClient unityVoiceClientScript;
        // Create a new network variable that can by default only be modified by the server
        private readonly NetworkVariable<FixedString32Bytes> _playerName = new();
       // private readonly NetworkVariable<bool> soundIndicator = new();

        private void Awake()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            if(matchMakerUIClientScript == null)
            {
                matchMakerUIClientScript = GameObject.FindAnyObjectByType<MatchmakerUI>();
            }

        }

        private void Singleton_OnClientConnectedCallback(ulong clientID)
        {
           if(clientID==NetworkManager.Singleton.LocalClientId)
            {
                if(unityVoiceClientScript==null)
                {
                    unityVoiceClientScript = GameObject.FindAnyObjectByType<UnityVoiceClient>();
                  //  unityVoiceClientScript.OnplayerSoundIndicatorStateChange += UnityVoiceClientScript_OnplayerSoundIndicatorStateChange;
                }
            }
        }

        private void UnityVoiceClientScript_OnplayerSoundIndicatorStateChange(object sender, bool soundIndicatorState)
        {
            Debug.Log("Sound Indicator Updated");
            ChangeSoundIndicator(soundIndicatorState);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _playerName.OnValueChanged += (_, newPlayerName) =>
            {
              //  nameText.text = newPlayerName.ToString();
            };
        }

        private void Start()
        {
            // When spawn ask the server to change our name
            if (IsOwner && IsLocalPlayer)
            {
                ChangePlayerName(matchMakerUIClientScript.playerName);
                
            }
            nameText.text = _playerName.Value.ToString();
                //soundIcon.SetActive(soundIndicator.Value);

        }

        private void ChangeSoundIndicator(bool soundIndicatorState)
        {
          //  if (IsServer)
               // soundIndicator.Value = soundIndicatorState;
          //  else
             //   ChangeSoundServerRpc(soundIndicatorState);
        }
        [ServerRpc]
        private void ChangeSoundServerRpc(bool soundIndicatorState)
        {
           // soundIndicator.Value = soundIndicatorState;
            ChangeSoundClientRpc(soundIndicatorState);
        }

        [ClientRpc]
        private void ChangeSoundClientRpc(bool soundIndicatorState)
        {
            Debug.Log("Client RPC Sound Indicator " + soundIndicatorState);
            //nameText.text = newName;
  
               // soundIcon.SetActive(soundIndicatorState);
    
        }


        private void ChangePlayerName(string playerName)
        {
            if (IsServer)
                _playerName.Value = playerName;
            else
                ChangePlayerNameServerRpc(playerName);
        }

        [ServerRpc]
        private void ChangePlayerNameServerRpc(string playerName)
        {
            _playerName.Value = playerName;
            UpdatePlayerNameClientRpc(playerName);
        }

        [ClientRpc]
        private void UpdatePlayerNameClientRpc(string newName)
        {
            Debug.Log("Client RPC PLayer Name "+newName);
            nameText.text = newName;
        }

        private void Update()
        {
            if (!IsOwner) return;
       /*     if(Input.GetKeyDown(KeyCode.O) && IsOwner && IsLocalPlayer)
            {
               
                ChangeSoundIndicator(true);
            }
            if (Input.GetKeyDown(KeyCode.P) && IsOwner && IsLocalPlayer)
            {
               
                ChangeSoundIndicator(false);
            }*/
        }
    }
}