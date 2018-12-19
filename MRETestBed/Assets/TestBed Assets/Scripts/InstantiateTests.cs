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
            var gameObject = GameObject.Instantiate(functionalTestPrefab, transform.parent);
            gameObject.name = testName;
            gameObject.transform.Find("Label").GetComponent<TextMesh>().text = testName;
            gameObject.GetComponent<MREComponent>().SessionID = testName;
            gameObject.GetComponent<MREComponent>().MREURL = MREURL + "?test=" + testName;
            gameObject.GetComponent<MREComponent>().AppID = testName;
            gameObject.GetComponent<MREComponent>().UserGameObject = transform.parent.Find("FPSControllerWithCursor").gameObject;

            //Fan out the tests in a half-circle, scaled based on how many apps there are
            float index = (((float)i) / (testNames.Length - 1)) - 0.5f;
            float scale = 11.0f* Mathf.Sqrt(testNames.Length);
            gameObject.transform.localPosition = new Vector3(Mathf.Sin(3.1415f*index), 0.0f, Mathf.Cos(3.1415f * index)-0.3f)*scale;
            gameObject.transform.localEulerAngles= new Vector3(0.0f, index * 180.0f, 0.0f);


        }
    }
    
}
