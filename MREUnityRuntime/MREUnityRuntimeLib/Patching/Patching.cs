// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Reflection;

namespace MixedRealityExtension.Patching
{
    internal static class PatchingUtils
    {
        // TODO @tombu - Look in to making this an extension method.
        public static bool IsPatched<T>(T patch) where T : IPatchable
        {
            var properties = patch.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.GetCustomAttributes(false).Any(attr => attr is PatchProperty))
                {
                    var val = property.GetValue(patch);
                    if (val is IPatchable)
                    {
                        if (IsPatched(val as IPatchable))
                        {
                            return true;
                        }
                    }
                    else if (val != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class PatchProperty : Attribute
    {
        public PatchProperty()
        {

        }
    }
}
