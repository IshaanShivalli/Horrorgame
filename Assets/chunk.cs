using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ChunkSaveData
{
    public int version;
    public int[] blocks;
}

public sealed class chunk
{
    private readonly world world;
    private readonly int[,,] blocks = new int[global::world.ChunkSize, global::world.WorldHeight, global::world.ChunkSize];
    private readonly List<Vector3Int> treeTrunks = new List<Vector3Int>();
    private readonly List<GameObject> torchModels = new List<GameObject>();
    private readonly GameObject gameObject;
    private readonly MeshFilter meshFilter;
    private readonly MeshCollider meshCollider;
    private int highestBlockY;
    private bool isDirty;

    public Vector2Int Coordinate { get; }
    public bool IsDirty => isDirty;

    public chunk(world world, Vector2Int coordinate)
    {
        this.world = world;
        Coordinate = coordinate;
        gameObject = new GameObject("Chunk " + coordinate.x + ", " + coordinate.y);
        gameObject.transform.SetParent(world.transform);
        gameObject.transform.position = new Vector3(coordinate.x * world.ChunkSize, 0f, coordinate.y * world.ChunkSize);
        meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = world.Materials;
        meshCollider = gameObject.AddComponent<MeshCollider>();

        for (int x = 0; x < world.ChunkSize; x++)
        {
            for (int z = 0; z < world.ChunkSize; z++)
            {
                int height = world.GetHeight(coordinate.x * world.ChunkSize + x, coordinate.y * world.ChunkSize + z);
                for (int y = 0; y <= height; y++)
                    blocks[x, y, z] = y == height ? 1 : y >= height - 3 ? 2 : 3;

                int worldX = coordinate.x * world.ChunkSize + x;
                int worldZ = coordinate.y * world.ChunkSize + z;
                float silverVein = Mathf.PerlinNoise((worldX + 413f) * 0.19f, (worldZ - 271f) * 0.19f);
                if (height > 7 && silverVein > 0.79f)
                    blocks[x, height - 5, z] = ItemTypes.SilverOre;
            }
        }

        treeTrunks.AddRange(Trees.Generate(world, coordinate, blocks, gameObject.transform));
        WorldStructures.GenerateChunk(world, coordinate, blocks);

        int[] savedBlocks = world.LoadChunkBlocks(coordinate, out int saveVersion);
        if (savedBlocks != null)
        {
            LoadBlocks(savedBlocks);
            if (savedBlocks.Length == world.ChunkSize * global::world.LegacyWorldHeight * world.ChunkSize)
            {
                foreach (Vector3Int trunkBlock in treeTrunks)
                    blocks[trunkBlock.x, trunkBlock.y, trunkBlock.z] = ItemTypes.Log;
            }

            if (saveVersion < world.CurrentChunkSaveVersion)
            {
                WorldStructures.GenerateChunk(world, coordinate, blocks, true);
                isDirty = true;
            }
        }

        WorldStructures.CreateChunkGraveModels(world, coordinate, gameObject.transform);
        RecalculateHighestBlockY();
    }

    public bool SetBlock(int x, int y, int z, int blockType)
    {
        if (x < 0 || x >= world.ChunkSize || y < 0 || y >= world.WorldHeight ||
            z < 0 || z >= world.ChunkSize ||
            (blockType != 0 && !ItemTypes.IsWorldBlock(blockType)))
            return false;
        if (blocks[x, y, z] == blockType)
            return true;

        blocks[x, y, z] = blockType;
        isDirty = true;
        if (blockType != 0 && y > highestBlockY)
            highestBlockY = y;
        else if (blockType == 0 && y == highestBlockY)
            RecalculateHighestBlockY();
        return true;
    }

    public int GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= world.ChunkSize || y < 0 || y >= world.WorldHeight || z < 0 || z >= world.ChunkSize)
            return 0;
        return blocks[x, y, z];
    }

    public int[] CopyBlocks()
    {
        int[] copy = new int[world.ChunkSize * world.WorldHeight * world.ChunkSize];
        for (int x = 0; x < world.ChunkSize; x++)
        {
            for (int y = 0; y <= highestBlockY; y++)
            {
                for (int z = 0; z < world.ChunkSize; z++)
                    copy[(x * world.WorldHeight + y) * world.ChunkSize + z] = blocks[x, y, z];
            }
        }
        return copy;
    }

    private void LoadBlocks(int[] savedBlocks)
    {
        int savedHeight = savedBlocks.Length == world.ChunkSize * global::world.LegacyWorldHeight * world.ChunkSize
            ? global::world.LegacyWorldHeight
            : world.WorldHeight;
        for (int x = 0; x < world.ChunkSize; x++)
        {
            for (int y = 0; y < savedHeight; y++)
            {
                for (int z = 0; z < world.ChunkSize; z++)
                    blocks[x, y, z] = savedBlocks[(x * savedHeight + y) * world.ChunkSize + z];
            }
        }
    }

    public void MarkSaved()
    {
        isDirty = false;
    }

    public void Destroy()
    {
        Mesh mesh = meshFilter.sharedMesh;
        UnityEngine.Object.Destroy(gameObject);
        if (mesh != null)
            UnityEngine.Object.Destroy(mesh);
    }

    public void BuildMesh()
    {
        foreach (GameObject torchModel in torchModels)
        {
            if (torchModel != null)
            {
                torchModel.SetActive(false);
                UnityEngine.Object.Destroy(torchModel);
            }
        }
        torchModels.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int>[] triangles = new List<int>[world.Materials.Length];
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        Vector3Int[] directions =
        {
            Vector3Int.up, Vector3Int.down, Vector3Int.left,
            Vector3Int.right, Vector3Int.forward, Vector3Int.back
        };
        Vector3[][] corners =
        {
            new[] { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) },
            new[] { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) },
            new[] { new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0) },
            new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
            new[] { new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1) },
            new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) }
        };

        for (int x = 0; x < world.ChunkSize; x++)
        {
            for (int y = 0; y <= highestBlockY; y++)
            {
                for (int z = 0; z < world.ChunkSize; z++)
                {
                    int block = blocks[x, y, z];
                    if (block == 0)
                        continue;
                    if (block == ItemTypes.Torch)
                    {
                        torchModels.Add(torch.CreateModel(
                            gameObject.transform,
                            new Vector3(x + 0.5f, y, z + 0.5f),
                            false));
                        continue;
                    }

                    for (int face = 0; face < directions.Length; face++)
                    {
                        Vector3Int neighbor = new Vector3Int(x, y, z) + directions[face];
                        if (IsSolid(neighbor.x, neighbor.y, neighbor.z))
                            continue;

                        int materialIndex = block == ItemTypes.SilverOre ? 5 : block == ItemTypes.Torch ? 4 :
                            block == ItemTypes.Log || block == ItemTypes.CraftingTable ? 3 :
                            block == ItemTypes.Stone ? 2 : block == ItemTypes.Dirt || face != 0 ? 1 : 0;
                        int firstVertex = vertices.Count;
                        for (int corner = 0; corner < 4; corner++)
                        {
                            vertices.Add(new Vector3(x, y, z) + corners[face][corner]);
                            uvs.Add(corner == 0 ? new Vector2(0f, 0f) :
                                corner == 1 ? new Vector2(0f, 1f) :
                                corner == 2 ? new Vector2(1f, 1f) : new Vector2(1f, 0f));
                        }

                        List<int> indices = triangles[materialIndex];
                        indices.Add(firstVertex);
                        indices.Add(firstVertex + 1);
                        indices.Add(firstVertex + 2);
                        indices.Add(firstVertex);
                        indices.Add(firstVertex + 2);
                        indices.Add(firstVertex + 3);
                    }
                }
            }
        }

        Mesh mesh = new Mesh
        {
            indexFormat = vertices.Count > ushort.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = triangles.Length;
        for (int i = 0; i < triangles.Length; i++)
            mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateNormals();

        Mesh previousMesh = meshFilter.sharedMesh;
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
        if (previousMesh != null)
            UnityEngine.Object.Destroy(previousMesh);
    }

    private bool IsSolid(int x, int y, int z)
    {
        if (y < 0)
            return true;
        if (y >= world.WorldHeight)
            return false;
        if (x >= 0 && x < world.ChunkSize && z >= 0 && z < world.ChunkSize)
            return blocks[x, y, z] != 0 && blocks[x, y, z] != ItemTypes.Torch;

        int worldX = Coordinate.x * world.ChunkSize + x;
        int worldZ = Coordinate.y * world.ChunkSize + z;
        Vector2Int neighborCoordinate = new Vector2Int(
            Mathf.FloorToInt((float)worldX / world.ChunkSize),
            Mathf.FloorToInt((float)worldZ / world.ChunkSize));
        return world.Chunks.TryGetValue(neighborCoordinate, out chunk neighbor) &&
            neighbor.blocks[worldX - neighborCoordinate.x * world.ChunkSize, y, worldZ - neighborCoordinate.y * world.ChunkSize] != 0 &&
            neighbor.blocks[worldX - neighborCoordinate.x * world.ChunkSize, y, worldZ - neighborCoordinate.y * world.ChunkSize] != ItemTypes.Torch;
    }

    private void RecalculateHighestBlockY()
    {
        highestBlockY = 0;
        for (int y = world.WorldHeight - 1; y > 0; y--)
        {
            for (int x = 0; x < world.ChunkSize; x++)
            {
                for (int z = 0; z < world.ChunkSize; z++)
                {
                    if (blocks[x, y, z] == 0)
                        continue;

                    highestBlockY = y;
                    return;
                }
            }
        }
    }

}
