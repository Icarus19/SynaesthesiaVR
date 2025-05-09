using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlanarReflection : MonoBehaviour
{
    public Camera mainCamera;
    public Material waterMaterial;

    [Header("Reflection settings")]
    public Camera reflectionCamera;
    public RenderTexture reflectionTexture;

    void Start()
    {
        //reflectionCamera = new GameObject("cubeMapCam").AddComponent<Camera>();
        reflectionTexture = new RenderTexture(512, 512, 16);
        reflectionTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        reflectionTexture.hideFlags = HideFlags.DontSave;
        reflectionTexture.useMipMap = true;
        reflectionTexture.autoGenerateMips = true;
        reflectionTexture.Create();
    }
    void FixedUpdate()
    {
        reflectionCamera.transform.position = mainCamera.transform.position;
        reflectionCamera.RenderToCubemap(reflectionTexture);
        waterMaterial.SetTexture("_ReflectionTexture", reflectionTexture);
    }


    //Probably not needed anymore since I'm using Cubemap now
    Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 reflectionMat = Matrix4x4.identity;

        reflectionMat.m00 = 1 - 2 * plane[0] * plane[0];
        reflectionMat.m01 = -2 * plane[0] * plane[1];
        reflectionMat.m02 = -2 * plane[0] * plane[2];
        reflectionMat.m03 = -2 * plane[3] * plane[0];

        reflectionMat.m10 = -2 * plane[1] * plane[0];
        reflectionMat.m11 = 1 - 2 * plane[1] * plane[1];
        reflectionMat.m12 = -2 * plane[1] * plane[2];
        reflectionMat.m13 = -2 * plane[3] * plane[1];

        reflectionMat.m20 = -2 * plane[2] * plane[0];
        reflectionMat.m21 = -2 * plane[2] * plane[1];
        reflectionMat.m22 = 1 - 2 * plane[2] * plane[2];
        reflectionMat.m23 = -2 * plane[3] * plane[2];

        return reflectionMat;
    }

    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * 0.07f;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cPos = m.MultiplyPoint(offsetPos);
        Vector3 cNormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }
}
