// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Util.Unity;
using UnityEngine;

namespace MixedRealityExtension.Behaviors.ActionData
{
	internal class PointData
	{
		public MWVector3 appSpacePoint;
		public MWVector3 localSpacePoint;

		public static PointData CreateFromUnityVector3(Vector3 point, Transform localRoot, Transform appRoot)
		{
			return new PointData()
			{
				localSpacePoint = new MWVector3().ToLocalMWVector3(point, localRoot),
				appSpacePoint = new MWVector3().ToLocalMWVector3(point, appRoot)
			};
		}
	}
}
