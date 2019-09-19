// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;

namespace MixedRealityExtension.Core.Interfaces
{
	/// <summary>
	/// The type of light.
	/// </summary>
	public enum LightType
	{
		/// <summary>
		/// Spot light.
		/// </summary>
		Spot,

		/// <summary>
		/// Point light.
		/// </summary>
		Point,

		/// <summary>
		/// Directional light.
		/// </summary>
		Directional
	}

	/// <summary>
	/// The interface that represents a light within the mixed reality extension runtime.
	/// </summary>
	public interface ILight
	{
		/// <summary>
		/// Gets whether the light is enabled.
		/// </summary>
		bool Enabled { get; }

		/// <summary>
		/// Gets the type of light it is.
		/// </summary>
		LightType Type { get; }

		/// <summary>
		/// Gets the color of the light.
		/// </summary>
		MWColor Color { get; }

		/// <summary>
		/// Gets the range of the light.
		/// </summary>
		float Range { get; }

		/// <summary>
		/// Gets the intensity of the light.
		/// </summary>
		float Intensity { get; }

		/// <summary>
		/// Gets the spot angle of the light.
		/// </summary>
		float SpotAngle { get; }
	}
}
