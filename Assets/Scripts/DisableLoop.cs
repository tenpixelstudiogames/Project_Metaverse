using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableLoop : MonoBehaviour
{
    AudioSource speakerPrefabAudioSource;
    private void Awake()
    {
        speakerPrefabAudioSource = this.GetComponent<AudioSource>();
    }
    void Start()
    {
        speakerPrefabAudioSource.loop = false;

    }

    // Update is called once per frame
    void Update()
    {
       // Debug.Log("Source Loop " + speakerPrefabAudioSource.loop);
    }
}
