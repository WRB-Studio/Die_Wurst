using UnityEngine;

public class LaneThrownObject : MonoBehaviour
{
    [SerializeField] private float conveyorSpeed = 3f;
    [SerializeField] private float destroyX = -20f;

    private Rigidbody body;
    private Vector3 throwStartPosition;
    private Vector3 lanePosition;
    private float throwDuration;
    private float throwArcHeight;
    private float throwProgress;
    private bool isOnLane;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 startPosition, Vector3 targetLanePosition, float targetThrowDuration, float targetThrowArcHeight, float laneMoveSpeed, float destroyAtX)
    {
        throwStartPosition = startPosition;
        lanePosition = targetLanePosition;
        throwDuration = Mathf.Max(0.01f, targetThrowDuration);
        throwArcHeight = targetThrowArcHeight;
        throwProgress = 0f;
        conveyorSpeed = laneMoveSpeed;
        destroyX = destroyAtX;
        isOnLane = false;

        if (body != null)
        {
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (!isOnLane)
        {
            UpdateThrow();
            return;
        }

        MoveOnLane();
    }

    private void UpdateThrow()
    {
        throwProgress += Time.deltaTime / throwDuration;

        if (throwProgress >= 1f)
        {
            AttachToLane();
            return;
        }

        float progress = Mathf.Clamp01(throwProgress);
        Vector3 flatPosition = Vector3.Lerp(throwStartPosition, lanePosition, progress);
        float arcOffset = 4f * throwArcHeight * progress * (1f - progress);

        transform.position = flatPosition + Vector3.up * arcOffset;
    }

    private void AttachToLane()
    {
        isOnLane = true;
        transform.position = lanePosition;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void MoveOnLane()
    {
        Vector3 position = transform.position;
        position.x -= conveyorSpeed * Time.deltaTime;
        transform.position = position;

        if (position.x <= destroyX)
        {
            Destroy(gameObject);
        }
    }
}
