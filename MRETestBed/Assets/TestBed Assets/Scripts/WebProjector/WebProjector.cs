// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace AltspaceVR.WebProjector
{
	public class WebProjector : MonoBehaviour
	{
		public RoomServer RoomServer;
		public RoomClient RoomClient;
		public AudioSource AudioSource;
		public VideoSource VideoSource;
		public Controls Controls;
		public GameObject Screen;
		internal WebProjectorRPC RPC;
	}
}
