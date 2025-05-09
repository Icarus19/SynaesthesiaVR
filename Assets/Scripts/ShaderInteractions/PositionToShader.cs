using UnityEngine;

public class PositionToShader : MonoBehaviour
{
    public Material material;

    void FixedUpdate()
    {
        material.SetVector("_PlayerWorldPosition", transform.position);
    }
}
