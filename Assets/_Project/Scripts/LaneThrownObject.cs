using UnityEngine;

public class LaneThrownObject : MonoBehaviour
{
    [SerializeField] private float conveyorSpeed = 3f;
    [SerializeField] private float destroyX = -20f;

    private Rigidbody body;
    private Vector3 lanePosition;
    private float attachTimer;
    private bool isOnLane;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 targetLanePosition, float laneAttachDelay, float laneMoveSpeed, float destroyAtX)
    {
        lanePosition = targetLanePosition;
        attachTimer = Mathf.Max(0f, laneAttachDelay);
        conveyorSpeed = laneMoveSpeed;
        destroyX = destroyAtX;
        isOnLane = false;
    }

    private void Update()
    {
        if (!isOnLane)
        {
            UpdateAttachTimer();
            return;
        }

        MoveOnLane();
    }

    private void UpdateAttachTimer()
    {
        attachTimer -= Time.deltaTime;

        if (attachTimer > 0f)
        {
            return;
        }

        AttachToLane();
    }

    private void AttachToLane()
    {
        isOnLane = true;
        transform.position = lanePosition;

        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
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
