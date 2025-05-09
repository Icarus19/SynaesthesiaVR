using UnityEngine;

[CreateAssetMenu(fileName = "HeightmapData", menuName = "Terrian/Heightmap Data")]
public class BakedHeightMapPositions : ScriptableObject
{
    //Hide in Inspector prevents crashes from large fields of grass
    [HideInInspector] public float[] heights;
    public int width;
    public int height;
    
    public float GetHeight(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return 0f;
        return heights[y * width + x];
    }

    public float GetHeightBilinear(float u, float v)
    {
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);
        float fx = u * (width - 1);
        float fy = v * (height - 1);
        int x = Mathf.FloorToInt(fx);
        int y = Mathf.FloorToInt(fy);
        return GetHeight(x, y);
    }
}
