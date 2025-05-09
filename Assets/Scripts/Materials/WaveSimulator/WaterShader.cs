using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterShader : MonoBehaviour
{
    public Material material;
    Vector2Int resolution = new Vector2Int(64, 64); //This only works with 64x64(Don't know why)
    public ComputeShader computeShader;
    public Transform player;

    [Header("Simulation parameters")]
    public float pressureConstant; //Original values I found 1, 1, 2, 4, 0.005, 0.002, 0.999
    public float pressureRadius, waveCenterWeight, waveDivisor, springConstant, velocityDamping, pressureDamping;

    RenderTexture[] textures = new RenderTexture[2];
    int currentTexture = 0;
    Vector3 prevPosition;
    bool ping = false;

    void Start()
    {
        //GeneratePlaneMesh();
        SetupWaterTexture();
    }
    
    void SetupWaterTexture() 
    {
        for(int i = 0; i < textures.Length; i++)
        {
            textures[i] = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.ARGBFloat);
            textures[i].enableRandomWrite = true;
            textures[i].wrapMode = TextureWrapMode.Clamp;
            textures[i].Create();
        }
    }

    void Update()
    {
        int nextTexture = 1 - currentTexture;

        computeShader.SetTexture(0, "_InputTexture", textures[currentTexture]);
        computeShader.SetTexture(0, "_OutputTexture", textures[nextTexture]);
        material.SetTexture("_DataTexture", textures[nextTexture]);
        currentTexture = nextTexture;

        computeShader.SetVector("_PlayerUVPosition", WorldToUV(player.position));
        computeShader.SetVector("_Resolution", new Vector3(resolution.x, resolution.y, 0));
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetBool("_Active", ping);
        ping = false;
        prevPosition = player.position;
        computeShader.SetFloat("_PressureConstant", pressureConstant);
        computeShader.SetFloat("_PressureRadius", pressureRadius);
        computeShader.SetFloat("_WaveCenterWeight", waveCenterWeight);
        computeShader.SetFloat("_WaveDivisor", waveDivisor);
        computeShader.SetFloat("_SpringConstant", springConstant);
        computeShader.SetFloat("_VelocityDamping", velocityDamping);
        computeShader.SetFloat("_PressureDamping", pressureDamping);

        computeShader.Dispatch(0, 8, 8, 1);
    }


    public void Ping()
    {
        ping = true;
    }

    Vector2 WorldToUV(Vector3 worldPosition)
    {
        //Plane size is -5.0 to 5.0 which would make world coordinates (-5.0, -5.0) into UV (0, 0) and world (0, 0) into UV (0.5, 0.5) and world (5.0, 5.0) into UV (1, 1)
        float halfWidth = 5f * transform.localScale.x;
        float halfHeight = 5f * transform.localScale.z;
        
        float u = (worldPosition.x - transform.position.x + halfWidth) / (2f * halfWidth); //Plane of size 10 with world position 0, 0 would stretch from -5 to 5;
        float v = (worldPosition.z - transform.position.z + halfHeight) / (2f * halfHeight);

        return new Vector2(u, v);
    }

    //Wanted to make a mesh based on resolution so I could easily change it but oh well
    void GeneratePlaneMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(resolution.x + 1) * (resolution.y + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[resolution.x * resolution.y * 6];

        for (int y = 0; y <= resolution.y; y++)
            for (int x = 0; x <= resolution.x; x++)
            {
                int i = x + y * (resolution.x + 1);
                float xPos = (float)x / resolution.x - 0.5f;
                float yPos = (float)y / resolution.y - 0.5f;

                vertices[i] = new Vector3(xPos, 0, yPos);
                uvs[i] = new Vector2((float)x / resolution.x, (float)y / resolution.y);
            }

        int tri = 0;
        for (int y = 0; y < resolution.y; y++)
            for (int x = 0; x < resolution.x; x++)
            {
                int i = x + y * (resolution.x + 1);

                triangles[tri++] = i;
                triangles[tri++] = i + resolution.x + 1;
                triangles[tri++] = i + 1;

                triangles[tri++] = i + 1;
                triangles[tri++] = i + resolution.x + 1;
                triangles[tri++] = i + resolution.x + 2;
            }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
