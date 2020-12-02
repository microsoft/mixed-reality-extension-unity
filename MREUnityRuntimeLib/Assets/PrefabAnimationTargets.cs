// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GLTF.Schema;
using MixedRealityExtension.Core;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// Stores animation clip targets as lists of transform tree positions
	/// </summary>
	internal class PrefabAnimationTargets : MonoBehaviour
	{
		/// <summary>
		/// The list of animation target maps. Each map is a list of indices into the parent-first transform node expansions.
		/// </summary>
		[SerializeField]
		public int[][] AnimationTargets;

		/// <summary>
		/// Populate the animation target maps from a glTF document.
		/// </summary>
		/// <param name="root">The parsed glTF document this prefab was created from</param>
		/// <param name="sceneIndex">Which scene in the glTF this prefab is based on</param>
		internal void Initialize(GLTFRoot root, int sceneIndex)
		{
			if (root.Animations.Count == 0)
			{
				AnimationTargets = new int[0][];
				return;
			}

			// generate node index to tree index mapping
			var nodeTree = new List<int>(root.Nodes.Count);
			var scene = root.Scenes[sceneIndex];
			foreach (var node in scene.Nodes)
			{
				WalkNode(node, ref nodeTree);
			}

			// generate animation target to tree index mapping
			var anims = root.Animations.Where(anim => anim.Channels.Any(c => nodeTree.Contains(c.Target.Node.Id))).ToArray();
			AnimationTargets = new int[anims.Length][];
			for (var i = 0; i < anims.Length; i++)
			{
				AnimationTargets[i] = anims[i].Channels.Select(c => nodeTree.IndexOf(c.Target.Node.Id)).Distinct().ToArray();
			}

			// helper function to talk the gltf tree
			void WalkNode(NodeId node, ref List<int> tree)
			{
				tree.Add(node.Id);
				if (node.Value.Children == null) return;
				foreach (var child in node.Value.Children)
				{
					WalkNode(child, ref tree);
				}
			}
		}

		/// <summary>
		/// Compare the prefab transform hierarchy with the real transform hierarchy, and grab the real actor references.
		/// </summary>
		/// <param name="root">The instantiated prefab root transform.</param>
		/// <param name="animationIndex">The map index that you want the targets of.</param>
		/// <param name="addRootToTargets">Should the root actor be included in targets?</param>
		/// <returns></returns>
		internal List<Actor> GetTargets(Transform root, int animationIndex, bool addRootToTargets = false)
		{
			var treePositions = new Dictionary<int, Transform>(AnimationTargets[animationIndex].Max());
			int xfrmIndex = 0;
			WalkTree(root, true);

			var targets = AnimationTargets[animationIndex].Select(i => treePositions[i].GetComponent<Actor>()).ToList();
			if (addRootToTargets)
			{
				var rootActor = root.gameObject.GetComponent<Actor>();
				if (rootActor != null && !targets.Contains(rootActor))
				{
					targets.Add(rootActor);
				}
			}

			return targets;

			void WalkTree(Transform transform, bool skip = false)
			{
				if (!skip) treePositions[xfrmIndex++] = transform;
				for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
				{
					WalkTree(transform.GetChild(childIndex));
				}
			}
		}
	}
}
