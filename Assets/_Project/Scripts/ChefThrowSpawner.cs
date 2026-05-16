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
    [SerializeField] private float throwDuration = 0.45f;
    [SerializeField] private float throwArcHeight = 2f;

    [Header("Conveyor")]
    [SerializeField] private float laneMoveSpeed = 3f;
    [SerializeField] private float destroyAtX = -20f;

    [Header("Shredder")]
    [SerializeField] private float shredderPullDistance = 0.4f;
    [SerializeField] private float shredderDropDistance = 2.5f;
    [SerializeField] private float shredderFallSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private bool logThrowSpawn = false;

    private float throwTimer;
    private bool wasWaitingLastFrame;

    private void Awake()
    {
        if (chefPatrol == null)
        {
            chefPatrol = GetComponent<ChefPatrol>();
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

        Transform currentThrowOrigin = GetThrowOrigin();
        Vector3 spawnPosition = currentThrowOrigin.position;
        Quaternion spawnRotation = prefab.transform.rotation;
        GameObject instance = Instantiate(prefab);
        instance.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        Vector3 targetPosition = GetTargetPosition(laneTarget);
        LaneThrownObject laneThrownObject = instance.GetComponent<LaneThrownObject>();

        if (laneThrownObject == null)
        {
            laneThrownObject = instance.AddComponent<LaneThrownObject>();
        }

        AudioManager.Instance.PlaySFX("sfx_knife", false); 

        laneThrownObject.Initialize(
            spawnPosition,
            targetPosition,
            throwDuration,
            throwArcHeight,
            laneMoveSpeed,
            destroyAtX,
            shredderPullDistance,
            shredderDropDistance,
            shredderFallSpeed);

        if (logThrowSpawn)
        {
            Debug.Log($"Chef throw spawn from {spawnPosition} to {targetPosition}", this);
        }
    }

    private Vector3 GetTargetPosition(Transform laneTarget)
    {
        Vector3 targetPosition = laneTarget.position;
        targetPosition.x += Random.Range(minTargetXOffset, maxTargetXOffset);
        targetPosition.y += targetYOffset;
        return targetPosition;
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

    private Transform GetThrowOrigin()
    {
        if (throwOrigin != null)
        {
            return throwOrigin;
        }

        if (chefPatrol != null)
        {
            return chefPatrol.transform;
        }

        return transform;
    }

    private void OnDrawGizmosSelected()
    {
        Transform currentThrowOrigin = GetThrowOrigin();

        if (!drawDebugGizmos || currentThrowOrigin == null || laneTargets == null)
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

            Gizmos.DrawLine(currentThrowOrigin.position, laneTarget.position);
        }
    }
}
