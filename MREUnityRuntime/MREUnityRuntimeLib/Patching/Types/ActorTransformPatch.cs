// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class ActorTransformPatch : Patchable<ActorTransformPatch>
	{
		private TransformPatch app;
		private TransformPatch savedApp;
		[PatchProperty]
		public TransformPatch App
		{
			get => app;
			set
			{
				if (value == null && app != null)
				{
					savedApp = app;
					savedApp.Clear();
				}
				app = value;
			}
		}

		private ScaledTransformPatch local;
		private ScaledTransformPatch savedLocal;
		[PatchProperty]
		public ScaledTransformPatch Local
		{
			get => local;
			set
			{
				if (value == null && local != null)
				{
					savedLocal = local;
					savedLocal.Clear();
				}
				local = value;
			}
		}

		public override void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// actor transforms are not directly patchable, do nothing
			}
			else if (path.PathParts[depth] == "local")
			{
				if (local == null)
				{
					if (savedLocal == null)
					{
						savedLocal = new ScaledTransformPatch();
					}
					local = savedLocal;
				}
				Local.WriteToPath(path, value, depth + 1);
			}
			else if (path.PathParts[depth] == "app")
			{
				if (app == null)
				{
					if (savedApp == null)
					{
						savedApp = new TransformPatch();
					}
					app = savedApp;
				}
				App.WriteToPath(path, value, depth + 1);
			}
			// else
			// an unrecognized path, do nothing
		}

		public override bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			if (path.PathParts[depth] == "local")
			{
				return Local?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			else if (path.PathParts[depth] == "app")
			{
				return App?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			return false;
		}

		public override void Clear()
		{
			App = null;
			Local = null;
		}

		public override void Restore(TargetPath path, int depth)
		{
			if (depth >= path.PathParts.Length) return;

			switch (path.PathParts[depth])
			{
				case "local":
					Local = savedLocal ?? new ScaledTransformPatch();
					Local.Restore(path, depth + 1);
					break;
				case "app":
					App = savedApp ?? new TransformPatch();
					App.Restore(path, depth + 1);
					break;
			}
		}

		public override void RestoreAll()
		{
			Local = savedLocal ?? new ScaledTransformPatch();
			Local.RestoreAll();
			App = savedApp ?? new TransformPatch();
			App.RestoreAll();
		}
	}
}
