using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Patching
{
    public abstract class Patch : IPatchable
    {
        public abstract bool ShouldSerialize();
    }
}
