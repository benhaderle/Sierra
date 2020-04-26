using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

public class SkiMovement : MonoBehaviour
{
    [Header("Body Transforms")]
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftSki;
    public Transform rightSki;
    public Transform neck;
    public Transform pole;
    public Transform leftHand;
    public Transform leftArm;
    public Transform rightHand;
    public Transform rightArm;

    [Header("General Variables")]
    public AnimationCurve frictionCurve;
    public float maxBodySpeed = 60;
    public float bodyAcceleration = 50;
    public float bodyTurningSpeed = 30;
    private Vector3 velocity;

    [Header("Scissor Variables")]
    public AnimationCurve scissorCurve;
    public AnimationCurve bodySpeedCurve;
    public float scissorSpeed = 3;
    private float tScissor;

    [Header("Foot Weight Variables")]
    public AnimationCurve weightCurve;
    public float weightSpeed;
    public float weightCurveEffect;
    private float tLeftWeight;
    private float tRightWeight;

    [Header("Turning Variables")]
    public AnimationCurve turningCurve;
    public float maxTurnRotation;
    public float skiTurnSpeed = 1;
    private float tTurn;

    private float comOffset_y = 5.5f;

    RaycastHit hit;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        InputDevice device = InputManager.ActiveDevice;

        //foot weight
        //translating inputs onto the curve
        //left curve
        tLeftWeight += weightSpeed * device.RightTrigger.Value * Time.deltaTime;
        if (device.RightTrigger.Value == 0) tLeftWeight = GoToZero(tLeftWeight, weightSpeed * Time.deltaTime, .05f);
        tLeftWeight = Mathf.Clamp(tLeftWeight, 0, device.RightTrigger.Value);
        float leftWeightCurve = weightCurve.Evaluate(tLeftWeight);
        //right curve
        tRightWeight += weightSpeed * device.LeftTrigger.Value * Time.deltaTime;
        if (device.LeftTrigger.Value == 0) tRightWeight = GoToZero(tRightWeight, weightSpeed * Time.deltaTime, .05f);
        tRightWeight = Mathf.Clamp(tRightWeight, 0, device.LeftTrigger.Value);
        float rightWeightCurve = weightCurve.Evaluate(tRightWeight);
        //moving left foot
        leftLeg.localPosition = new Vector3(-.5f, -1.36f, 0) + Vector3.up * leftWeightCurve;
        //moving right foot
        rightLeg.localPosition = new Vector3(.5f, -1.36f, 0) + Vector3.up * rightWeightCurve;

        leftWeightCurve *= weightCurveEffect;
        rightWeightCurve *= weightCurveEffect;

        //scissoring
        //translating inputs onto the curve
        tScissor += scissorSpeed * device.LeftStick.Y * Time.deltaTime;
        if (Mathf.Abs(device.LeftStick.Y) <= 0.05f) tScissor = GoToZero(tScissor, scissorSpeed * Time.deltaTime, .05f);
        else tScissor = Mathf.Clamp(tScissor, -Mathf.Abs(device.LeftStick.Y), Mathf.Abs(device.LeftStick.Y));
        float leftScissorCurve = scissorCurve.Evaluate(tScissor);
        float rightScissorCurve = scissorCurve.Evaluate(-tScissor);

        //turning
        tTurn += skiTurnSpeed * device.LeftStick.X * Time.deltaTime;
        if (Mathf.Abs(device.LeftStick.X) < 0.5f) tTurn = GoToZero(tTurn, skiTurnSpeed * Time.deltaTime, .05f);
        else tTurn = Mathf.Clamp(tTurn, -Mathf.Abs(device.LeftStick.X), Mathf.Abs(device.LeftStick.X));
        float desiredTurnAngle = turningCurve.Evaluate(tTurn) * maxTurnRotation;
        Debug.Log(tTurn);

        //straightening skis, turning neck + pole
        leftLeg.localRotation = Quaternion.Euler(30 * leftScissorCurve, desiredTurnAngle, 0);
        rightLeg.localRotation = Quaternion.Euler(30 * rightScissorCurve, desiredTurnAngle, 0);
        neck.localRotation = Quaternion.Euler(0, desiredTurnAngle * 1.5f, 0);
        pole.localRotation = Quaternion.Euler(0, 30 * leftScissorCurve, -desiredTurnAngle + (rightWeightCurve - leftWeightCurve) * 2);
        //deal with arms
        leftArm.rotation = Quaternion.LookRotation((leftHand.position - leftArm.position).normalized);
        rightArm.rotation = Quaternion.LookRotation((rightHand.position - rightArm.position).normalized);
        leftArm.localScale = new Vector3(1, 1, (leftHand.position - leftArm.position).magnitude);
        rightArm.localScale = new Vector3(1, 1, (rightHand.position - rightArm.position).magnitude);

        Physics.Raycast(leftLeg.position, -leftLeg.up, out hit, 10, 1 << 9);
        leftSki.position = hit.point + hit.normal * .08f;
        leftSki.rotation = Quaternion.LookRotation((new Vector3(leftLeg.forward.x, 0, leftLeg.forward.z)).normalized, hit.normal); 
        Physics.Raycast(rightLeg.position, -rightLeg.up, out hit, 10, 1 << 9);
        rightSki.position = hit.point + hit.normal * .08f;
        rightSki.rotation = Quaternion.LookRotation((new Vector3(rightLeg.forward.x, 0, rightLeg.forward.z)).normalized, hit.normal);

        //body movement
        float vel021 = velocity.magnitude / maxBodySpeed;
        if (Mathf.Abs(tScissor) < Mathf.Abs(device.LeftStick.Y))
        {
            velocity += ((leftScissorCurve * (1 + leftWeightCurve) + rightScissorCurve * (1 + rightWeightCurve)) / .8f)
                * bodySpeedCurve.Evaluate(vel021) * bodyAcceleration
                * transform.forward.normalized * Time.deltaTime;
        }
        else if (velocity.magnitude < .01f)
        {
            velocity = Vector3.zero;
        }
        //turning
        if (desiredTurnAngle != 0)
        {
            Vector3 skiForward = (leftSki.transform.forward + rightSki.transform.forward).normalized;
            if (velocity.magnitude > .5f)
            {
                float velSkiDot = Vector3.Dot(skiForward, velocity);
                float weightFactor = Mathf.Abs(1 + (leftWeightCurve * tTurn) + (rightWeightCurve * tTurn));
                Debug.Log(weightFactor);
                velocity = Vector3.RotateTowards(velocity,
                    skiForward * velocity.magnitude * Mathf.Sign(velSkiDot),
                    bodyTurningSpeed * weightFactor * vel021 * Mathf.Deg2Rad * Time.deltaTime, velocity.magnitude);
                transform.rotation = Quaternion.LookRotation((Mathf.Sign(velSkiDot) >= 0 ? velocity.normalized : -velocity.normalized), hit.normal);
            }
            else
            {
                transform.forward = Vector3.RotateTowards(transform.forward, skiForward, bodyTurningSpeed * .6f * Mathf.Deg2Rad * Time.deltaTime,
                    transform.forward.magnitude) ;
            }
            
        }
        //friction
        velocity += -velocity.normalized * frictionCurve.Evaluate(vel021) * bodyAcceleration * Time.deltaTime;
        transform.position = transform.position + velocity * Time.deltaTime;

        //deal with camera
        ThirdPersonCamera cam = Camera.main.GetComponent<ThirdPersonCamera>();
        cam.transform.localPosition = Vector3.Lerp(cam.slowLocalPos, cam.fastLocalPos, cam.speedCurve.Evaluate(vel021));
        cam.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(cam.slowLocalRot), Quaternion.Euler(cam.fastLocalRot), cam.speedCurve.Evaluate(vel021));
    }

    public float GoToZero(float f, float decel, float bounds)
    {
        if(Mathf.Abs(f) <= bounds) return 0;

        if (f > bounds) f -= decel;
        else f += decel;

        return f;
    }

    public Vector3 GoToZero(Vector3 f, float decel, float bounds)
    {
        if (f.x > bounds) f.x -= decel;
        else f.x += decel;

        if (Mathf.Abs(f.x) <= bounds) f.x = 0;
        if (Mathf.Abs(f.y) <= bounds) f.y = 0;
        if (Mathf.Abs(f.z) <= bounds) f.z = 0;

        

        return f;
    }
}
