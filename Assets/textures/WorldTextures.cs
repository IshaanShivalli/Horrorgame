using UnityEngine;

public static class WorldTextures
{
    public static void Apply(Material material, string textureName)
    {
        Texture2D texture = Resources.Load<Texture2D>("textures/" + textureName);
        if (texture == null)
        {
            Debug.LogWarning("Missing world texture resource: textures/" + textureName);
            return;
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        else if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
    }
}
