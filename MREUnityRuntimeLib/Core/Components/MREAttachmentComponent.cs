// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Core.Components
{
	internal class MREAttachmentComponent : MonoBehaviour
	{
		public Guid UserId { get; set; }

		public Actor Actor { get; set; }
	}
}
