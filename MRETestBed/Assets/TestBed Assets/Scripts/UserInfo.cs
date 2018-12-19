using MixedRealityExtension.Core.Interfaces;
using System;
using UnityEngine;

internal class UserInfo : IUserInfo
{
    public Guid UserId { get; set; }

    public GameObject UserGO { get; set; }

    public Guid Id => UserId;

    public string Name => UserGO.name;

    public Vector3 LookAtPosition => UserGO.transform.position;
}
