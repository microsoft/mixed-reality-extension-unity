using MixedRealityExtension.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Core.Interfaces
{
    /// <summary>
    /// Interface for providing information about a user.
    /// </summary>
    public interface IUserInfo
    {
        /// <summary>
        /// The obfuscated id of the user.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The user's display name. Null if user not found.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// TODO: Remove this in the next LookAt overhaul.
        /// The world-space position to be the look-at target by other objects. Null if user not found.
        /// </summary>
        Vector3? LookAtPosition { get; }

        /// <summary>
        /// Gets the transform of the specified attach point.
        /// </summary>
        /// <param name="attachPointName">The name of the attach point to retrieve.</param>
        /// <returns>The attach point transform, or null if not found.</returns>
        Transform GetAttachPoint(string attachPointName);

        /// <summary>
        /// Called before the user's avatar is destroyed.
        /// </summary>
        event MWEventHandler BeforeAvatarDestroyed;

        /// <summary>
        /// Called after the user's avatar is recreated.
        /// </summary>
        event MWEventHandler AfterAvatarCreated;
    }
}
