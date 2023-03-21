using UnityEngine;
using UnityEngine.AI;

public static class Helpers
{
    public static Quaternion TransformLocalRotationToQuaternion(Transform transform)
    {
        Quaternion result = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);
        return result;
    }

    public static bool IsLocationPathable(Transform targetLocation, float? searchRadius = 3f)
    {
        if (NavMesh.SamplePosition(targetLocation.position, out NavMeshHit target, (float)searchRadius, NavMesh.AllAreas))
        {
            return target.hit;
        }

        return false;
    }

    public static bool IsObjectLayerInLayerMask(GameObject obj, LayerMask layerMask)
    {
        int objectLayerMaskToInt = (1 << obj.layer);

        if ((layerMask.value & objectLayerMaskToInt) > 0) return true;
        else return false;
    }
}
