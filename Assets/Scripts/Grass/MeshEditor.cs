using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MeshEditor : MonoBehaviour
{
    [Header("Grass Settings")]
    [SerializeField] string meshName = "Grass";
    [SerializeField] float height = 1f;
    [SerializeField] float width = 0.1f;
    [SerializeField] int segmentCount = 3;
    [SerializeField] AnimationCurve widthOverHeight = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Mesh inspector")]
    public Mesh grassMesh;

    [ContextMenu("GenerateMesh")]
    void OnValidate()
    {
        if(grassMesh != null)
        {
            DestroyImmediate(grassMesh);
        }

        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for(int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float y = t * height;

            float widthMultiplier = widthOverHeight.Evaluate(t);
            float xOffset = (width * 0.5f) * widthMultiplier;

            vertices.Add(new Vector3(-xOffset, y, 0));
            vertices.Add(new Vector3(xOffset, y, 0));

            if(i < segmentCount)
            {
                int idx = i * 2;
                triangles.Add(idx);
                triangles.Add(idx + 2);
                triangles.Add(idx + 1);

                triangles.Add(idx + 1);
                triangles.Add(idx + 2);
                triangles.Add(idx + 3);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.name = meshName;

        grassMesh = mesh;
    }
}
