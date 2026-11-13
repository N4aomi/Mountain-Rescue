using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BuildingSafeZone : MonoBehaviour
{
    private void Reset()
    {
        BoxCollider zoneCollider = GetComponent<BoxCollider>();
        zoneCollider.isTrigger = true;
    }
}
