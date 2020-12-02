// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Util
{
	// This is a quick hack, and does not generate valid UUIDs.
	// To generate a deterministic sequence of values that are also valid
	// UUIDs, we must emplement the "Name-based UUID" method described in
	// RFC 4122 §4.3 (http://www.ietf.org/rfc/rfc4122.txt).
	internal class DeterministicGuids
	{
		private Guid seed;

		public DeterministicGuids(Guid? seed)
		{
			if (seed.HasValue && seed.Value != Guid.Empty)
			{
				this.seed = seed.Value;
			}
			else
			{
				this.seed = Guid.NewGuid();
			}
		}

		public Guid Next()
		{
			var result = this.seed;
			this.seed = UtilMethods.StringToGuid(this.seed.ToString());
			return result;
		}
	}
}
