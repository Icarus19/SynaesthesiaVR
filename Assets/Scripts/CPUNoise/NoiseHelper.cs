using UnityEngine;

public static class NoiseHelper
{
    //This is not recommended to call multiple times per frame
    public static float FBM(float x, float y, int octaves, float amplitude, float frequency, float lacunarity, float gain)
    {
        float sum = 0.0f;
        float normalization = 0.0f;

        for(int i = 0; i < octaves; i++)
        {
            sum += amplitude * Mathf.PerlinNoise(x * frequency, y * frequency);
            normalization += amplitude;

            amplitude *= gain;
            frequency *= lacunarity;
        }

        return sum;
    }
}
