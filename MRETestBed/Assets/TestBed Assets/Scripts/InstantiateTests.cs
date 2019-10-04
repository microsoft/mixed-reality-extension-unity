// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateTests : MonoBehaviour
{
	public GameObject functionalTestPrefab;
	public string MREURL;
	// Use this for initialization
	public string[] testNames;
	void Start()
	{
		for(var i=0; i<testNames.Length;i++)
		{
			var testName= testNames[i];

			//Instantiate the test prefab and set parameters
			var test = GameObject.Instantiate(functionalTestPrefab, transform.parent);
			test.name = testName;
			test.transform.Find("Label").GetComponent<TextMesh>().text = testName;
			var mres = test.GetComponentsInChildren<MREComponent>();
			foreach(var mre in mres)
			{
				mre.SessionId = testName;
				mre.MreUrl = $"{MREURL}?test={testName}&autorun=true&nomenu=true";
				mre.AppId = testName;
				mre.UserGameObject = transform.parent.Find("FPSControllerWithCursor").gameObject;
			}

			//Fan out the tests in a three-quarters circle, scaled based on how many apps there are

			float threeQuarters = Mathf.PI * 1.5f;
			// from -0.5 to 0.5
			float index = (((float) i) / (testNames.Length - 1)) - 0.5f;
			// find the radius of a three-quarters circle with n 12 meter tests
			float radius = Mathf.Max(12, 12 * testNames.Length / threeQuarters);
			test.transform.localPosition = radius * new Vector3(
				Mathf.Sin(threeQuarters * index),
				0.0f,
				Mathf.Cos(threeQuarters * index)
			);
			test.transform.localRotation = Quaternion.LookRotation(test.transform.localPosition, Vector3.up);
		}
	}
	
}
