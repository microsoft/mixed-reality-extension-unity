// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using UnityEngine;

namespace MixedRealityExtension.Util.Unity
{
	internal static class MWGOTreeWalker
	{
		public delegate void VisitorFn(GameObject gameObject);

		public static void VisitTree(GameObject treeRoot, VisitorFn fn)
		{
			fn(treeRoot);

			// Walk children to add to the actors flat list.
			Transform transform = treeRoot.transform;
			for (int i = 0; i < transform.childCount; ++i)
			{
				var childGO = transform.GetChild(i).gameObject;
				VisitTree(childGO, fn);
			}
		}
	}
}
