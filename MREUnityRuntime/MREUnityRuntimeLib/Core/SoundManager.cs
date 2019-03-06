// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Core
{
    internal class SoundManager
    {
        #region Constructor

        public SoundManager(MixedRealityExtensionApp app)
        {
            _app = app;
        }

        #endregion

        #region Public Methods

        public void AddSoundInstance(Guid id, AudioSource soundInstance)
        {
            _soundInstances.Add(id, soundInstance);
        }

        public bool TryGetSoundInstance(Guid id, out AudioSource soundInstance)
        {
            return _soundInstances.TryGetValue(id, out soundInstance);
        }

        public void TrackUnpauseSound(Guid id)
        {
            _unpausedSoundInstances.Add(id);
        }

        public void ApplySoundStateOptions(AudioSource soundInstance, SoundStateOptions options, Guid id)
        {
            if (options != null)
            {
                //pause must happen before other sound state changes
                if (options.paused != null && options.paused.Value == true)
                {
                    var index = _unpausedSoundInstances.FindIndex(x => x == id);
                    if (index >= 0)
                    {
                        _unpausedSoundInstances.RemoveAt(index);
                        soundInstance.Pause();
                    }
                }

                if (options.Volume != null)
                {
                    soundInstance.volume = options.Volume.Value;
                }
                if (options.Pitch != null)
                {
                    //convert from halftone offset (-12/0/12/24/36) to pitch multiplier (0.5/1/2/4/8).
                    soundInstance.pitch = Mathf.Pow(2.0f, (options.Pitch.Value / 12.0f));
                }
                if (options.Looping != null)
                {
                    soundInstance.loop = options.Looping.Value;
                }
                if (options.Doppler != null)
                {
                    soundInstance.dopplerLevel = options.Doppler.Value;
                }
                if (options.MultiChannelSpread != null)
                {
                    soundInstance.spread = options.MultiChannelSpread.Value * 180.0f;
                }
                if (options.RolloffStartDistance != null)
                {
                    soundInstance.minDistance = options.RolloffStartDistance.Value;
                    soundInstance.maxDistance = options.RolloffStartDistance.Value * 1000000.0f;
                }

                //unpause must happen after other sound state changes
                if (options.paused != null && options.paused.Value == false)
                {
                    var index = _unpausedSoundInstances.FindIndex(x => x == id);
                    if (index < 0)
                    {
                        soundInstance.UnPause();
                        _unpausedSoundInstances.Add(id);
                    }
                }


            }
        }

        public void Update()
        {
            //garbage collect expired sounds, one per frame
            if (_soundStoppedCheckIndex >= _unpausedSoundInstances.Count)
            {
                _soundStoppedCheckIndex = 0;
            }
            else
            {
                var id = _unpausedSoundInstances[_soundStoppedCheckIndex];
                if (_soundInstances.TryGetValue(id, out AudioSource soundInstance) && !soundInstance.isPlaying)
                {
                    DestroySoundInstance(soundInstance, id);
                }
                else
                {
                    _soundStoppedCheckIndex++;
                }
            }
        }

        #endregion

        public void DestroySoundInstance(AudioSource soundInstance, Guid id)
        {
            Component.Destroy(soundInstance);
            _soundInstances.Remove(id);
            var index = _unpausedSoundInstances.FindIndex(x => x == id);
            if (index >= 0)
            {
                _unpausedSoundInstances.RemoveAt(index);
            }
        }

        #region Private Fields

        MixedRealityExtensionApp _app;
        private static Dictionary<Guid, AudioSource> _soundInstances = new Dictionary<Guid, AudioSource>();
        private static List<Guid> _unpausedSoundInstances = new List<Guid>();
        private int _soundStoppedCheckIndex = 0;

        #endregion
    }
}
