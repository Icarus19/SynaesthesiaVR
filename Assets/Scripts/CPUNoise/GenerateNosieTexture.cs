using UnityEngine;
using System.IO;

public class GenerateNosieTexture : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] Vector2Int resolution = new Vector2Int(256, 256);

    [Header("Noise Settings")]
    [SerializeField] int octaves = 4;
    [SerializeField] float amplitude = 1f, frequency = 1f, lacunarity = 2.0f, gain = 0.5f;
    [SerializeField] string textureName = "noiseTexture";
    [SerializeField] string savePath = "Assets/";


    [Header("Preview")]
    [SerializeField] bool autoUpdate = true;
    [SerializeField] Texture2D previewTexture;

    public Texture2D PreviewTexture => previewTexture;

    private void OnValidate()
    {
        if (autoUpdate)
            CreateTexture();
    }

    void CreateTexture()
    {
        previewTexture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBAFloat, false);
        previewTexture.name = "NoisePreview";
        previewTexture.wrapMode = TextureWrapMode.Clamp;
        previewTexture.filterMode = FilterMode.Bilinear;

        for(int x = 0; x < resolution.x; x++)
            for(int y = 0; y < resolution.y; y++)
            {
                float nx = (float)x / resolution.x;
                float ny = (float)y / resolution.y;

                float noise = NoiseHelper.FBM(nx, ny, octaves, amplitude, frequency, lacunarity, gain);
                Color col = new Color(noise, noise, noise, 1f);
                previewTexture.SetPixel(x, y, col);
            }

        previewTexture.Apply();
    }

    [ContextMenu("SaveTexture")]
    void SaveTexture()
    {
        if(previewTexture == null)
        {
            Debug.LogWarning("No texture to save");
            return;
        }

        byte[] pngData = previewTexture.EncodeToPNG();
        string fullPath = Path.Combine(savePath, textureName + ".png");

        File.WriteAllBytes(fullPath, pngData);
        Debug.Log("Saved texture to: " + fullPath);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
