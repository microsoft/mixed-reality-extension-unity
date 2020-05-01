using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MixedRealityExtension.Patching
{
	/// <summary>
	/// Statically caches properties for patchables. This increases IsPatched performance from 200+us
	/// per transform patch to 30-60us and reduced chess' initial patching frame time from 150ms to
	/// 20ms on a high end CPU. Note we had to duplicate some code in the ScaledTransform due to a lack
	/// of multiple inheritance or mixins in C#.
	/// </summary>
	public class Patchable<T>
	{
		private static PropertyInfo[] _patchableProperties = null;
		static Patchable()
		{
			List<PropertyInfo> patchableProperties = new List<PropertyInfo>();
			PropertyInfo[] properties = typeof(T).GetProperties();
			for (int i = 0; i < properties.Length; ++i)
			{
				if (properties[i].GetCustomAttributes(false).Any(attr => attr is PatchProperty))
				{
					patchableProperties.Add(properties[i]);
				}
			}
			_patchableProperties = patchableProperties.ToArray();
		}

		public PropertyInfo[] GetPatchableProperties()
		{
			return _patchableProperties;
		}
	}
}
