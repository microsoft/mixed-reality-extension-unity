// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.IPC;
using MixedRealityExtension.PluginInterfaces;
using System.Collections.Generic;
using UnityEngine;

internal class HostAppUser : IHostAppUser
{
	public GameObject UserGO { get; set; }

	public string HostUserId { get; }

	public string Name { get; private set; }

	public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>()
	{
		{"host", "MRETestBed" },
		{"engine", Application.version }
	};

	public Vector3? LookAtPosition => UserGO.transform.position;

	public event MWEventHandler BeforeAvatarDestroyed;
	public event MWEventHandler AfterAvatarCreated;

	public HostAppUser(string hostUserId, string name)
	{
		Debug.Log($"Creating host app with host user id: {hostUserId}");
		HostUserId = hostUserId;
		Name = name;
	}

	private static Transform FindChildRecursive(Transform parent, string name)
	{
		Transform transform = parent.Find(name);
		if (transform != null)
		{
			return transform;
		}
		for (int i = 0; i < parent.childCount; ++i)
		{
			transform = FindChildRecursive(parent.GetChild(i), name);
			if (transform != null)
			{
				return transform;
			}
		}
		return null;
	}

	public Transform GetAttachPoint(string attachPointName)
	{
		string socketName = $"socket-{attachPointName}";
		Transform socket = FindChildRecursive(UserGO.transform, socketName);
		if (socket == null)
		{
			socket = UserGO.transform;
		}
		return socket;
	}
}
