using UnityEngine;

public class GPUVectorField : MonoBehaviour
{
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Material material;
    ComputeBuffer vectorFieldBuffer;

    [Header("VectorField Settings")]
    [SerializeField][Tooltip("Please use a resolution of 16x1x16")]Vector3Int size;
    [SerializeField][Range(0, 100000)] int seed;
    [SerializeField][Range(0, 1)] float scale;
    [SerializeField] float speed;

    [Header("Debug")]
    [SerializeField] bool debug;
    [SerializeField] Material debugMaterial;

    void Start()
    {
        RenderTexture vectorFieldTex = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        vectorFieldTex.enableRandomWrite = true;
        vectorFieldTex.filterMode = FilterMode.Bilinear;
        vectorFieldTex.wrapMode = TextureWrapMode.Clamp;
        vectorFieldTex.Create();

        computeShader.SetTexture(0, "_VectorFieldTex", vectorFieldTex);
        computeShader.SetInt("_SizeX", size.x);
        computeShader.SetInt("_SizeY", size.y);
        computeShader.SetInt("_SizeZ", size.z);
        computeShader.SetInt("_Seed", seed);
        computeShader.SetFloat("_Scale", scale);
        computeShader.SetFloat("_Speed", speed);

        if (material != null)
        {
            material.SetTexture("_VectorFieldTex", vectorFieldTex);
            material.SetInt("_SizeX", size.x);
            material.SetInt("_SizeY", size.y);
            material.SetInt("_SizeZ", size.z);
        }
            
        if (debugMaterial != null)
        {
            debugMaterial.SetTexture("_VectorFieldTex", vectorFieldTex);
            debugMaterial.SetInt("_SizeX", size.x );
            debugMaterial.SetInt("_SizeY", size.y );
            debugMaterial.SetInt("_SizeZ", size.z );
        }
    }

    void Update()
    {
        computeShader.SetFloat("_TimeCPU", Time.time);
        computeShader.Dispatch(0, Mathf.CeilToInt(size.x / 16.0f), Mathf.CeilToInt(size.y / 1.0f), Mathf.CeilToInt(size.z / 16.0f));
    }

    void OnRenderObject()
    {
        if (debugMaterial != null && debug)
        {
            //Debug.Log("Render VectorField");
            debugMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Lines, size.x * size.y * size.z * 2);
        }
    }

    void OnValidate()
    {
        computeShader.SetInt("_SizeX", size.x);
        computeShader.SetInt("_SizeY", size.y);
        computeShader.SetInt("_SizeZ", size.z);
        computeShader.SetInt("_Seed", seed);
        computeShader.SetFloat("_Scale", scale);
        computeShader.SetFloat("_Speed", speed);
    }

    void OnDisable()
    {
        //vectorFieldBuffer.Dispose();
    }
}
