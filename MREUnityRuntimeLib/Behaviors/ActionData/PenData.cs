// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System.Collections.Generic;
using System.Linq;

namespace MixedRealityExtension.Behaviors.ActionData
{
	/// <summary>
	/// Class that represents the discrete draw data for a single from of the pen tool.
	/// </summary>
	public class DrawData
	{
		/// <summary>
		/// The transform for the collected draw position.
		/// </summary>
		public MWTransform Transform { get; set; }
	}

	/// <summary>
	/// Class that represents the pen tool action data.
	/// </summary>
	public class PenData : BaseToolData
	{
		public override bool IsEmpty => !DrawData.Any();

		/// <summary>
		/// The list of draw data entries from the pen tool.
		/// </summary>
		public IList<DrawData> DrawData { get; set; } = new List<DrawData>();

		/// <inheritdoc />
		public override void Reset()
		{
			DrawData.Clear();
		}
	}
}
