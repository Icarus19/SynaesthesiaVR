using Unity.Mathematics;
using UnityEngine;

namespace CustomPhysics
{
    public class CustomCollider : MonoBehaviour
    {
        public bool gravity, kinetic, debug;

        [SerializeField] float raycastDistance = 0.1f;

        Vector3 velocity;
        bool isFalling = true;
        bool isGripping = false;
        bool isTouching = false;
        bool grounded;


        void FixedUpdate()
        {
            
        }



        public void Grab() => isGripping = isTouching;

        public void Release() => isGripping = false;

        public bool GrabbingState() => isGripping;

        public bool FallingState() => isFalling;

        private void OnDrawGizmos()
        {
            if (!debug) return;
            Color rayColor = grounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastDistance);

            if (!Application.isPlaying) return;
            Vector3 predictedOffset = velocity * Time.fixedDeltaTime;
            Gizmos.DrawSphere(transform.position + predictedOffset, 0.1f);
            
        }
    }
}
