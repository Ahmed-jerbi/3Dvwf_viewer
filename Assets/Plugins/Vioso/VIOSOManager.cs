using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VIOSOManager : MonoBehaviour
{
    private GameObject[] viosoCams;
    // Start is called before the first frame update
    void Start()
    {
        viosoCams = GameObject.FindGameObjectsWithTag("camera");
        foreach (GameObject obj in viosoCams) { obj.GetComponent<VIOSOCamera>().enabled = true; }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
