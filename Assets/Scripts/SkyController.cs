using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyController : MonoBehaviour
{

    public Material skyBox;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 d = (-transform.forward).normalized;
        skyBox.SetVector("_SunPos", new Vector4(d.x, d.y, d.z, 0));
    }
}
