using System;

namespace MixedRealityExtension.Patching
{
	[AttributeUsage(AttributeTargets.Property)]
	internal class PatchProperty : Attribute
	{
		public PatchProperty()
		{
		}
	}
}
