using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    public GameObject rightLegJoint;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        if (Input.GetKeyDown(KeyCode.W))
        {
            if(Physics.Raycast(rightLegJoint.transform.position, -rightLegJoint.transform.up, out hit))
            {
                rightLegJoint.transform.localScale = new Vector3(rightLegJoint.transform.localScale.x, 
                    .5f * Vector3.Distance(rightLegJoint.transform.position, hit.point),
                    rightLegJoint.transform.localScale.z);
            }
        }
    }
}
