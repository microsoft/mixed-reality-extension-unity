// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Core
{
	internal struct SoundInstance
	{
		public Guid id;
		public Actor actor;
		public SoundInstance(Guid id, Actor actor)
		{
			this.id = id;
			this.actor = actor;
		}
	}

	internal class SoundManager
	{
		#region Constructor

		public SoundManager(MixedRealityExtensionApp app)
		{
			_app = app;
		}

		#endregion

		#region Public Methods

		public AudioSource AddSoundInstance(Actor actor, Guid id, AudioClip audioClip, MediaStateOptions options)
		{
			float offset = options.Time.GetValueOrDefault();
			if (options.Looping != null && options.Looping.Value && audioClip.length != 0.0f)
			{
				offset = offset % audioClip.length;
			}
			if (offset < audioClip.length)
			{
				var soundInstance = actor.gameObject.AddComponent<AudioSource>();
				soundInstance.clip = audioClip;
				soundInstance.time = offset;
				soundInstance.spatialBlend = 1.0f;
				soundInstance.spread = 90.0f;   //only affects multichannel sounds. Default to 50% spread, 50% stereo.
				soundInstance.minDistance = 1.0f;
				soundInstance.maxDistance = 1000000.0f;
				ApplyMediaStateOptions(actor, soundInstance, options, id, true);
				if (options.paused != null && options.paused.Value == true)
				{
					//start as paused
					soundInstance.Play();
					soundInstance.Pause();
				}
				else
				{
					//start as unpaused
					_unpausedSoundInstances.Add(new SoundInstance(id, actor));
					soundInstance.Play();
				}
				return soundInstance;
			}
			return null;
		}


		public void ApplyMediaStateOptions(Actor actor, AudioSource soundInstance, MediaStateOptions options, Guid id, bool startSound)
		{
			if (options != null)
			{
				//pause must happen before other sound state changes
				if (options.paused != null && options.paused.Value == true)
				{
					if (_unpausedSoundInstances.RemoveAll(x => x.id == id) > 0)
					{
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
				if (options.Spread != null)
				{
					soundInstance.spread = options.Spread.Value * 180.0f;
				}
				if (options.RolloffStartDistance != null)
				{
					soundInstance.minDistance = options.RolloffStartDistance.Value;
					soundInstance.maxDistance = options.RolloffStartDistance.Value * 1000000.0f;
				}
				if (options.Time != null)
				{
					soundInstance.time = options.Time.Value;
				}

				//unpause must happen after other sound state changes
				if (!startSound)
				{
					if (options.paused != null && options.paused.Value == false)
					{
						if (!_unpausedSoundInstances.Exists(x => x.id == id))
						{
							soundInstance.UnPause();
							_unpausedSoundInstances.Add(new SoundInstance(id, actor));
						}
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
				var soundInstance = _unpausedSoundInstances[_soundStoppedCheckIndex];
				if (!soundInstance.actor.CheckIfSoundExpired(soundInstance.id))
				{
					_soundStoppedCheckIndex++;
				}
			}
		}

		#endregion

		public void DestroySoundInstance(AudioSource soundInstance, Guid id)
		{
			_unpausedSoundInstances.RemoveAll(x => x.id == id);
			Component.Destroy(soundInstance);
		}

		#region Private Fields

		MixedRealityExtensionApp _app;
		private List<SoundInstance> _unpausedSoundInstances = new List<SoundInstance>();
		private int _soundStoppedCheckIndex = 0;

		#endregion
	}
}
