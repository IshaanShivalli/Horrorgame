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
        Part(root, PrimitiveType.Capsule, "Witch body", new Vector3(0f, 1f, 0f),
            new Vector3(0.5f, 0.8f, 0.4f), robe);
        Part(root, PrimitiveType.Sphere, "Witch head", new Vector3(0f, 1.9f, 0f),
            new Vector3(0.45f, 0.5f, 0.42f), skin);
        Part(root, PrimitiveType.Cube, "Witch hat brim", new Vector3(0f, 2.3f, 0f),
            new Vector3(0.8f, 0.1f, 0.6f), robe);
        Part(root, PrimitiveType.Cylinder, "Witch hat", new Vector3(0f, 2.6f, 0f),
            new Vector3(0.15f, 0.4f, 0.15f), robe);
        Part(root, PrimitiveType.Sphere, "Witch left eye", new Vector3(-0.1f, 1.92f, 0.15f),
            new Vector3(0.06f, 0.07f, 0.05f), eyes);
        Part(root, PrimitiveType.Sphere, "Witch right eye", new Vector3(0.1f, 1.92f, 0.15f),
            new Vector3(0.06f, 0.07f, 0.05f), eyes);
        Part(root, PrimitiveType.Capsule, "Witch arm", new Vector3(0.35f, 1.2f, 0f),
            new Vector3(0.18f, 0.4f, 0.18f), robe).transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
        Part(root, PrimitiveType.Cylinder, "Witch staff", new Vector3(0.55f, 1.1f, 0.05f),
            new Vector3(0.05f, 0.6f, 0.05f), robe);
        Part(root, PrimitiveType.Sphere, "Witch staff orb", new Vector3(0.55f, 1.8f, 0.05f),
            new Vector3(0.18f, 0.18f, 0.18f), eyes);
    }

    public static void CreateGhoul(Transform root, Material hide, Material eyes)
    {
        Part(root, PrimitiveType.Capsule, "Ghoul hunched torso", new Vector3(0f, 1.02f, 0f),
            new Vector3(0.58f, 0.7f, 0.44f), hide);
        Part(root, PrimitiveType.Sphere, "Ghoul head", new Vector3(0f, 1.82f, 0.08f),
            new Vector3(0.42f, 0.42f, 0.4f), hide);
        Part(root, PrimitiveType.Capsule, "Ghoul left arm", new Vector3(-0.47f, 1.12f, 0.05f),
            new Vector3(0.2f, 0.55f, 0.22f), hide).transform.localRotation =
            Quaternion.Euler(0f, 0f, 25f);
        Part(root, PrimitiveType.Capsule, "Ghoul right arm", new Vector3(0.47f, 1.12f, 0.05f),
            new Vector3(0.2f, 0.55f, 0.22f), hide).transform.localRotation =
            Quaternion.Euler(0f, 0f, -25f);
        Part(root, PrimitiveType.Capsule, "Ghoul left leg", new Vector3(-0.17f, 0.42f, 0f),
            new Vector3(0.23f, 0.48f, 0.25f), hide);
        Part(root, PrimitiveType.Capsule, "Ghoul right leg", new Vector3(0.17f, 0.42f, 0f),
            new Vector3(0.23f, 0.48f, 0.25f), hide);
        Part(root, PrimitiveType.Sphere, "Ghoul left eye", new Vector3(-0.11f, 1.87f, 0.27f),
            new Vector3(0.075f, 0.07f, 0.045f), eyes);
        Part(root, PrimitiveType.Sphere, "Ghoul right eye", new Vector3(0.11f, 1.87f, 0.27f),
            new Vector3(0.075f, 0.07f, 0.045f), eyes);
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
