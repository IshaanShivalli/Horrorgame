using System.Collections.Generic;
using UnityEngine;

public static class WorldStructures
{
    private const int WitchHutSpacing = 17;
    private const int DungeonSpacing = 19;

    public static void GenerateChunk(world currentWorld, Vector2Int coordinate, int[,,] blocks)
    {
        for (int x = coordinate.x - 1; x <= coordinate.x + 1; x++)
        {
            for (int z = coordinate.y - 1; z <= coordinate.y + 1; z++)
            {
                if (TryGetLocation(x, z, WitchHutSpacing, 0x4a17, true, out Vector2Int hut))
                    BuildHut(currentWorld, coordinate, blocks, hut);
                if (TryGetLocation(x, z, DungeonSpacing, 0x61d3, false, out Vector2Int dungeon))
                    BuildDungeon(currentWorld, coordinate, blocks, dungeon);
            }
        }
    }

    public static List<Vector3> GetNearbyWitchHuts(Vector3 position, int radiusChunks)
    {
        return FindNearby(position, radiusChunks, WitchHutSpacing, 0x4a17, true);
    }

    public static List<Vector3> GetNearbyDungeons(Vector3 position, int radiusChunks)
    {
        return FindNearby(position, radiusChunks, DungeonSpacing, 0x61d3, false);
    }

    private static List<Vector3> FindNearby(
        Vector3 position,
        int radiusChunks,
        int spacing,
        int salt,
        bool hut)
    {
        int centerX = Mathf.FloorToInt(position.x / world.ChunkSize);
        int centerZ = Mathf.FloorToInt(position.z / world.ChunkSize);
        List<Vector3> found = new List<Vector3>();
        for (int x = centerX - radiusChunks; x <= centerX + radiusChunks; x++)
        {
            for (int z = centerZ - radiusChunks; z <= centerZ + radiusChunks; z++)
            {
                if (!TryGetLocation(x, z, spacing, salt, hut, out Vector2Int location))
                    continue;

                found.Add(new Vector3(
                    location.x + 0.5f,
                    WorldHeight(location.x, location.y) + 1.02f,
                    location.y + 0.5f));
            }
        }
        return found;
    }

    private static bool TryGetLocation(
        int chunkX,
        int chunkZ,
        int spacing,
        int salt,
        bool hut,
        out Vector2Int location)
    {
        if (hut && chunkX == 1 && chunkZ == 0)
        {
            location = new Vector2Int(24, 8);
            return true;
        }

        if (!hut && chunkX == -1 && chunkZ == 1)
        {
            location = new Vector2Int(-24, 24);
            return true;
        }

        unchecked
        {
            int hash = chunkX * 73856093 ^ chunkZ * 19349663 ^ salt;
            if (Mathf.Abs(hash % spacing) != 0)
            {
                location = Vector2Int.zero;
                return false;
            }

            System.Random random = new System.Random(hash);
            int x = chunkX * world.ChunkSize + random.Next(4, world.ChunkSize - 4);
            int z = chunkZ * world.ChunkSize + random.Next(4, world.ChunkSize - 4);
            location = new Vector2Int(x, z);
            return true;
        }
    }

    private static int WorldHeight(int x, int z)
    {
        float broad = Mathf.PerlinNoise((x + 720f) * 0.035f, (z + 720f) * 0.035f);
        float detail = Mathf.PerlinNoise((x + 180f) * 0.11f, (z + 180f) * 0.11f);
        return Mathf.Clamp(5 + Mathf.RoundToInt(broad * 5f + detail * 2f), 4, world.WorldHeight - 1);
    }

    private static void BuildHut(world currentWorld, Vector2Int chunkCoordinate, int[,,] blocks, Vector2Int center)
    {
        int baseY = currentWorld.GetHeight(center.x, center.y);
        for (int x = -3; x <= 3; x++)
        {
            for (int z = -3; z <= 3; z++)
            {
                SetBlock(chunkCoordinate, blocks, center.x + x, baseY, center.y + z, ItemTypes.Planks);
                bool wall = Mathf.Abs(x) == 3 || Mathf.Abs(z) == 3;
                for (int y = 1; y <= 3; y++)
                {
                    bool doorway = z == -3 && x == 0 && y <= 2;
                    bool window = z == 3 && (x == -1 || x == 1) && y == 2;
                    if (wall && !doorway && !window)
                        SetBlock(chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, ItemTypes.Log);
                    else if (window)
                        SetBlock(chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, ItemTypes.Planks);
                }

                SetBlock(chunkCoordinate, blocks, center.x + x, baseY + 4, center.y + z, ItemTypes.Planks);
            }
        }

        SetBlock(chunkCoordinate, blocks, center.x, baseY + 1, center.y + 1, ItemTypes.Torch);
        SetBlock(chunkCoordinate, blocks, center.x, baseY + 1, center.y - 1, ItemTypes.CraftingTable);
    }

    private static void BuildDungeon(world currentWorld, Vector2Int chunkCoordinate, int[,,] blocks, Vector2Int center)
    {
        int surfaceY = currentWorld.GetHeight(center.x, center.y);
        int floorY = surfaceY;
        for (int x = -5; x <= 5; x++)
        {
            for (int z = -5; z <= 5; z++)
            {
                SetBlock(chunkCoordinate, blocks, center.x + x, floorY, center.y + z, ItemTypes.Stone);
                SetBlock(chunkCoordinate, blocks, center.x + x, floorY + 5, center.y + z, ItemTypes.Stone);
                bool wall = Mathf.Abs(x) == 5 || Mathf.Abs(z) == 5;
                for (int y = floorY + 1; y < floorY + 5; y++)
                {
                    bool doorway = x == 0 && z == -5 && y <= floorY + 3;
                    SetBlock(
                        chunkCoordinate,
                        blocks,
                        center.x + x,
                        y,
                        center.y + z,
                        wall && !doorway ? ItemTypes.Stone : 0);
                }
            }
        }

        SetBlock(chunkCoordinate, blocks, center.x, floorY + 1, center.y, ItemTypes.Torch);
        SetBlock(chunkCoordinate, blocks, center.x, floorY + 1, center.y + 3, ItemTypes.Torch);
    }

    private static void SetBlock(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        int worldX,
        int y,
        int worldZ,
        int block)
    {
        if (y < 0 || y >= world.WorldHeight)
            return;

        int localX = worldX - chunkCoordinate.x * world.ChunkSize;
        int localZ = worldZ - chunkCoordinate.y * world.ChunkSize;
        if (localX >= 0 && localX < world.ChunkSize &&
            localZ >= 0 && localZ < world.ChunkSize)
            blocks[localX, y, localZ] = block;
    }
}
