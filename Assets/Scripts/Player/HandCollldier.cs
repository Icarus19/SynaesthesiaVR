using UnityEngine;

namespace playerSystem
{
    public class HandCollider : MonoBehaviour
    {
        [SerializeField] float raycastDistance = 0.3f;
        bool isFalling = true;
        bool isGripping = false;
        bool isTouching = false;
        bool grounded;

        void FixedUpdate()
        {
            grounded = Physics.Raycast(transform.position, Vector3.down, raycastDistance);

            isFalling = !(isGripping || grounded);
            Debug.Log($"{transform.name}, isGripping = {isGripping}, grounded = {grounded}, isTouching = {isTouching}");
        }
        void OnTriggerEnter(Collider other)
        {
            isTouching = true;
            Debug.Log("Collision entered");
        }

        void OnTriggerExit(Collider other)
        {
           isTouching = false;
            Debug.Log("Collision exited");
        }

        public void Grab() => isGripping = isTouching;

        public void Release() => isGripping = false;

        public bool GrabbingState() => isGripping;

        public bool FallingState() => isFalling;

        private void OnDrawGizmos()
        {
            Color rayColor = grounded ? Color.green : Color.red;
            Debug.DrawRay(transform.position, Vector3.down * raycastDistance, rayColor);
        }
    }
}