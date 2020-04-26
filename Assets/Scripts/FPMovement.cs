using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPMovement : MonoBehaviour
{
    public float lookSensitivity;
    public float moveAccel;
    public float maxMoveSpeed;
    Rigidbody rb;

    Vector3 velocity;
    public Vector3 viewRotation;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        velocity = new Vector3();
        viewRotation = transform.rotation.eulerAngles;
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //rotation
        viewRotation += new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0) * lookSensitivity * Time.fixedDeltaTime;
        viewRotation.x = Mathf.Clamp(viewRotation.x, -90, 90);

        Camera.main.transform.localRotation = Quaternion.Euler(viewRotation.x, 0, 0);
        rb.MoveRotation(Quaternion.Euler(0, viewRotation.y, 0));
        RaycastHit hit;
        

        //movement
        Vector3 potentialPos = transform.position;
        Vector3 acceleration = (transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal")).normalized;
        if (acceleration != Vector3.zero)
        {
            rb.useGravity = true;
            velocity += acceleration * moveAccel * Time.fixedDeltaTime;

            velocity = Vector3.ClampMagnitude(velocity, maxMoveSpeed);
        }
        else
        {
            velocity += new Vector3(-velocity.x, 0, -velocity.z) * moveAccel * Time.fixedDeltaTime;
            if (velocity.x < .001f) velocity.x = 0;
            if (velocity.z < .001f) velocity.z = 0;
        }


        rb.MovePosition(potentialPos + velocity * Time.deltaTime);

    }

    private void FixedUpdate()
    {
       
    }
}
