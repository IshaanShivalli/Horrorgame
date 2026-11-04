using System;
using System.Collections.Generic;
using UnityEngine;

public static class Trees
{
    public static List<Vector3Int> Generate(world currentWorld, Vector2Int coordinate, int[,,] blocks, Transform parent)
    {
        List<Vector3Int> trunks = new List<Vector3Int>();
        int seed = unchecked(coordinate.x * 73856093 ^ coordinate.y * 19349663 ^ 83492791);
        System.Random random = new System.Random(seed);
        List<Vector2Int> treePositions = new List<Vector2Int>();

        for (int attempt = 0; attempt < 4; attempt++)
        {
            if (random.NextDouble() > 0.72)
                continue;

            int x = random.Next(2, world.ChunkSize - 2);
            int z = random.Next(2, world.ChunkSize - 2);
            bool tooClose = false;
            foreach (Vector2Int existing in treePositions)
            {
                if ((existing - new Vector2Int(x, z)).sqrMagnitude < 64)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            treePositions.Add(new Vector2Int(x, z));
            int worldX = coordinate.x * world.ChunkSize + x;
            int worldZ = coordinate.y * world.ChunkSize + z;
            int groundY = currentWorld.GetHeight(worldX, worldZ) + 1;
            int trunkHeight = Mathf.Min(random.Next(5, 8), world.WorldHeight - groundY - 4);
            if (trunkHeight < 5)
                continue;

            for (int y = groundY; y < groundY + trunkHeight; y++)
            {
                blocks[x, y, z] = ItemTypes.Log;
                trunks.Add(new Vector3Int(x, y, z));
            }
            CreateTree(currentWorld, parent, x + 0.5f, groundY, z + 0.5f, trunkHeight, random);
        }

        return trunks;
    }

    private static void CreateTree(
        world currentWorld,
        Transform parent,
        float x,
        float groundY,
        float z,
        int trunkHeight,
        System.Random random)
    {
        GameObject treeRoot = new GameObject("Horror Tree");
        treeRoot.transform.SetParent(parent, false);
        treeRoot.transform.localPosition = new Vector3(x, groundY, z);
        treeRoot.layer = LayerMask.NameToLayer("Ignore Raycast");

        int canopyBase = trunkHeight - 2;
        float[] canopySizes = { 3.8f, 3f, 2f };
        for (int layer = 0; layer < canopySizes.Length; layer++)
        {
            float size = canopySizes[layer];
            float canopyY = canopyBase + layer * 1.1f;
            CreatePrimitive(treeRoot.transform, PrimitiveType.Cube, "Blocky Canopy",
                new Vector3(0f, canopyY, 0f),
                new Vector3(size, 1.1f, size),
                currentWorld.TreeLeafMaterial);
        }

        for (int branch = 0; branch < 3; branch++)
        {
            float angle = branch * 120f + random.Next(-18, 19);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 start = new Vector3(0f, trunkHeight - 2.3f + branch * 0.6f, 0f);
            Vector3 end = start + direction * 2.15f + Vector3.up * 0.45f;
            CreateBranch(treeRoot.transform, start, end, currentWorld.TreeBarkMaterial);
        }

        float hangingAngle = random.Next(0, 360);
        Vector3 hangingDirection = Quaternion.Euler(0f, hangingAngle, 0f) * Vector3.forward;
        Vector3 hangingAnchor = hangingDirection * 0.9f;
        hangingAnchor.y = trunkHeight - 1.8f;
        CreatePrimitive(treeRoot.transform, PrimitiveType.Cylinder, "Hanging Rope",
            hangingAnchor + Vector3.down * 0.55f,
            new Vector3(0.045f, 0.55f, 0.045f),
            currentWorld.TreeBarkMaterial);
        CreateHangingFigure(treeRoot.transform, hangingAnchor + Vector3.down * 1.15f, currentWorld.HangingBodyMaterial);
    }

    private static void CreateBranch(Transform parent, Vector3 start, Vector3 end, Material material)
    {
        Vector3 direction = end - start;
        GameObject branch = CreatePrimitive(parent, PrimitiveType.Cylinder, "Branch",
            (start + end) * 0.5f,
            new Vector3(0.22f, direction.magnitude * 0.5f, 0.22f),
            material);
        branch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
    }

    private static void CreateHangingFigure(Transform parent, Vector3 position, Material material)
    {
        GameObject figure = new GameObject("Hanging Figure");
        figure.transform.SetParent(parent, false);
        figure.transform.localPosition = position;
        figure.layer = LayerMask.NameToLayer("Ignore Raycast");

        CreatePrimitive(figure.transform, PrimitiveType.Cube, "Head",
            new Vector3(0f, 0.45f, 0f), new Vector3(0.38f, 0.38f, 0.36f), material);
        CreatePrimitive(figure.transform, PrimitiveType.Cube, "Torso",
            new Vector3(0f, -0.1f, 0f), new Vector3(0.5f, 0.42f, 0.4f), material);
        CreateLimb(figure.transform, new Vector3(-0.2f, -0.22f, 0f), new Vector3(-0.34f, -0.72f, 0f), material);
        CreateLimb(figure.transform, new Vector3(0.2f, -0.22f, 0f), new Vector3(0.34f, -0.72f, 0f), material);
        CreateLimb(figure.transform, new Vector3(-0.12f, -0.38f, 0f), new Vector3(-0.19f, -0.92f, 0.03f), material);
        CreateLimb(figure.transform, new Vector3(0.12f, -0.38f, 0f), new Vector3(0.19f, -0.92f, -0.03f), material);
    }

    private static void CreateLimb(Transform parent, Vector3 start, Vector3 end, Material material)
    {
        Vector3 direction = end - start;
        GameObject limb = CreatePrimitive(parent, PrimitiveType.Cube, "Hanging Figure Limb",
            (start + end) * 0.5f,
            new Vector3(0.19f, direction.magnitude * 0.5f, 0.19f),
            material);
        limb.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
    }

    private static GameObject CreatePrimitive(Transform parent, PrimitiveType type, string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.layer = LayerMask.NameToLayer("Ignore Raycast");
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            UnityEngine.Object.Destroy(partCollider);
        return part;
    }
}
