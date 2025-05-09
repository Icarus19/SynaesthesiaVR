using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;

namespace CustomPhysics
{
    //This script is so messy. There's no point in having a Rigidbody for the rig is there?
    public class PlayerPhysics : MonoBehaviour
    {
        Transform headTransform, leftHandTransform, rightHandTransform;
        Rigidbody rigRB, headRB, leftHandRB, rightHandRB;
        HandCollider leftCollider, rightCollider;
        CustomInputDevice headDevice, leftHandDevice, rightHandDevice;

        Vector3 colliderForce = Vector3.zero;
        Vector3 momentumVelocity= Vector3.zero;
        Vector3 predictedPosition = Vector3.zero;

        public float swingSpeed = 100f;
        public float gravity = 9.81f;
        public float damping = 0.9f;

        void Start()
        {
            InitializeInputDevices();
            InitializeTransforms();
            InitializeRigibodies();
            InitializeColliders();
        }

        private void FixedUpdate()
        {
            if (!headDevice.IsValid() || !leftHandDevice.IsValid() || !rightHandDevice.IsValid())
                InitializeInputDevices();

            //Apply Inputs as movement
            var movementDelta = Vector3.zero;
            int gripCount = 0;

            if((leftHandDevice.GetGrip() && leftCollider.isColliding))
            {
                movementDelta -= leftHandDevice.GetVelocity() * Time.fixedDeltaTime;
                gripCount++;
            }

            if ((rightHandDevice.GetGrip() && rightCollider.isColliding))
            {
                movementDelta -= rightHandDevice.GetVelocity() * Time.fixedDeltaTime;
                gripCount++;
            }

            if (gripCount > 0)
            {
                //Reverse force because we want a pulling action
                movementDelta /= gripCount;
                momentumVelocity += movementDelta * swingSpeed;
            }
            else if(!(leftCollider.grounded || rightCollider.grounded))
            {
                momentumVelocity += gravity * Time.fixedDeltaTime * Vector3.down;
            }

            Debug.Log($"left : {leftCollider.isColliding}, right : {rightCollider.isColliding}");
            momentumVelocity *= damping;

            if (!leftCollider.PredictMove(leftHandDevice.GetPosition() + predictedPosition) || !rightCollider.PredictMove(rightHandDevice.GetPosition() + predictedPosition))
                momentumVelocity = -momentumVelocity;

            predictedPosition = transform.position + momentumVelocity * Time.fixedDeltaTime;
            MoveRigidbodies();
        }

        void MoveRigidbodies()
        {
            rigRB.MovePosition(predictedPosition);

            //Move head
            headRB.MovePosition(headDevice.GetPosition() + predictedPosition);
            headRB.MoveRotation(headDevice.GetRotation());

            //Move hands
            leftHandRB.MovePosition(leftHandDevice.GetPosition() + predictedPosition);
            rightHandRB.MovePosition(rightHandDevice.GetPosition() + predictedPosition);
        }
        void MoveSafeRigidBodies()
        {

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
            headTransform = transform.GetChild(0);
            leftHandTransform = transform.GetChild(1);
            rightHandTransform = transform.GetChild(2);

            Debug.Log($"Head : {headTransform.name}, left : {leftHandTransform.name}, right : {rightHandTransform.name}");
        }
        void InitializeRigibodies()
        {
            rigRB = transform.GetComponent<Rigidbody>();
            headRB = transform.GetChild(0).GetComponent<Rigidbody>();
            leftHandRB = transform.GetChild(1).GetComponent<Rigidbody>();
            rightHandRB = transform.GetChild(2).GetComponent<Rigidbody>();
        }
        void InitializeColliders()
        {
            leftCollider = leftHandTransform.GetComponent<HandCollider>();
            rightCollider = rightHandTransform.GetComponent<HandCollider>();
        }
    }

    
}