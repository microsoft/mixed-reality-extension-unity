using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using System;
using System.Collections.Generic;
using UnityEngine;

internal class UserInfo : IUserInfo
{
    public UserInfo(Guid id)
    {
        Id = id;
    }

    public GameObject UserGO { get; set; }

    public Guid Id { get; }

    public string Name => UserGO.name;

    public Dictionary<string, string> Properties => new Dictionary<string, string>()
    {
        {"host", "MRETestBed" },
        {"engine", "Unity 2018.1.9f2" }
    };

    public Vector3? LookAtPosition => UserGO.transform.position;

    public event MWEventHandler BeforeAvatarDestroyed;
    public event MWEventHandler AfterAvatarCreated;

    public Transform GetAttachPoint(string attachPointName)
    {
        return UserGO.transform;
    }
}
