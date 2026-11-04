using System.Collections.Generic;
using UnityEngine;

public static class WorldStructures
{
    private const int WitchHutSpacing = 17;
    private const int DungeonSpacing = 19;
    private const int LegacyGraveyardSpacing = 13;
    private static Material graveMarkerMaterial;

    public static bool GenerateChunk(
        world currentWorld,
        Vector2Int coordinate,
        int[,,] blocks,
        bool preserveExistingBlocks = false)
    {
        bool changed = false;
        for (int x = coordinate.x - 1; x <= coordinate.x + 1; x++)
        {
            for (int z = coordinate.y - 1; z <= coordinate.y + 1; z++)
            {
                if (preserveExistingBlocks &&
                    TryGetLegacyGraveyardLocation(x, z, out Vector2Int oldGraveyard))
                {
                    int oldGroundY = currentWorld.GetHeight(oldGraveyard.x, oldGraveyard.y);
                    changed |= ClearLegacyCemetery(
                        coordinate,
                        blocks,
                        oldGraveyard,
                        oldGroundY);
                }
                if (TryGetLocation(x, z, WitchHutSpacing, 0x4a17, true, out Vector2Int hut))
                    changed |= BuildHut(currentWorld, coordinate, blocks, hut, preserveExistingBlocks);
                if (TryGetLocation(x, z, DungeonSpacing, 0x61d3, false, out Vector2Int dungeon))
                    changed |= BuildDungeon(currentWorld, coordinate, blocks, dungeon, preserveExistingBlocks);
            }
        }
        return changed;
    }

    public static List<Vector3> GetNearbyWitchHuts(Vector3 position, int radiusChunks)
    {
        List<Vector3> found = new List<Vector3>();
        CollectNearbyWitchHuts(position, radiusChunks, found);
        return found;
    }

    public static void CollectNearbyWitchHuts(Vector3 position, int radiusChunks, List<Vector3> found)
    {
        FindNearby(position, radiusChunks, WitchHutSpacing, 0x4a17, true, found);
    }

    public static List<Vector3> GetNearbyDungeons(Vector3 position, int radiusChunks)
    {
        List<Vector3> found = new List<Vector3>();
        CollectNearbyDungeons(position, radiusChunks, found);
        return found;
    }

    public static void CollectNearbyDungeons(Vector3 position, int radiusChunks, List<Vector3> found)
    {
        FindNearby(position, radiusChunks, DungeonSpacing, 0x61d3, false, found);
    }

    public static void CreateChunkGraveModels(
        world currentWorld,
        Vector2Int coordinate,
        Transform parent)
    {
        int chunkHash = unchecked(coordinate.x * 73856093 ^ coordinate.y * 19349663 ^ 0x319d);
        System.Random random = new System.Random(chunkHash);
        int graveCount = random.Next(2, 4);
        for (int i = 0; i < graveCount; i++)
        {
            bool created = false;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int worldX = coordinate.x * world.ChunkSize + random.Next(2, world.ChunkSize - 2);
                int worldZ = coordinate.y * world.ChunkSize + random.Next(2, world.ChunkSize - 2);
                if (NearStructure(worldX, worldZ))
                    continue;

                int groundY = currentWorld.GetHeight(worldX, worldZ);
                float yaw = random.Next(0, 4) * 90f;
                CreateGraveMarker(
                    parent,
                    new Vector3(
                        worldX - coordinate.x * world.ChunkSize + 0.5f,
                        groundY + 1.02f,
                        worldZ - coordinate.y * world.ChunkSize + 0.5f),
                    yaw);
                created = true;
                break;
            }
            if (!created)
                break;
        }
    }

    private static bool NearStructure(int worldX, int worldZ)
    {
        int chunkX = Mathf.FloorToInt((float)worldX / world.ChunkSize);
        int chunkZ = Mathf.FloorToInt((float)worldZ / world.ChunkSize);
        for (int x = chunkX - 1; x <= chunkX + 1; x++)
        {
            for (int z = chunkZ - 1; z <= chunkZ + 1; z++)
            {
                if (TryGetLocation(x, z, WitchHutSpacing, 0x4a17, true, out Vector2Int hut) &&
                    (new Vector2(worldX - hut.x, worldZ - hut.y)).sqrMagnitude < 100f)
                    return true;
                if (TryGetLocation(x, z, DungeonSpacing, 0x61d3, false, out Vector2Int dungeon) &&
                    (new Vector2(worldX - dungeon.x, worldZ - dungeon.y)).sqrMagnitude < 144f)
                    return true;
            }
        }
        return false;
    }

    private static void CreateGraveMarker(Transform parent, Vector3 localPosition, float yaw)
    {
        graveMarkerMaterial = MobModels.Material(
            graveMarkerMaterial,
            "Weathered grave stone",
            new Color(0.39f, 0.4f, 0.43f));
        GameObject marker = new GameObject("Scattered grave cross");
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        MobModels.Part(
            marker.transform,
            PrimitiveType.Cube,
            "Cross shaft",
            new Vector3(0f, 0.52f, 0f),
            new Vector3(0.2f, 1.04f, 0.16f),
            graveMarkerMaterial);
        MobModels.Part(
            marker.transform,
            PrimitiveType.Cube,
            "Cross beam",
            new Vector3(0f, 0.75f, 0f),
            new Vector3(0.62f, 0.17f, 0.18f),
            graveMarkerMaterial);
        MobModels.Part(
            marker.transform,
            PrimitiveType.Cube,
            "Grave base",
            new Vector3(0f, 0.1f, 0f),
            new Vector3(0.52f, 0.18f, 0.36f),
            graveMarkerMaterial);
    }

    private static Vector2Int GetGraveyardLocation(int chunkX, int chunkZ, int hash)
    {
        System.Random random = new System.Random(hash);
        return new Vector2Int(
            chunkX * world.ChunkSize + random.Next(8, world.ChunkSize - 7),
            chunkZ * world.ChunkSize + random.Next(7, world.ChunkSize - 6));
    }

    private static bool TryGetLegacyGraveyardLocation(int chunkX, int chunkZ, out Vector2Int location)
    {
        if (chunkX == 0 && chunkZ == 1)
        {
            location = new Vector2Int(8, 24);
            return true;
        }

        unchecked
        {
            int hash = chunkX * 73856093 ^ chunkZ * 19349663 ^ 0x72b9;
            if (Mathf.Abs(hash % LegacyGraveyardSpacing) != 0)
            {
                location = Vector2Int.zero;
                return false;
            }

            location = GetGraveyardLocation(chunkX, chunkZ, hash);
            return true;
        }
    }

    private static void FindNearby(
        Vector3 position,
        int radiusChunks,
        int spacing,
        int salt,
        bool hut,
        List<Vector3> found)
    {
        found.Clear();
        int centerX = Mathf.FloorToInt(position.x / world.ChunkSize);
        int centerZ = Mathf.FloorToInt(position.z / world.ChunkSize);
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

    private static bool BuildHut(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        bool preserveExistingBlocks)
    {
        int baseY = currentWorld.GetHeight(center.x, center.y);
        if (preserveExistingBlocks &&
            !CanPlaceOnNaturalGround(currentWorld, chunkCoordinate, blocks, center, 3, baseY + 4))
            return false;

        bool changed = false;
        for (int x = -3; x <= 3; x++)
        {
            for (int z = -3; z <= 3; z++)
            {
                changed |= SetBlock(chunkCoordinate, blocks, center.x + x, baseY, center.y + z, ItemTypes.Planks);
                bool wall = Mathf.Abs(x) == 3 || Mathf.Abs(z) == 3;
                for (int y = 1; y <= 3; y++)
                {
                    bool doorway = z == -3 && x == 0 && y <= 2;
                    bool window = z == 3 && (x == -1 || x == 1) && y == 2;
                    if (wall && !doorway && !window)
                        changed |= SetBlock(
                            chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, ItemTypes.Log);
                    else if (window)
                        changed |= SetBlock(
                            chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, ItemTypes.Planks);
                    else
                        changed |= SetBlock(chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, 0);
                }

                changed |= SetBlock(
                    chunkCoordinate, blocks, center.x + x, baseY + 4, center.y + z, ItemTypes.Planks);
            }
        }

        changed |= SetBlock(chunkCoordinate, blocks, center.x, baseY + 1, center.y + 1, ItemTypes.Torch);
        changed |= SetBlock(chunkCoordinate, blocks, center.x, baseY + 1, center.y - 1, ItemTypes.CraftingTable);
        return changed;
    }

    private static bool BuildDungeon(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        bool preserveExistingBlocks)
    {
        int floorY = currentWorld.GetHeight(center.x, center.y);
        if (preserveExistingBlocks &&
            !CanPlaceOnNaturalGround(currentWorld, chunkCoordinate, blocks, center, 8, floorY + 6))
            return false;

        bool changed = false;
        CreateCorridorRoomSystem(chunkCoordinate, blocks, center, floorY, ref changed, 5);
        return changed;
    }

    private static void CreateCorridorRoomSystem(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int floorY,
        ref bool changed,
        int maxRooms)
    {
        int roomSize = 5;
        int corridorWidth = 2;
        int roomsCreated = 0;
        Vector2Int currentRoomCenter = center;

        for (int room = 0; room < maxRooms; room++)
        {
            if (room > 0)
            {
                CreateCorridor(chunkCoordinate, blocks, currentRoomCenter, center, corridorWidth, floorY, ref changed);
                currentRoomCenter = new Vector2Int(
                    currentRoomCenter.x + UnityEngine.Random.Range(-3, 3),
                    currentRoomCenter.y + UnityEngine.Random.Range(-3, 3));
            }

            int roomXMin = currentRoomCenter.x - roomSize;
            int roomXMax = currentRoomCenter.x + roomSize;
            int roomYMin = currentRoomCenter.y - roomSize;
            int roomYMax = currentRoomCenter.y + roomSize;

            for (int x = roomXMin; x <= roomXMax; x++)
            {
                for (int y = roomYMin; y <= roomYMax; y++)
                {
                    for (int z = floorY; z <= floorY + 5; z++)
                    {
                        changed |= SetBlock(chunkCoordinate, blocks, center.x + x, z, center.y + y, ItemTypes.Stone);
                    }
                }
            }

            roomsCreated++;
            if (room >= maxRooms - 1)
                break;

            bool validDirection = false;
            int attempt = 0;
            while (!validDirection && attempt < 8)
            {
                int direction = UnityEngine.Random.Range(0, 4);
                int dx = 0, dz = 0;
                switch (direction)
                {
                    case 0: dx = roomSize + corridorWidth; break;
                    case 1: dx = -(roomSize + corridorWidth); break;
                    case 2: dz = roomSize + corridorWidth; break;
                    case 3: dz = -(roomSize + corridorWidth); break;
                }

                bool overlap = false;
                for (int r = 0; r < room; r++)
                {
                    Vector2Int prevRoom = new Vector2Int(
                        UnityEngine.Random.Range(-3, 3),
                        UnityEngine.Random.Range(-3, 3));
                    Vector2Int checkPos = new Vector2Int(currentRoomCenter.x + dx, currentRoomCenter.y + dz);
                    if ((prevRoom - checkPos).sqrMagnitude < 25)
                    {
                        overlap = true;
                        break;
                    }
                }
                if (!overlap)
                {
                    validDirection = true;
                    center = new Vector2Int(center.x + dx, center.y + dz);
                }
                attempt++;
            }
            if (!validDirection) break;
        }

        for (int i = 0; i < 4; i++)
        {
            changed |= SetBlock(chunkCoordinate, blocks, center.x + (i % 2 == 0 ? i / 2 : -(i / 2)), floorY + 1, center.y + (i >= 2 ? i - 2 : 0), ItemTypes.Torch);
        }
    }

    private static void CreateCorridor(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int fromRoom,
        Vector2Int toRoom,
        int width,
        int floorY,
        ref bool changed)
    {
        int startX = fromRoom.x;
        int startY = fromRoom.y;
        int endX = toRoom.x;
        int endY = toRoom.y;

        int dx = (int)Mathf.Sign(endX - startX);
        int dy = (int)Mathf.Sign(endY - startY);

        int x = startX;
        int y = startY;

        while ((dx > 0 && x <= endX) || (dx < 0 && x >= endX) || (dx == 0) ||
               (dy > 0 && y <= endY) || (dy < 0 && y >= endY) || (dy == 0))
        {
            for (int wy = floorY; wy <= floorY + 5; wy++)
            {
                for (int wy2 = y - width/2; wy2 <= y + width/2; wy2++)
                {
                    changed |= SetBlock(chunkCoordinate, blocks, x - chunkCoordinate.x, wy, wy2 - chunkCoordinate.y, ItemTypes.Stone);
                }
            }

            if (x != endX) x += dx;
            if (y != endY) y += dy;
        }
    }

    private static bool ClearLegacyCemetery(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int groundY)
    {
        bool changed = false;
        for (int offset = -8; offset <= 8; offset += 2)
        {
            if (offset != 0)
                changed |= ClearBlockIfType(
                    chunkCoordinate, blocks, center.x + offset, groundY + 1, center.y - 8, ItemTypes.Log);
            changed |= ClearBlockIfType(
                chunkCoordinate, blocks, center.x + offset, groundY + 1, center.y + 8, ItemTypes.Log);
            changed |= ClearBlockIfType(
                chunkCoordinate, blocks, center.x - 8, groundY + 1, center.y + offset, ItemTypes.Log);
            changed |= ClearBlockIfType(
                chunkCoordinate, blocks, center.x + 8, groundY + 1, center.y + offset, ItemTypes.Log);
        }

        for (int x = -6; x <= 6; x++)
        {
            for (int z = -7; z <= 7; z++)
            {
                changed |= ClearBlockIfType(
                    chunkCoordinate,
                    blocks,
                    center.x + x,
                    groundY + 1,
                    center.y + z,
                    ItemTypes.Dirt);
            }
        }

        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x, groundY + 1, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x, groundY + 2, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x - 1, groundY + 2, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x + 1, groundY + 2, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x - 6, groundY + 1, center.y - 6, ItemTypes.Torch);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x + 6, groundY + 1, center.y - 6, ItemTypes.Torch);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x - 6, groundY + 1, center.y + 6, ItemTypes.Torch);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x + 6, groundY + 1, center.y + 6, ItemTypes.Torch);

        Vector2Int[] oldMarkers =
        {
            new Vector2Int(-4, -4), new Vector2Int(4, -4),
            new Vector2Int(-4, 0), new Vector2Int(4, 0),
            new Vector2Int(-4, 4), new Vector2Int(4, 4),
            new Vector2Int(0, 2),
            new Vector2Int(-3, -1), new Vector2Int(3, -1),
            new Vector2Int(-3, 3), new Vector2Int(3, 3),
            new Vector2Int(0, 1)
        };
        for (int i = 0; i < oldMarkers.Length; i++)
        {
            int worldX = center.x + oldMarkers[i].x;
            int worldZ = center.y + oldMarkers[i].y;
            int markerBlocks = 0;
            for (int y = groundY + 1; y <= groundY + 4; y++)
            {
                int localX = worldX - chunkCoordinate.x * world.ChunkSize;
                int localZ = worldZ - chunkCoordinate.y * world.ChunkSize;
                if (GetBlock(blocks, localX, y, localZ) == ItemTypes.Stone)
                    markerBlocks++;
            }
            if (markerBlocks < 3)
                continue;

            for (int y = groundY + 1; y <= groundY + 4; y++)
                changed |= ClearBlockIfType(
                    chunkCoordinate, blocks, worldX, y, worldZ, ItemTypes.Stone);
            int crossY = groundY + 2;
            for (int arm = -1; arm <= 1; arm += 2)
                changed |= ClearBlockIfType(
                    chunkCoordinate,
                    blocks,
                    worldX + arm,
                    crossY,
                    worldZ,
                    ItemTypes.Stone);
        }
        return changed;
    }

    private static bool ClearBlockIfType(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        int worldX,
        int y,
        int worldZ,
        int expectedBlock)
    {
        int localX = worldX - chunkCoordinate.x * world.ChunkSize;
        int localZ = worldZ - chunkCoordinate.y * world.ChunkSize;
        return GetBlock(blocks, localX, y, localZ) == expectedBlock &&
            SetBlock(chunkCoordinate, blocks, worldX, y, worldZ, 0);
    }

    private static int GetBlock(int[,,] blocks, int x, int y, int z)
    {
        return x >= 0 && x < world.ChunkSize && y >= 0 && y < world.WorldHeight &&
            z >= 0 && z < world.ChunkSize
            ? blocks[x, y, z]
            : 0;
    }

    private static bool CanPlaceOnNaturalGround(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int radius,
        int maximumY)
    {
        int chunkMinX = chunkCoordinate.x * world.ChunkSize;
        int chunkMinZ = chunkCoordinate.y * world.ChunkSize;
        for (int worldX = center.x - radius; worldX <= center.x + radius; worldX++)
        {
            for (int worldZ = center.y - radius; worldZ <= center.y + radius; worldZ++)
            {
                int localX = worldX - chunkMinX;
                int localZ = worldZ - chunkMinZ;
                if (localX < 0 || localX >= world.ChunkSize || localZ < 0 || localZ >= world.ChunkSize)
                    continue;

                int groundY = currentWorld.GetHeight(worldX, worldZ);
                if (blocks[localX, groundY, localZ] != ItemTypes.Grass)
                    return false;
                for (int y = groundY + 1; y <= maximumY && y < world.WorldHeight; y++)
                {
                    if (blocks[localX, y, localZ] != 0)
                        return false;
                }
            }
        }
        return true;
    }

    private static bool SetBlock(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        int worldX,
        int y,
        int worldZ,
        int block)
    {
        if (y < 0 || y >= world.WorldHeight)
            return false;

        int localX = worldX - chunkCoordinate.x * world.ChunkSize;
        int localZ = worldZ - chunkCoordinate.y * world.ChunkSize;
        if (localX >= 0 && localX < world.ChunkSize &&
            localZ >= 0 && localZ < world.ChunkSize &&
            blocks[localX, y, localZ] != block)
        {
            blocks[localX, y, localZ] = block;
            return true;
        }
        return false;
    }
}
