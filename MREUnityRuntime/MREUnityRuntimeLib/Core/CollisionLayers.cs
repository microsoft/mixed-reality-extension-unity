using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Core
{
	public enum CollisionLayer
	{
		Default,
		Navigation,
		Hologram,
		UI
	}

	public struct EngineCollisionLayers
	{
		public byte Default;
		public byte Navigation;
		public byte Hologram;
		public byte UI;

		public EngineCollisionLayers(byte Default, byte Navigation, byte Hologram, byte UI)
		{
			this.Default = Default;
			this.Navigation = Navigation;
			this.Hologram = Hologram;
			this.UI = UI;
		}
	}
}
