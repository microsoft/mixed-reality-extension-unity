// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using UnityEngine;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// A factory class that instantiates a video player
	/// </summary>
	public interface IAudioController
	{
		void ModifyAudioSource(AudioSource audioSource);
	}
}
