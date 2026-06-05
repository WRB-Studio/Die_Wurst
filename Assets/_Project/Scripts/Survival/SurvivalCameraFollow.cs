using UnityEngine;

public class SurvivalCameraFollow : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform target;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 6f;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.9f);

    public void SetCamera(Camera cameraToFollow)
    {
        targetCamera = cameraToFollow;
    }

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    private void LateUpdate()
    {
        if (target == null || targetCamera == null)
        {
            return;
        }

        Vector3 position = targetCamera.transform.position;
        position.y = Mathf.Clamp(target.position.y, minY, maxY);
        targetCamera.transform.position = position;
    }

    private void OnValidate()
    {
        maxY = Mathf.Max(minY, maxY);

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Camera cameraComponent = targetCamera;

        if (cameraComponent == null || !cameraComponent.orthographic)
        {
            return;
        }

        Vector3 center = cameraComponent.transform.position;
        float halfHeight = cameraComponent.orthographicSize;
        float halfWidth = halfHeight * cameraComponent.aspect;

        Gizmos.color = gizmoColor;
        DrawCameraBounds(new Vector3(center.x, minY, center.z), halfWidth, halfHeight);
        DrawCameraBounds(new Vector3(center.x, maxY, center.z), halfWidth, halfHeight);
        Gizmos.DrawLine(
            new Vector3(center.x, minY, center.z),
            new Vector3(center.x, maxY, center.z));
    }

    private void DrawCameraBounds(Vector3 center, float halfWidth, float halfHeight)
    {
        Vector3 topLeft = new Vector3(center.x - halfWidth, center.y + halfHeight, center.z);
        Vector3 topRight = new Vector3(center.x + halfWidth, center.y + halfHeight, center.z);
        Vector3 bottomLeft = new Vector3(center.x - halfWidth, center.y - halfHeight, center.z);
        Vector3 bottomRight = new Vector3(center.x + halfWidth, center.y - halfHeight, center.z);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
