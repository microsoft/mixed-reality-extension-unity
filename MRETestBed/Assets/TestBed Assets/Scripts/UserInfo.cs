using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using System;
using System.Collections.Generic;
using UnityEngine;

internal class UserInfo : IUserInfo
{
    public UserInfo(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public GameObject UserGO { get; set; }

    public Guid Id { get; }

    public string Name { get; private set; }

    public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>()
    {
        {"host", "MRETestBed" },
        {"engine", "Unity 2018.1.9f2" }
    };

    public Vector3? LookAtPosition => UserGO.transform.position;

    public event MWEventHandler BeforeAvatarDestroyed;
    public event MWEventHandler AfterAvatarCreated;

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
