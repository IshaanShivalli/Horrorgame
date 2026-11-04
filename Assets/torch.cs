using UnityEngine;

public sealed class torch : MonoBehaviour
{
    public static GameObject CreateLight(Transform parent, Vector3 localPosition)
    {
        GameObject torchObject = new GameObject("Torch light");
        torchObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        torchObject.transform.SetParent(parent, false);
        torchObject.transform.localPosition = localPosition;
        Light torchLight = torchObject.AddComponent<Light>();
        torchLight.type = LightType.Point;
        torchLight.color = new Color(1f, 0.58f, 0.24f);
        torchLight.intensity = 1.4f;
        torchLight.range = 8f;
        torchLight.shadows = LightShadows.None;
        return torchObject;
    }
}
