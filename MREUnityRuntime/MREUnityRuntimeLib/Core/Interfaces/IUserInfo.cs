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
		/// The unobfuscated id of the user. Handle with care.
		/// </summary>
		string InvariantId { get; }

		/// <summary>
		/// The user's display name. Null if user not found.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Generic user properties. Usually informational only.
		/// </summary>
		Dictionary<string, string> Properties { get; }

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
