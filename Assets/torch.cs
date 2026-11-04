using UnityEngine;

public sealed class torch : MonoBehaviour
{
    private static Material handleMaterial;
    private static Material flameMaterial;

    public static GameObject CreateModel(Transform parent, Vector3 localPosition, bool held = true)
    {
        handleMaterial = MobModels.Material(
            handleMaterial,
            "Torch wood",
            new Color(0.35f, 0.19f, 0.08f));
        flameMaterial = MobModels.Material(
            flameMaterial,
            "Torch flame",
            new Color(1f, 0.46f, 0.08f));
        if (flameMaterial.HasProperty("_EmissionColor"))
        {
            flameMaterial.EnableKeyword("_EMISSION");
            flameMaterial.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.035f) * 2.5f);
        }
        WorldTextures.Apply(flameMaterial, "torch");

        GameObject model = new GameObject(held ? "Held torch" : "Torch model");
        model.layer = LayerMask.NameToLayer("Ignore Raycast");
        model.transform.SetParent(parent, false);
        model.transform.localPosition = localPosition;
        if (held)
            model.transform.localRotation = Quaternion.Euler(12f, 0f, -18f);

        MobModels.Part(
            model.transform,
            PrimitiveType.Cube,
            "Torch handle",
            new Vector3(0f, 0.27f, 0f),
            new Vector3(0.14f, 0.55f, 0.14f),
            handleMaterial);
        MobModels.Part(
            model.transform,
            PrimitiveType.Sphere,
            "Torch flame",
            new Vector3(0f, 0.63f, 0f),
            new Vector3(0.2f, 0.29f, 0.2f),
            flameMaterial);
        MobModels.Part(
            model.transform,
            PrimitiveType.Sphere,
            "Torch ember",
            new Vector3(0f, 0.62f, 0.04f),
            new Vector3(0.1f, 0.16f, 0.1f),
            MobModels.Material(null, "Torch ember", new Color(1f, 0.86f, 0.25f)));

        Light torchLight = model.AddComponent<Light>();
        torchLight.type = LightType.Point;
        torchLight.color = new Color(1f, 0.58f, 0.24f);
        torchLight.intensity = held ? 1.8f : 1.4f;
        torchLight.range = held ? 10f : 8f;
        torchLight.shadows = LightShadows.None;
        return model;
    }
}
