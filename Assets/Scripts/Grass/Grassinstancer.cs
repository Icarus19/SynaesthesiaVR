using System.ComponentModel;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using static UnityEngine.Rendering.GPUSort;
using System;

using Random = UnityEngine.Random;

public class Grassinstancer : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    Mesh instanceMesh;

    public MeshEditor meshEditor;
    
    [Header("Grass Settings")]
    [SerializeField, Range(0, 2048)]public int grassResolution;
    [SerializeField][Range(10, 500)] public float fieldSize;
    [SerializeField, Range(4, 64)] int tileSize = 32;
    public int instanceCount;
    [SerializeField] Vector2 grassHeight;
    [SerializeField] Texture2D heightMap;
    [SerializeField] float heightMapScale, heightMapAmplitude;
    [SerializeField][Range(0, 2)] float grassNoiseScale = 0.3f, amplitude = 1.0f, frequency = 1.0f, lacunarity = 2.0f, gain = 0.5f;
    [SerializeField][Range(0, 5)] int octaves = 3;
    ComputeBuffer positionBuffer, rotationBuffer;
    GraphicsBuffer argsBuffer;
    int grassCount;
    uint[] args;
    List<Vector2Int> _visibleTiles = new();

    [Header("HeightmapData")]
    [SerializeField, Tooltip("This will default to the saved map if not chosen")] BakedHeightMapPositions heightMapData;
    [SerializeField] string HeightMapName = "BakedHeightMapPositions";

    [Header("Terrain settings")]
    [SerializeField, Range(4, 256)] int resolution;
    [SerializeField] string meshName = "GrassTerrainMesh";

    /// <summary>
    /// Culling data
    /// </summary>
    Camera cam;

    void Start()
    {
        GetMesh();

        grassCount = grassResolution * grassResolution;

        args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (mesh != null) ? (uint)mesh.GetIndexCount(0) : 0; // indices per instance
        args[1] = (uint)grassCount; // instance count
        args[2] = (mesh != null) ? (uint)mesh.GetIndexStart(0) : 0;
        args[3] = (mesh != null) ? (uint)mesh.GetBaseVertex(0) : 0;
        args[4] = 0;

        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, args.Length * sizeof(uint));
        argsBuffer.SetData(args);

        //You will have to insert the texture directly into the shader because it doesn't work otherwise?!?!
        material.SetInt("_InstanceResolution", grassCount);
        material.SetInt("_GridSize", (int)fieldSize);
    }

    void Update()
    {
        material.SetVector("_CameraData", GetCameraData());

        Graphics.RenderMeshIndirect(
            new RenderParams(material) { worldBounds = new Bounds(Vector3.zero, fieldSize * 2 * Vector3.one) },
            mesh,
            argsBuffer
        );
    }

    Vector4 GetCameraData()
    {
        if (cam == null)
            cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        Vector2 position = new Vector2(cam.transform.position.x, cam.transform.position.z);
        Vector2 direction = new Vector2(cam.transform.forward.x, cam.transform.forward.z).normalized;
        //This is off by about 30 radians on the Y axis, but changing it here doesn't do anything so fuck me

        return new Vector4(position.x, position.y, direction.x, direction.y);
    }

    /// <summary>
    /// Editor mode generations
    /// </summary>
    [ContextMenu("Get Mesh")]
    void GetMesh()
    {
        mesh = meshEditor.grassMesh;
    }

    Vector4[] GenerateGrassPositions()
    {
        Vector4[] positions = new Vector4[instanceCount];

        heightMapData = Resources.Load<BakedHeightMapPositions>(HeightMapName);

        for (int i = 0; i < instanceCount; i++)
        {
            float x = Random.Range(-fieldSize, fieldSize);
            float z = Random.Range(-fieldSize, fieldSize);
            //Sample height later in the shader which would allow us to store rotation here aswell as Y but I don't have time
            //Nevermind. I can't sample a texture in the vertex shader with quest 2
            float y = -1.0f;
            if(heightMapData != null)
            {
                //normalize UV
                float u = (x + fieldSize) / (fieldSize * 2f);
                float v = (z + fieldSize) / (fieldSize * 2f);

                y = heightMapData.GetHeightBilinear(u, v);
            }
            else
            {
                Debug.Log("Heightmap not found");
            }
            
            //Noise to clump grass height together
            //float noise = Mathf.PerlinNoise(x * grassNoiseScale, z * grassNoiseScale);
            float noise = NoiseHelper.FBM(x * grassNoiseScale, z * grassNoiseScale, octaves, amplitude, frequency, lacunarity, gain);
            float w = Mathf.Lerp(grassHeight.x, grassHeight.y, noise);

            positions[i] = new Vector4(x, y, z, w);
        }

        return positions;
    }

    Vector2[] GenerateGrassRotations()
    {
        Vector2[] rotations = new Vector2[instanceCount];

        for(int i = 0; i < instanceCount; i++)
        {
            float angle = Random.Range(0.0f, 360.0f);
            float radians = angle * Mathf.Deg2Rad;

            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            rotations[i] = new Vector2(sin, cos);
        }

        return rotations;
    }

#if UNITY_EDITOR
    [ContextMenu("Save Heightmap Data")]
    void SaveHeightMapData()
    {
        if(heightMap == null)
        {
            Debug.LogError("No texture to read from");
            return;
        }

        Color[] pixels = heightMap.GetPixels();
        int width = heightMap.width;
        int height = heightMap.height;

        float[] heights = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            heights[i] = pixels[i].r * heightMapScale * heightMapAmplitude;
        }

        BakedHeightMapPositions data = ScriptableObject.CreateInstance<BakedHeightMapPositions>();
        data.heights = heights;
        data.width = width;
        data.height = height;

        string path = $"Assets/Resources/{HeightMapName}.asset";
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"HeightMapData saved");
    }

    [ContextMenu("Save heightmap as TerrainMesh")]
    void CreateMeshFromHeightMap()
    {
        float stepSize = (fieldSize * 2f) / resolution;

        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        int[] triangles = new int[resolution * resolution * 6];
        Vector2[] UVs = new Vector2[vertices.Length];

        Color[] pixels = heightMap.GetPixels();
        int width = heightMap.width;
        int height = heightMap.height;

        for (int z = 0; z <= resolution; z++)
            for(int x = 0; x <= resolution; x++)
            {
                int i = x + z * (resolution + 1);

                float worldX = -fieldSize + x * stepSize;
                float worldZ = -fieldSize + z * stepSize;

                float u = (worldX + fieldSize) / (fieldSize * 2f);
                float v = (worldZ + fieldSize) / (fieldSize * 2f);
                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);

                int px = Mathf.Clamp((int)(u * width), 0, width - 1);
                int py = Mathf.Clamp((int)(v * height), 0, height - 1);

                //I dont know why I need to multiply by 100 but it works
                float y = heightMap.GetPixel(px, py).r * heightMapAmplitude * 100;
                Debug.Log(y);

                vertices[i] = new Vector3(worldX, y, worldZ);
                UVs[i] = new Vector2(u, v);
            }

        int triIndex = 0;
        for(int z = 0; z < resolution; z++)
            for(int x = 0; x < resolution; x++)
            {
                int topLeft = x + z * (resolution + 1);
                int bottomLeft = x + (z + 1) * (resolution + 1);

                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft + 1;

                triangles[triIndex++] = topLeft + 1;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomLeft + 1;
            }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = UVs;
        mesh.RecalculateNormals();

        string path = $"Assets/Resources/{meshName}.asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Mesh saved");
    }
#endif

    void OnDestroy()
    {
        positionBuffer?.Release();
        rotationBuffer?.Release();
        argsBuffer?.Release();
    }
}
