using UnityEngine;

namespace CustomPhysics
{
    public class HandCollider : MonoBehaviour
    {
        [SerializeField] PlayerPhysics playerPhysics;
        [SerializeField] float raycastDistance = 0.3f;
        [SerializeField] LayerMask ignoreLayer;
        [HideInInspector] public bool isColliding;
        [HideInInspector] public bool grounded;
        Vector3 collisionNormal = Vector3.zero;

        private void FixedUpdate()
        {
            grounded = Physics.Raycast(transform.position, Vector3.down, raycastDistance, ~ignoreLayer);
        }

        public bool PredictMove(Vector3 position)
        {
            return !Physics.CheckSphere(position, transform.localScale.x, ~ignoreLayer);
        }

        //Unused because it didn't work
        public Vector3 GetCollisionNormal()
        {
            if (grounded)
                return Vector3.up;

            if (isColliding)
                return collisionNormal;

            return Vector3.zero;
        }
        void OnCollisionEnter(Collision other)
        {
            isColliding = true;
            /*foreach(ContactPoint contact in other.contacts)
            {
                //Surface normals to prevent collision is not advisable
                playerPhysics.ColliderForce(contact.normal * 0.1f);
                collisionNormal = contact.normal;
            }*/
        }

        void OnCollisionExit(Collision other)
        {
            isColliding = false;
        }

        private void OnDrawGizmos()
        {
            Color rayColor = grounded ? Color.green : Color.red;
            Debug.DrawRay(transform.position, Vector3.down * raycastDistance, rayColor);
        }
    }
}