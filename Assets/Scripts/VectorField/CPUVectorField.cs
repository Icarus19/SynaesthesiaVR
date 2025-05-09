using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;


public class CPUVectorField : MonoBehaviour
{
    //Flat array of index x + y * sizeX + z * sizeX * sizeY
    //Specifically not using NativeArray becuause ChatGPT told me it works better with Jobs and not GPU
    List<float3> VectorField;

    //Lets do simple CPU vector field as a jagged array first (Are Vector3[][] not a jagged array?)
    Vector3[,,] CPUField;

    //Random time variable for now
    float time;

    [Header("VectorField settings")]
    public Vector3Int size;
    [Range(0, 1)]public float noiseScale;
    [Range(0, 100000)]public int seed;

    [Header("Debug settings")]
    public float arrowHeadLength = 0.25f;
    public float arrowHeadAngle = 20f;

    void Update()
    {
        time = Time.time;
        CreateField();
    }

    [ContextMenu("CreateField")]
    void CreateField() 
    {
        Vector3 halfsize = new Vector3(size.x, size.y, size.z) * 0.5f;
     
        CPUField = new Vector3[size.x, size.y, size.z];
        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
                for(int z = 0; z < size.z; z++)
                {
                    CPUField[x, y, z] = GetWindDirection(new Vector3(x, y, z) - halfsize, time);
                }
    }

    Vector3 GetWindDirection(Vector3 position, float time)
    {
        float x = (position.x  + seed) * noiseScale;
        float y = (position.y + seed * 2f) * noiseScale;
        float z = (position.z + seed * 3f) * noiseScale;

        float noiseX = (Mathf.PerlinNoise(y + time, z + time * 0.7f) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(x + time * 0.6f, z + time) - 0.5f) * 2f;
        float noiseZ = (Mathf.PerlinNoise(x + time, y + time * 0.4f) - 0.5f) * 2f;

        return new Vector3(noiseX, noiseY, noiseZ).normalized;
    }

    void OnDrawGizmos()
    {
        Vector3 halfsize = new Vector3(size.x, size.y, size.z) * 0.5f;

        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++)
                {
                    Vector3 position = new Vector3(x, y, z) - halfsize;
                    Vector3 direction = CPUField[x, y, z];
                    DrawArrow(position, direction);
                }
    }

    void DrawArrow(Vector3 pos, Vector3 dir)
    {
        Color color = VectorToColor(dir);
        Gizmos.color = color;

        if (dir == Vector3.zero) return;

        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

        Gizmos.DrawRay(pos + dir, right * arrowHeadLength);
        Gizmos.DrawRay(pos + dir, left * arrowHeadLength);
    }

    Color VectorToColor(Vector3 dir)
    {
        Vector3 mapped = (dir + Vector3.one) * 0.5f;
        return new Color(mapped.x, mapped.y, mapped.z);
    }
}
