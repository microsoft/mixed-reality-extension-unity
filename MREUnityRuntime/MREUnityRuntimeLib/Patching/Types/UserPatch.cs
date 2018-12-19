// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using System;

namespace MixedRealityExtension.Patching.Types
{
    public class UserPatch : MixedRealityExtensionObjectPatch
    {
        public UserPatch()
        {

        }

        internal UserPatch(Guid id)
            : base(id)
        {

        }

        internal UserPatch(User user)
            : base(user.Id)
        {
            Name = user.Name;
            Transform = PatchingUtilMethods.GeneratePatch((MWTransform)null, user.transform);
        }
    }
}
