using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Patching.Types
{
    public class ColliderPatch : IPatchable
    {
        [PatchProperty]
        public bool? IsEnabled { get; set; }

        [PatchProperty]
        public bool? IsTrigger { get; set; }

        //[PatchProperty]
        //public CollisionLayer? CollisionLayer { get; set; }

        [PatchProperty]
        public ColliderGeometry ColliderGeometry { get; set; }
    }
}
