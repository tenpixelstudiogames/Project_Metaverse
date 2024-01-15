using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.Unity;
using Unity.Netcode;
public class PlayerSpeakIndicator : NetworkBehaviour
{
    private Speaker speakerScript;
    private GameObject playerSoundIndicator;

    private const string PLAYER_SOUND_INDICATOR_TAG = "PlayerSoundIndicator";

    private void Awake()
    {
        if (!IsClient || !IsOwner) return;
        Debug.Log("PlayerSpeakIndicator is the Client and the Owner");
        speakerScript = this.GetComponent<Speaker>();      
        if(playerSoundIndicator == null)
        {
            playerSoundIndicator = GameObject.FindGameObjectWithTag(PLAYER_SOUND_INDICATOR_TAG);
            playerSoundIndicator.SetActive(false);
            Debug.Log("PlayerSpeakIndicator Name "+ playerSoundIndicator.name +" And Player Name "+ playerSoundIndicator.transform.root.name);
        }
      
    }
    // Update is called once per frame
    void Update()
    {
        if (!IsClient || !IsOwner) return;
        if(speakerScript.IsPlaying)
        {
            Debug.Log("Speaker is Playing");
            playerSoundIndicator.SetActive(true);
        }
        else
        {
            playerSoundIndicator.SetActive(false);
        }
    }
}
