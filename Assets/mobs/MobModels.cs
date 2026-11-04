using UnityEngine;

public static class MobModels
{
    private static Shader mobShader;

    public static Material Material(Material current, string name, Color color)
    {
        if (current != null)
            return current;

        if (mobShader == null)
        {
            mobShader = Shader.Find("Universal Render Pipeline/Lit");
            if (mobShader == null)
                mobShader = Shader.Find("Standard");
        }

        Material material = new Material(mobShader) { name = name };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    public static void CreateZombie(Transform root, Material skin, Material shirt, Material pants, Material eyes)
    {
        if (TryCreateImported(root, "zombie"))
            return;

        Part(root, PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.08f, 0f),
            new Vector3(0.58f, 0.48f, 0.38f), shirt);
        Part(root, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.78f, 0.015f),
            new Vector3(0.47f, 0.5f, 0.45f), skin);
        Part(root, PrimitiveType.Cylinder, "Neck", new Vector3(0f, 1.48f, 0f),
            new Vector3(0.15f, 0.14f, 0.15f), skin);
        Part(root, PrimitiveType.Capsule, "Left arm", new Vector3(-0.43f, 1.05f, 0f),
            new Vector3(0.23f, 0.43f, 0.23f), shirt).transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        Part(root, PrimitiveType.Capsule, "Right arm", new Vector3(0.43f, 1.05f, 0f),
            new Vector3(0.23f, 0.43f, 0.23f), shirt).transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        Part(root, PrimitiveType.Capsule, "Left leg", new Vector3(-0.17f, 0.35f, 0f),
            new Vector3(0.24f, 0.38f, 0.25f), pants);
        Part(root, PrimitiveType.Capsule, "Right leg", new Vector3(0.17f, 0.35f, 0f),
            new Vector3(0.24f, 0.38f, 0.25f), pants);
        Part(root, PrimitiveType.Sphere, "Left eye", new Vector3(-0.105f, 1.83f, 0.215f),
            new Vector3(0.075f, 0.09f, 0.045f), eyes);
        Part(root, PrimitiveType.Sphere, "Right eye", new Vector3(0.105f, 1.83f, 0.215f),
            new Vector3(0.075f, 0.09f, 0.045f), eyes);
    }

    public static void CreateWitch(Transform root, Material skin, Material robe, Material eyes)
    {
        if (TryCreateImported(root, "witch"))
            return;

        Part(root, PrimitiveType.Capsule, "Witch body", new Vector3(0f, 0.95f, 0f),
            new Vector3(0.58f, 0.7f, 0.42f), robe);
        Part(root, PrimitiveType.Sphere, "Witch head", new Vector3(0f, 1.82f, 0f),
            new Vector3(0.4f, 0.45f, 0.38f), skin);
        Part(root, PrimitiveType.Cube, "Witch hat brim", new Vector3(0f, 2.19f, 0f),
            new Vector3(0.88f, 0.08f, 0.78f), robe);
        Part(root, PrimitiveType.Cylinder, "Witch hat", new Vector3(0f, 2.42f, 0f),
            new Vector3(0.2f, 0.34f, 0.2f), robe);
        Part(root, PrimitiveType.Sphere, "Witch eyes", new Vector3(0f, 1.87f, 0.2f),
            new Vector3(0.11f, 0.08f, 0.06f), eyes);
        Part(root, PrimitiveType.Capsule, "Witch arm", new Vector3(0.42f, 1.13f, 0f),
            new Vector3(0.2f, 0.43f, 0.2f), robe).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
    }

    public static void CreateGhoul(Transform root, Material hide, Material wings, Material eyes)
    {
        if (TryCreateImported(root, "ghoul"))
            return;

        Part(root, PrimitiveType.Sphere, "Ghoul torso", new Vector3(0f, 0f, 0f),
            new Vector3(0.75f, 0.9f, 0.45f), hide);
        Part(root, PrimitiveType.Sphere, "Ghoul head", new Vector3(0f, 0.65f, 0.05f),
            new Vector3(0.5f, 0.48f, 0.44f), hide);
        Part(root, PrimitiveType.Sphere, "Left wing", new Vector3(-0.8f, 0.13f, -0.02f),
            new Vector3(0.92f, 0.12f, 0.48f), wings);
        Part(root, PrimitiveType.Sphere, "Right wing", new Vector3(0.8f, 0.13f, -0.02f),
            new Vector3(0.92f, 0.12f, 0.48f), wings);
        Part(root, PrimitiveType.Sphere, "Ghoul eyes", new Vector3(0f, 0.72f, 0.26f),
            new Vector3(0.14f, 0.09f, 0.055f), eyes);
    }

    public static GameObject Part(
        Transform parent,
        PrimitiveType shape,
        string partName,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.layer = LayerMask.NameToLayer("Ignore Raycast");
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        return part;
    }

    private static bool TryCreateImported(Transform root, string modelName)
    {
        GameObject modelPrefab = Resources.Load<GameObject>("models/" + modelName);
        if (modelPrefab == null)
            return false;

        GameObject model = Object.Instantiate(modelPrefab, root, false);
        model.name = modelName + " model";
        return true;
    }
}
