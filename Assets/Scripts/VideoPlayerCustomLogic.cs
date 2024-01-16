using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerCustomLogic : MonoBehaviour
{
    public double syncedTime; // Variable that will update similar to the VideoPlayer's time
    public double playbackSpeed = 1.0; // Speed at which the variable updates
    public VideoPlayer videoplayer;
    private double currentTime = 0.0;
    private double videoDuration;
    private void Start()
    {
        videoDuration = videoplayer.length;
        Debug.Log("Video Lenght "+videoDuration);
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(!videoplayer.isPlaying)
            {
                videoplayer.Play();
            }

            videoplayer.time = syncedTime;
        }
       if(currentTime>=videoDuration)
        {
            currentTime = 0;
        }
        // Increase the current time based on the elapsed time since the last frame
        currentTime += Time.deltaTime * playbackSpeed;

        // Update the syncedTime variable with the current time
        syncedTime = currentTime;
        Debug.Log(syncedTime);
    }
}
