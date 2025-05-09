using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace playerSystem
{
    public class XRInputs : MonoBehaviour
    {
        InputDevice head, left, right;

        public GameObject XRrig;
        public Transform headTransform, leftTransform, rightTransform;
        public HandCollider leftHand, rightHand;

        public float swingSpeed = 1f;
        [Range(0, 1)]
        public float damping = 0.9f;
        public float gravity = 9.81f;

        Vector3 momentumVelocity = Vector3.zero;

        void Start()
        {
            InitializeInputDevices();
            //lastLeftHandPosition = head.TryGetFeatureValue(CommonUsages.devicePosition, out var leftPos) ? leftPos : Vector3.zero;
            //lastRightHandPosition = head.TryGetFeatureValue(CommonUsages.devicePosition, out var rightPos) ? rightPos : Vector3.zero;

            headTransform = XRrig.transform.GetChild(0).transform;
            leftTransform = XRrig.transform.GetChild(1).transform;
            rightTransform = XRrig.transform.GetChild(2).transform;
        }

        void FixedUpdate()
        {
            //Initialize devices if not found
            if (!head.isValid || !left.isValid || !right.isValid)
                InitializeInputDevices();

            //Read VR Inputs
            head.TryGetFeatureValue(CommonUsages.devicePosition, out var headPosition);
            head.TryGetFeatureValue(CommonUsages.deviceVelocity, out var headVelocity);
            head.TryGetFeatureValue(CommonUsages.deviceRotation, out var headRotation);
            left.TryGetFeatureValue(CommonUsages.devicePosition, out var leftPosition);
            left.TryGetFeatureValue(CommonUsages.deviceVelocity, out var leftVelocity);
            left.TryGetFeatureValue(CommonUsages.deviceRotation, out var leftRotation);
            right.TryGetFeatureValue(CommonUsages.devicePosition, out var rightPosition);
            right.TryGetFeatureValue(CommonUsages.deviceVelocity, out var rightVelocity);
            right.TryGetFeatureValue(CommonUsages.deviceRotation, out var rightRotation);
            left.TryGetFeatureValue(CommonUsages.grip, out var leftGrip);
            right.TryGetFeatureValue(CommonUsages.grip, out var rightGrip);

            //Move body parts before rig
            //headTransform.localPosition = headPosition;
            leftTransform.localPosition = leftPosition;
            rightTransform.localPosition = rightPosition;

            //headTransform.localRotation = Quaternion.Inverse(XRrig.transform.rotation) * headRotation;

            //Apply Inputs as movement
            var movementDelta = Vector3.zero;
            int gripCount = 0;

            //Grippers
            if (leftGrip > 0.5f) leftHand.Grab();
            else leftHand.Release();

            if (rightGrip > 0.5f) rightHand.Grab();
            else rightHand.Release();

            if (leftHand.GrabbingState())
            {
                movementDelta += leftVelocity * Time.fixedDeltaTime;
                gripCount++;
            }

            if (rightHand.GrabbingState())
            {
                movementDelta += rightVelocity * Time.fixedDeltaTime;
                gripCount++;
            }

            if (gripCount > 0)
            {
                //Reverse force because we want a pulling action
                movementDelta /= gripCount;
                momentumVelocity -= movementDelta * swingSpeed;
            }

            //Gravity and friction forces
            if (leftHand.FallingState() && rightHand.FallingState() && !(leftHand.GrabbingState() || rightHand.GrabbingState()))
                momentumVelocity += gravity * Time.fixedDeltaTime * Vector3.down;

            //Debug.Log($"left : {leftHand.FallingState()}, right : {rightHand.FallingState()}");

            //Check collision 2 frames ahead to prevent failing a collision check ---------Dont know how to find world scale so hard-coded for now------------
            Vector3 predictedOffset = momentumVelocity * Time.fixedDeltaTime;
            if ((Physics.CheckSphere(leftTransform.position + predictedOffset, 0.1f)) || Physics.CheckSphere(rightTransform.position + predictedOffset, 0.1f))
            {
                momentumVelocity *= damping;
            }

            momentumVelocity *= damping;

            if (momentumVelocity.magnitude > 0.01f)
                XRrig.transform.position += momentumVelocity * Time.fixedDeltaTime;
            
        }

        void LateUpdate()
        {
            //This avoids flickering becuase of a slower framerate in FixedUpdate
            head.TryGetFeatureValue(CommonUsages.devicePosition, out var headPosition);
            head.TryGetFeatureValue(CommonUsages.deviceRotation, out var headRotation);

            headTransform.localPosition = headPosition;
            headTransform.localRotation = headRotation;
        }

        void InitializeInputDevices()
        {
            head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            Debug.Log($"Head : {head.isValid}, left : {left.isValid}, right : {right.isValid}");
        }
    }
}