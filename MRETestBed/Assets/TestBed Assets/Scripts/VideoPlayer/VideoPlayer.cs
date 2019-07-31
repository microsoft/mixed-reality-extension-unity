using AvStreamPlugin;
using MixedRealityExtension;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

internal class VideoPlayer : MonoBehaviour, IVideoPlayer
{
    private VideoSourceDescription videoSourceDescription;
    private AVStream avStream;
    private AudioSource audioSource;
    private IActor actor;

    public void Awake()
    {
        avStream = GetComponentInChildren<AVStream>();
        audioSource = GetComponentInChildren<AudioSource>();
    }

    public void SetActor(IActor actor)
    {
        this.actor = actor;
    }

    public void Play(VideoStreamDescription description, MediaStateOptions options)
    {
        videoSourceDescription = description as VideoSourceDescription;
        if (videoSourceDescription != null)
        {
            avStream.ConnectToVideo(videoSourceDescription.VideoDetails);
        }

        audioSource.spatialBlend = 1.0f;
        audioSource.spatialize = true;
        audioSource.spread = 90.0f;
        audioSource.minDistance = 1.0f;
        audioSource.maxDistance = 1000000.0f;

        if (!options.paused.HasValue)
        {
            options.paused = false;
        }
        ApplyMediaStateOptions(options);
    }

    public void Destroy()
    {
        Destroy(this.gameObject);
    }

    public void ApplyMediaStateOptions(MediaStateOptions options)
    {
        if (options != null)
        {
            // Pause must happen before other media state changes.
            if (options.paused != null && options.paused.Value == true)
            {
                avStream.Pause();
            }

            if (options.Volume != null)
            {
                audioSource.volume = options.Volume.Value;
            }
            if (options.Pitch != null)
            {
                // Convert from halftone offset (-12/0/12/24/36) to pitch multiplier (0.5/1/2/4/8).
                audioSource.pitch = Mathf.Pow(2.0f, (options.Pitch.Value / 12.0f));
            }
            if (options.Spread != null)
            {
                audioSource.spread = options.Spread.Value * 180.0f;
            }
            if (options.RolloffStartDistance != null)
            {
                audioSource.minDistance = options.RolloffStartDistance.Value;
                audioSource.maxDistance = options.RolloffStartDistance.Value * 1000000.0f;
            }
            if (options.Time != null)
            {
                avStream.Seek(TimeSpan.FromSeconds(options.Time.Value));
            }
            if (options.Visible != null)
            {
                GetComponentInChildren<UnityEngine.Renderer>().enabled = options.Visible.Value;
            }

            // Play must happen after other media state changes.
            if (options.paused != null && options.paused.Value == false)
            {
                avStream.Play();
            }
        }
    }
}
