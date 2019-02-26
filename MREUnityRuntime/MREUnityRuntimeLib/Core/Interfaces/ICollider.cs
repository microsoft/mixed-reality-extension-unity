// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension.Core.Interfaces
{
    // public enum CollisionLayer
    // {
    //     Object,
    //     Environment,
    //     Hologram
    // }

    interface ICollider
    {
        bool IsEnabled { get; }

        bool IsTrigger { get; }

        //CollisionLayer CollisionLayer { get; }

        ColliderType ColliderType { get; }
    }
}
