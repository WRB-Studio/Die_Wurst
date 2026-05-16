using UnityEngine;

public class ChefPatrol : MonoBehaviour
{
    [Header("Movement Range")]
    [SerializeField] private float minX = -6f;
    [SerializeField] private float maxX = 6f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arrivalDistance = 0.05f;
    [SerializeField] private float minMoveDistance = 1.5f;

    [Header("Waiting")]
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;
    [SerializeField] private bool startWithRandomWait = true;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    private float targetX;
    private float waitTimer;
    private bool isWaiting;

    public bool IsWaiting => isWaiting;

    private void Start()
    {
        AudioManager.Instance.PlaySFX("sfx_running02", true); // Spielt den ersten Soundeffekt (z.B. Schritte) ab
        targetX = GetRandomTargetX();

        if (startWithRandomWait)
        {
            StartWaiting();
        }
    }

    private void Update()
    {
        if (isWaiting)
        {
            UpdateWaiting();
            return;
        }

        MoveToTarget();
    }

    private void UpdateWaiting()
    {
        waitTimer -= Time.deltaTime;

        if (waitTimer > 0f)
        {
            return;
        }

        isWaiting = false;
        targetX = GetRandomTargetX();
    }

    private void MoveToTarget()
    {
        Vector3 position = transform.position;
        position.x = Mathf.MoveTowards(position.x, targetX, moveSpeed * Time.deltaTime);
        transform.position = position;

        if (Mathf.Abs(position.x - targetX) <= arrivalDistance)
        {
            StartWaiting();
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    private float GetRandomTargetX()
    {
        float currentX = transform.position.x;
        float moveDistance = Random.Range(minMoveDistance, Mathf.Max(minMoveDistance, maxX - minX));
        float direction = Random.value < 0.5f ? -1f : 1f;
        float nextTargetX = currentX + moveDistance * direction;

        if (nextTargetX < minX || nextTargetX > maxX)
        {
            nextTargetX = currentX - moveDistance * direction;
        }

        return Mathf.Clamp(nextTargetX, minX, maxX);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Vector3 from = new Vector3(minX, transform.position.y, transform.position.z);
        Vector3 to = new Vector3(maxX, transform.position.y, transform.position.z);
        Gizmos.DrawLine(from, to);

        Gizmos.color = Color.red;
        Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);
        Gizmos.DrawSphere(targetPosition, 0.15f);
    }
}
