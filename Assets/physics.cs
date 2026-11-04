using UnityEngine;

public static class VoxelPhysics
{
    public static bool TryGetMobTarget(
        Camera playerCamera,
        Transform player,
        float range,
        out hostileMob target)
    {
        target = null;
        if (playerCamera == null || player == null)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int layerMask = UnityEngine.Physics.DefaultRaycastLayers & ~(1 << player.gameObject.layer);
        if (!UnityEngine.Physics.Raycast(ray, out RaycastHit hit, range, layerMask, QueryTriggerInteraction.Ignore))
            return false;

        target = hit.collider.GetComponentInParent<hostileMob>();
        return target != null;
    }

    public static bool TryGetTarget(
        Camera playerCamera,
        Transform player,
        out Vector3Int hitBlock,
        out Vector3Int adjacentBlock,
        out Vector3 hitPoint)
    {
        hitBlock = Vector3Int.zero;
        adjacentBlock = Vector3Int.zero;
        hitPoint = Vector3.zero;
        if (playerCamera == null || player == null)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int layerMask = UnityEngine.Physics.DefaultRaycastLayers & ~(1 << player.gameObject.layer);
        if (!UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 6f, layerMask, QueryTriggerInteraction.Ignore))
            return false;

        hitBlock = Vector3Int.FloorToInt(hit.point - hit.normal * 0.02f);
        adjacentBlock = hitBlock + Vector3Int.RoundToInt(hit.normal);
        hitPoint = hit.point;
        return true;
    }

    public static bool BlockIntersectsPlayer(Vector3Int blockPosition, CharacterController controller)
    {
        if (controller == null)
            return false;

        Bounds blockBounds = new Bounds((Vector3)blockPosition + Vector3.one * 0.5f, Vector3.one * 0.9f);
        return controller.bounds.Intersects(blockBounds);
    }
}
