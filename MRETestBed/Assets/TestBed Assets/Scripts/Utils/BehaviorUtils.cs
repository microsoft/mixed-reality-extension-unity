using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

internal static class BehaviorUtils
{
    public static T EnsureComponent<T>(this Behaviour caller)
    {
        var component = caller.GetComponent<T>();
        if (component == null)
        {
            caller.enabled = false;
            throw new MissingComponentException(string.Format("Object of type {0} must have a component of type {1} attached",
                caller.GetType(), typeof(T)));
        }
        return component;
    }
}
