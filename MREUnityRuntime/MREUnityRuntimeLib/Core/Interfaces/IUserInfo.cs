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
        /// The user's display name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The world-space position to be the look-at target by other objects.
        /// </summary>
        Vector3 LookAtPosition { get; }
    }
}
