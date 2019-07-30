using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class TransformUtil
{

	/// <summary>
	/// Some components need to be made invisible but maintain their position and stay Active. 
	/// On such occassions we shrink the component down to microsopic size so it can't be seen. 
	/// The WebBrowser does this for example, in order to keep its WebView alive and happy.
	/// </summary>
	public static readonly Vector3 TinyScale = new Vector3(0.0001f, 0.0001f, 0.0001f);

	public static void ResetPositionAndRotation(this Transform t)
	{
		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;
	}

	public static void ResetPosition(this Transform t)
	{
		t.localPosition = Vector3.zero;
	}

	/// <summary>
	/// Resets position, rotation and scale to defaults
	/// </summary>
	public static void ResetPosRotScale(this Transform t, float scaleFactor = 1f)
	{
		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;
		t.localScale = Vector3.one * scaleFactor;
	}

    /*
	/// <summary>
	/// A helper method that makes it easy to position something like a dialog in the path of the user's gaze.
	/// the object's Y rotation will match center-eye so it will be billboarded correctly. 
	/// </summary>
	public static void PositionForwardOfCenterEye(this Transform target, Vector3? localOffset = null,
		Quaternion? localRotation = null)
	{
		Transform centerEye = Main.HMDManager.CenterEyeTransform;

		target.rotation = Quaternion.Euler(0, centerEye.rotation.eulerAngles.y, 0);
		target.position = centerEye.position + target.TransformDirection(localOffset ?? new Vector3(0, 0, 1.4f));

		if (localRotation.HasValue) target.rotation = target.TransformRotation(localRotation.Value);
	}
    */

	public static T GetComponentInSelfOrAncestors<T>(this Transform t) where T : Component
	{
		do
		{
			if (t.GetComponent<T>())
			{
				return t.GetComponent<T>();
			}
			t = t.parent;
		} while (t != null);

		return null;
	}

	public static T[] GetComponentsInImmediateChildren<T>(this Transform transform) where T : Component
	{
		var list = new List<T>();
		foreach (Transform childTransform in transform)
		{
			var component = childTransform.GetComponent<T>();
			if (component != null) list.Add(component);
		}
		return list.ToArray();
	}

	public static Quaternion TransformRotation(this Transform t, Quaternion localRotation)
	{
		if (t == null) throw new System.ArgumentNullException("transform");
		return t.rotation*localRotation;
	}

	public static Quaternion InverseTransformRotation(this Transform t, Quaternion worldRotation)
	{
		if (t == null) throw new System.ArgumentNullException("transform");
		return Quaternion.Inverse(t.rotation)*worldRotation;
	}

	public static void TransformSoThatChildIsAtParentOrigin(this Transform t, Transform child) //TODO: Test rotation
	{
		if (!child.IsChildOf(t)) throw new System.ArgumentException("Not a child");
		if (t.parent == null) throw new System.ArgumentException("Does not have a parent");
		var positionOffset = t.parent.position - child.position;
		var rotationOffset = Quaternion.Inverse(t.parent.rotation)*child.rotation;

		t.localPosition = positionOffset;
		t.localRotation = rotationOffset;
	}

	public static string GetFullPath(this Transform t)
	{
		string path = t.name;
		Transform currentNode = t;

		while (currentNode.parent)
		{
			currentNode = currentNode.parent;
			path = currentNode.name + "/" + path;
		}
		return path;
	}

	public static T GetComponentInDescendents<T>(this Transform t)
	{
		List<Transform> transformStack = new List<Transform>();
		transformStack.Add(t);
		Transform currentTransform = t;
		T component = t.GetComponent<T>();

		while (transformStack.Count > 0)
		{
			currentTransform = transformStack[0];
			Debug.Log("   checking " + currentTransform.GetFullPath());
			transformStack.RemoveAt(0);
			component = currentTransform.GetComponent<T>();
			if (component != null)
			{
				return component;
			}

			for (int i = 0; i < currentTransform.childCount; i++)
			{
				transformStack.Add(currentTransform.GetChild(i));
			}
		}
		return component;
	}

	/// <summary>
	/// depth of the given transform in the heirarchy, where root transforms have a depth of zero
	/// </summary>
	public static int DepthInHeirarchy(this Transform transform)
	{
		int depth = 0;

		while (transform.parent != null)
		{
			transform = transform.parent;
			depth++;
		}

		return depth;
	}

	public static T GetOrAddComponent<T>(this GameObject obj) where T : UnityEngine.Component
	{
		T component = obj.GetComponent<T>();

		//it's very important here to not use the ?? operator, since it doesn't work properly with Unity Components.
		return (component) ? component : obj.AddComponent<T>();
	}

	public static T GetOrAddComponent<T>(this UnityEngine.Component obj) where T : UnityEngine.Component
	{
		return GetOrAddComponent<T>(obj.gameObject);
	}
}
