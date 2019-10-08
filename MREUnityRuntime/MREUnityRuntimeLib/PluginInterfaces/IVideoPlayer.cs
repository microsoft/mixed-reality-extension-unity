// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using MixedRealityExtension.Core;
namespace MixedRealityExtension.PluginInterfaces
{
	public interface IVideoPlayer
	{
		void Play(VideoStreamDescription description, MediaStateOptions options);
		void Destroy();
		void ApplyMediaStateOptions(MediaStateOptions options);
	}
}
