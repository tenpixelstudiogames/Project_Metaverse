using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
public class PlayerWorldCanvas : NetworkBehaviour
{
    [SerializeField] GameObject soundImage;
    private TestingRelay testingRelayScript;
    private bool isNeworkSpawned;
    private bool previousValue;
    private void Awake()
    {
        isNeworkSpawned = false;
        if (testingRelayScript==null)
        {
            testingRelayScript = FindAnyObjectByType<TestingRelay>();
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner & IsClient)
        {
            isNeworkSpawned = true;
            Debug.Log("This is the OWner Client"+ OwnerClientId);
            
        }
    }

    private void Update()
    {
        if (!isNeworkSpawned) return;
        if (!IsOwner) return;
        testingRelayScript = FindAnyObjectByType<TestingRelay>();
        previousValue = testingRelayScript.isParticipentSpeaking;
        Debug.Log("PreviousValue "+ previousValue);
        if (testingRelayScript.isParticipentSpeaking)
        {
           // Debug.Log("OnNetworkSpawn speaking");
            soundImage.SetActive(true);
        }
        else if (!testingRelayScript.isParticipentSpeaking)
        {
          //  Debug.Log("OnNetworkSpawn not speaking");
            soundImage.SetActive(false);
        }
       
       
    }
}
