using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Unity.Netcode;
public class OtherTVScreens : NetworkBehaviour
{
    private VideoPlayer vPlayerMain;
    private VideoPlayer thisVPlayer;  
    private bool isTimeSet=false;
    private void Awake()
    {
        /* if (vPlayerMain == null)
         {
             //vPlayer = FindObjectOfType<VideoPlayer>();
             GameObject vPlayerGameobject = GameObject.FindGameObjectWithTag("MainTV");
             vPlayerMain = vPlayerGameobject.GetComponent<VideoPlayer>();
         }*/
        if (vPlayerMain == null)
        {
            //vPlayer = FindObjectOfType<VideoPlayer>();
            GameObject vPlayerGameobject = GameObject.FindGameObjectWithTag("MainTV");
            vPlayerMain = vPlayerGameobject.GetComponent<VideoPlayer>();
          if(vPlayerMain!=null)
            {
                Debug.Log("Other TV Player Get");
            }
          
        }
        thisVPlayer = this.GetComponent<VideoPlayer>();

  
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsClient)
        {
            Debug.Log("Is Client Other");
        }
    }


    // Update is called once per frame
    void Update()
    {

        if (!IsClient) return;
        if (vPlayerMain.isPlaying && !isTimeSet)
        {
            Debug.Log("Inside IF Other TV");
            thisVPlayer.Play();
            thisVPlayer.time = vPlayerMain.time;
            Invoke("SynceClientVideo", 1f);
            Debug.Log("Other is Playing!!!");
            isTimeSet = true;
        }

    }
    private void SynceClientVideo()
    {    
        thisVPlayer.time = vPlayerMain.time;
        Debug.Log("Other TV Invoke is Playing!!!");
    }
}
