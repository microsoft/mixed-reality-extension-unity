using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MixedRealityExtension.Core;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using UnityEngine;

namespace MixedRealityExtension.Animation
{
	internal class TargetPath
	{
		private struct Accessor<T>
		{
			public Func<T, object> Get;
			public Action<T, object> Set;
			public Accessor(Func<T, object> get, Action<T, object> set)
			{
				Get = get;
				Set = set;
			}
		}

		private static Dictionary<string, Accessor<Actor>> ActorAccessors = new Dictionary<string, Accessor<Actor>>()
		{
			{"transform/local/position", new Accessor<Actor>(
				(Actor actor) => new Vector3Patch(actor.LocalTransform.Position),
				(Actor actor, object patch) => actor.ApplyPatch(new ActorPatch() { Transform = new ActorTransformPatch() { Local = new ScaledTransformPatch() { Position = patch as Vector3Patch } } })
			)}
		};

		private static Regex PathRegex = new Regex("^(?<type>actor|animation|material):(?<placeholder>[^/]+)/(?<path>.+)$");

		public string PathString { get; }

		public TargetPath(string pathString)
		{
			PathString = pathString;
		}
	}
}
