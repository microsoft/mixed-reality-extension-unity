using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Patching.Types
{
	public class LookAtPatch : IPatchable
	{
		[PatchProperty]
		public Guid? ActorId { get; set; }

		[PatchProperty]
		public LookAtMode? Mode { get; set; }

		[PatchProperty]
		public bool? Backward { get; set; }
	}
}
