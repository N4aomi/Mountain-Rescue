
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuildingStatus : MonoBehaviour
{
    private readonly HashSet<BuildingSafeZone> activeZones =
        new HashSet<BuildingSafeZone>();

    public bool IsInsideBuilding => activeZones.Count > 0;

    private void OnTriggerEnter(Collider other)
    {
        BuildingSafeZone zone =
            other.GetComponentInParent<BuildingSafeZone>();

        if (zone != null)
        {
            activeZones.Add(zone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BuildingSafeZone zone =
            other.GetComponentInParent<BuildingSafeZone>();

        if (zone != null)
        {
            activeZones.Remove(zone);
        }
    }
}
