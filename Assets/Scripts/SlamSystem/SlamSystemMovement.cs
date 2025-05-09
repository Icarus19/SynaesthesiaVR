using CustomPhysics;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class SlamSystemMovement : MonoBehaviour
{
    //I'll write it down before I forget.
    //Instead of using colliders and trying to create a realistic system Lets go ham and just wave our arms
    //Idea is that the player is a rigidbody that moves with physics, then the player, instead of pushing of collider, will just wave their arms frantically to cause enough velocity and fly away, this might make them able to fly but who cares anymore.
    //Sonic boom punches and jedi double jumps
    CustomInputDevice headDevice, leftHandDevice, rightHandDevice;
    Transform offsetTransform, headTransform, leftHandTransform, rightHandTransform;
    Rigidbody playerRigidbody;

    public float strength = 0.3f, torqueStrength = 0.3f;
    public LayerMask excludeLayer;

    bool grounded = false;

    void Start()
    {
        playerRigidbody = transform.GetComponent<Rigidbody>();   
        InitializeInputDevices();
        InitializeTransforms();
    }
    void FixedUpdate()
    {
        if (!headDevice.IsValid() || !leftHandDevice.IsValid() || !rightHandDevice.IsValid())
            InitializeInputDevices();


        Vector3 leftVelocity = leftHandDevice.GetVelocity();
        Vector3 rightVelocity = rightHandDevice.GetVelocity();

        grounded = (Physics.CheckCapsule(transform.position + Vector3.up, transform.position + Vector3.down, transform.localScale.x, ~excludeLayer));
        

        if(grounded)
        {
            Debug.Log($"leftVelocity : {leftVelocity}, rightVelocity : {rightVelocity}");
            if (leftVelocity.magnitude > 1)
                ApplyForce(-leftVelocity * strength);

            if (rightVelocity.magnitude > 1)
                ApplyForce(-rightVelocity * strength);

            var torque = Mathf.Abs(leftVelocity.magnitude - rightVelocity.magnitude);
            if (torque > 1)
                playerRigidbody.AddTorque(Vector3.up * torque * torqueStrength);
        }

        leftHandTransform.localPosition = leftHandDevice.GetPosition();
        rightHandTransform.localPosition = rightHandDevice.GetPosition();
    }

    void LateUpdate()
    {
        headTransform.localPosition = headDevice.GetPosition();
        headTransform.localRotation = headDevice.GetRotation();
    }
    void InitializeInputDevices()
    {
        headDevice = new CustomInputDevice(XRNode.Head);
        leftHandDevice = new CustomInputDevice(XRNode.LeftHand);
        rightHandDevice = new CustomInputDevice(XRNode.RightHand);

        Debug.Log($"Head : {headDevice.IsValid()}, left : {leftHandDevice.IsValid()}, right : {rightHandDevice.IsValid()}");
    }
    void InitializeTransforms()
    {
        offsetTransform = transform.GetChild(0);
        headTransform = offsetTransform.GetChild(0);
        leftHandTransform = offsetTransform.GetChild(1);
        rightHandTransform = offsetTransform.GetChild(2);
    }

    void ApplyForce(Vector3 force)
    {
        playerRigidbody.AddForce(force, ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + Vector3.up, transform.localScale.x);
        Gizmos.DrawWireSphere(transform.position + Vector3.down, transform.localScale.x);
    }
}
