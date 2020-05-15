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
	public abstract class PatchPropertyCache<T>
	{
		private static PropertyInfo[] _patchProperties = null;
		static PatchPropertyCache()
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
	}
}
