using UnityEngine;

public class ChefThrowSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChefPatrol chefPatrol;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private Transform[] laneTargets;
    [SerializeField] private GameObject[] throwablePrefabs;

    [Header("Timing")]
    [SerializeField] private float minThrowInterval = 0.4f;
    [SerializeField] private float maxThrowInterval = 1.2f;

    [Header("Throw")]
    [SerializeField] private float minTargetXOffset = -1f;
    [SerializeField] private float maxTargetXOffset = 1f;
    [SerializeField] private float targetYOffset = 0f;
    [SerializeField] private float throwHeightOffset = 1.5f;
    [SerializeField] private float throwDuration = 0.45f;

    [Header("Conveyor")]
    [SerializeField] private float laneMoveSpeed = 3f;
    [SerializeField] private float destroyAtX = -20f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    private float throwTimer;
    private bool wasWaitingLastFrame;

    private void Awake()
    {
        if (chefPatrol == null)
        {
            chefPatrol = GetComponent<ChefPatrol>();
        }

        if (throwOrigin == null)
        {
            throwOrigin = transform;
        }
    }

    private void OnEnable()
    {
        ResetThrowTimer();
    }

    private void Update()
    {
        if (chefPatrol == null)
        {
            wasWaitingLastFrame = false;
            return;
        }

        bool isWaiting = chefPatrol.IsWaiting;

        if (isWaiting && !wasWaitingLastFrame)
        {
            ResetThrowTimer();
        }

        wasWaitingLastFrame = isWaiting;

        if (!isWaiting)
        {
            return;
        }

        if (laneTargets == null || laneTargets.Length == 0 || throwablePrefabs == null || throwablePrefabs.Length == 0)
        {
            return;
        }

        throwTimer -= Time.deltaTime;

        if (throwTimer > 0f)
        {
            return;
        }

        ThrowRandomObject();
        ResetThrowTimer();
    }

    private void ThrowRandomObject()
    {
        GameObject prefab = GetRandomPrefab();
        Transform laneTarget = GetRandomLaneTarget();

        if (prefab == null || laneTarget == null)
        {
            return;
        }

        Vector3 spawnPosition = throwOrigin.position;
        GameObject instance = Instantiate(prefab, spawnPosition, prefab.transform.rotation);
        Vector3 targetPosition = GetTargetPosition(laneTarget);
        LaneThrownObject laneThrownObject = instance.GetComponent<LaneThrownObject>();

        if (laneThrownObject == null)
        {
            laneThrownObject = instance.AddComponent<LaneThrownObject>();
        }

        laneThrownObject.Initialize(targetPosition, throwDuration, laneMoveSpeed, destroyAtX);

        Rigidbody body = instance.GetComponent<Rigidbody>();

        if (body == null)
        {
            return;
        }

        body.linearVelocity = CalculateThrowVelocity(spawnPosition, targetPosition);
    }

    private Vector3 GetTargetPosition(Transform laneTarget)
    {
        Vector3 targetPosition = laneTarget.position;
        targetPosition.x += Random.Range(minTargetXOffset, maxTargetXOffset);
        targetPosition.y += targetYOffset;
        return targetPosition;
    }

    private Vector3 CalculateThrowVelocity(Vector3 startPosition, Vector3 targetPosition)
    {
        Vector3 flatDelta = targetPosition - startPosition;
        flatDelta.y = 0f;

        float safeDuration = Mathf.Max(throwDuration, 0.05f);
        Vector3 horizontalVelocity = flatDelta / safeDuration;
        float verticalVelocity = ((targetPosition.y + throwHeightOffset) - startPosition.y) / safeDuration;

        return new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
    }

    private GameObject GetRandomPrefab()
    {
        int index = Random.Range(0, throwablePrefabs.Length);
        return throwablePrefabs[index];
    }

    private Transform GetRandomLaneTarget()
    {
        int index = Random.Range(0, laneTargets.Length);
        return laneTargets[index];
    }

    private void ResetThrowTimer()
    {
        throwTimer = Random.Range(minThrowInterval, maxThrowInterval);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos || throwOrigin == null || laneTargets == null)
        {
            return;
        }

        Gizmos.color = Color.green;

        foreach (Transform laneTarget in laneTargets)
        {
            if (laneTarget == null)
            {
                continue;
            }

            Gizmos.DrawLine(throwOrigin.position, GetTargetPosition(laneTarget));
        }
    }
}
