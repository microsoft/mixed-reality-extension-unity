// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MixedRealityExtension.Patching
{
	/// <summary>
	/// Statically caches reflection info for PatchProperty-tagged properties.
	/// Note it was necessary to duplicate some code in the ScaledTransform due
	/// to a lack of multiple inheritance or mixins in C#.
	/// </summary>
	public abstract class Patchable<T> : IPatchable
	{
		private static PropertyInfo[] _patchProperties = null;
		static Patchable()
		{
			List<PropertyInfo> patchProperties = new List<PropertyInfo>();
			PropertyInfo[] properties = typeof(T).GetProperties();
			for (int i = 0; i < properties.Length; ++i)
			{
				if (properties[i].GetCustomAttributes(false).Any(attr => attr is PatchProperty))
				{
					patchProperties.Add(properties[i]);
				}
			}
			_patchProperties = patchProperties.ToArray();
		}

		public PropertyInfo[] GetPatchProperties()
		{
			return _patchProperties;
		}

		/// <inheritdoc/>
		public bool IsPatched()
		{
			foreach (PropertyInfo property in GetPatchProperties())
			{
				var val = property.GetValue(this);
				if (val is IPatchable childPatch)
				{
					return childPatch.IsPatched();
				}
				else if (val != null)
				{
					return true;
				}
			}

			return false;
		}

		/// <inheritdoc/>
		public virtual void WriteToPath(TargetPath path, JToken value, int depth)
		{

		}

		///<inheritdoc/>
		public virtual bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			return false;
		}

		///<inheritdoc/>
		public virtual void Clear()
		{

		}

		///<inheritdoc/>
		public virtual void Restore(TargetPath path, int depth)
		{

		}

		///<inheritdoc/>
		public virtual void RestoreAll()
		{

		}
	}
}
