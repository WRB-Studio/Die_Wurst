using UnityEngine;

public class LaneThrownObject : MonoBehaviour
{
    private enum MovementState
    {
        Throwing,
        OnLane,
        FallingIntoShredder
    }

    private float conveyorSpeed;
    [SerializeField] private float destroyX = -20f;
    [SerializeField] private float shredderPullDistance = 0.4f;
    [SerializeField] private float shredderDropDistance = 2.5f;
    [SerializeField] private float shredderFallSpeed = 5f;

    private Rigidbody body;
    private Vector3 throwStartPosition;
    private Vector3 lanePosition;
    private float throwDuration;
    private float throwArcHeight;
    private float throwProgress;
    private bool hasFollowUpThrow;
    private Vector3 followUpTargetPosition;
    private float followUpThrowDuration;
    private float followUpThrowArcHeight;
    private System.Action onIntermediateThrowComplete;
    private MovementState movementState;
    private Vector3 shredderTargetPosition;
    private CollectableSausage collectableSausage;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        collectableSausage = GetComponent<CollectableSausage>();
    }

    public void Initialize(
        Vector3 startPosition,
        Vector3 targetLanePosition,
        float targetThrowDuration,
        float targetThrowArcHeight,
        float laneMoveSpeed,
        float destroyAtX,
        float targetShredderPullDistance,
        float targetShredderDropDistance,
        float targetShredderFallSpeed)
    {
        throwStartPosition = startPosition;
        lanePosition = targetLanePosition;
        throwDuration = Mathf.Max(0.01f, targetThrowDuration);
        throwArcHeight = targetThrowArcHeight;
        throwProgress = 0f;
        conveyorSpeed = laneMoveSpeed;
        destroyX = destroyAtX;
        shredderPullDistance = targetShredderPullDistance;
        shredderDropDistance = targetShredderDropDistance;
        shredderFallSpeed = targetShredderFallSpeed;
        movementState = MovementState.Throwing;
        shredderTargetPosition = Vector3.zero;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        hasFollowUpThrow = false;
        onIntermediateThrowComplete = null;
    }

    public void InitializeWithImpact(
        Vector3 startPosition,
        Vector3 impactPosition,
        Vector3 targetLanePosition,
        float totalThrowDuration,
        float targetThrowArcHeight,
        float laneMoveSpeed,
        float destroyAtX,
        float targetShredderPullDistance,
        float targetShredderDropDistance,
        float targetShredderFallSpeed,
        System.Action impactReachedCallback)
    {
        float firstThrowDuration = Mathf.Max(0.01f, totalThrowDuration * 0.55f);
        float secondThrowDuration = Mathf.Max(0.01f, totalThrowDuration - firstThrowDuration);

        Initialize(
            startPosition,
            impactPosition,
            firstThrowDuration,
            targetThrowArcHeight,
            laneMoveSpeed,
            destroyAtX,
            targetShredderPullDistance,
            targetShredderDropDistance,
            targetShredderFallSpeed);

        hasFollowUpThrow = true;
        followUpTargetPosition = targetLanePosition;
        followUpThrowDuration = secondThrowDuration;
        followUpThrowArcHeight = Mathf.Max(0.1f, targetThrowArcHeight * 0.35f);
        onIntermediateThrowComplete = impactReachedCallback;
    }

    public void ResumeOnLane(Vector3 startPosition)
    {
        throwStartPosition = startPosition;
        lanePosition = startPosition;
        throwProgress = 0f;
        movementState = MovementState.OnLane;
        shredderTargetPosition = Vector3.zero;
        transform.position = startPosition;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.position = startPosition;
        }
    }

    private void Update()
    {
        if (movementState == MovementState.Throwing)
        {
            UpdateThrow();
            return;
        }

        if (movementState == MovementState.OnLane)
        {
            MoveOnLane();
            return;
        }

        UpdateShredderFall();
    }

    private void UpdateThrow()
    {
        throwProgress += Time.deltaTime / throwDuration;

        if (throwProgress >= 1f)
        {
            if (hasFollowUpThrow)
            {
                StartFollowUpThrow();
                return;
            }

            AttachToLane();
            return;
        }

        float progress = Mathf.Clamp01(throwProgress);
        Vector3 flatPosition = Vector3.Lerp(throwStartPosition, lanePosition, progress);
        float arcOffset = 4f * throwArcHeight * progress * (1f - progress);

        transform.position = flatPosition + Vector3.up * arcOffset;
    }

    private void StartFollowUpThrow()
    {
        transform.position = lanePosition;
        onIntermediateThrowComplete?.Invoke();

        throwStartPosition = lanePosition;
        lanePosition = followUpTargetPosition;
        throwDuration = followUpThrowDuration;
        throwArcHeight = followUpThrowArcHeight;
        throwProgress = 0f;
        hasFollowUpThrow = false;
        onIntermediateThrowComplete = null;

        if (body != null)
        {
            body.position = throwStartPosition;
        }
    }

    private void AttachToLane()
    {
        movementState = MovementState.OnLane;
        transform.position = lanePosition;

        if (body != null)
        {
            body.position = lanePosition;
        }
    }

    private void MoveOnLane()
    {
        if (GameHandler.Instance != null)
        {
            conveyorSpeed = GameHandler.Instance.GetConveyorSpeed();
        }

        Vector3 position = transform.position;
        position.x -= conveyorSpeed * Time.deltaTime;
        transform.position = position;

        if (position.x <= destroyX)
        {
            StartShredderFall();
        }
    }

    private void StartShredderFall()
    {
        movementState = MovementState.FallingIntoShredder;
        shredderTargetPosition = transform.position + new Vector3(-shredderPullDistance, -shredderDropDistance, 0f);
        AudioManager.Instance?.PlayGrinderSfx(0.7f);

        if (collectableSausage != null)
        {
            AudioManager.Instance?.PlayHitOuchSfx(0.7f);
        }
    }

    private void UpdateShredderFall()
    {
        transform.position = Vector3.MoveTowards(transform.position, shredderTargetPosition, shredderFallSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, shredderTargetPosition) <= 0.01f)
        {
            Destroy(gameObject);
        }
    }
}
