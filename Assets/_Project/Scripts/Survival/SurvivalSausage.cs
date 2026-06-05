using UnityEngine;

public class SurvivalSausage : MonoBehaviour
{
    [SerializeField] private float runningThreshold = 0.01f;

    private SurvivalEndGame owner;
    private Vector3 moveVelocity;
    private Animator cachedAnimator;
    private SpriteRenderer cachedSpriteRenderer;
    private Transform facingVisualTransform;
    private Transform bounceVisualTransform;
    private Vector3 facingBaseScale = Vector3.one;
    private Vector3 bounceBaseLocalPosition = Vector3.zero;
    private Vector3 movementLocalPosition = Vector3.zero;
    private bool isRunning;
    private bool isGroundMode;
    private float bounceTimer;
    private float groundBounceHeight = 0.08f;
    private float groundBounceSpeed = 8f;

    public bool IsMainSausage { get; private set; }
    public int InitialSortingOrder => cachedSpriteRenderer != null ? cachedSpriteRenderer.sortingOrder : 0;

    private void Awake()
    {
        cachedAnimator = GetComponentInChildren<Animator>();
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        facingVisualTransform = cachedSpriteRenderer != null ? cachedSpriteRenderer.transform : transform;
        bounceVisualTransform = facingVisualTransform != null && facingVisualTransform != transform
            ? facingVisualTransform
            : null;
        facingBaseScale = facingVisualTransform.localScale;
        bounceBaseLocalPosition = bounceVisualTransform != null ? bounceVisualTransform.localPosition : Vector3.zero;
        movementLocalPosition = transform.localPosition;
    }

    public void Initialize(SurvivalEndGame gameOwner, bool isMainSausage)
    {
        owner = gameOwner;
        IsMainSausage = isMainSausage;
        isGroundMode = false;
        bounceTimer = 0f;
        movementLocalPosition = transform.localPosition;
        UpdateBounceRestState();
        SetRunning(false);
    }

    public bool TryHitByBird(SurvivalBird bird)
    {
        return owner != null && owner.HandleBirdHit(this, bird);
    }

    public void ConfigureGroundBounce(float bounceHeight, float bounceSpeed)
    {
        groundBounceHeight = Mathf.Max(0f, bounceHeight);
        groundBounceSpeed = Mathf.Max(0.01f, bounceSpeed);
    }

    public void EnterGroundMode()
    {
        isGroundMode = true;
        transform.localRotation = Quaternion.identity;
        SetFacingDirection(1f);
        SetRunning(false);
    }

    public void SetLocalPositionImmediate(Vector3 localPosition)
    {
        movementLocalPosition = localPosition;
        ApplyMovementPosition();
        SetRunning(false);
    }

    public void SetWorldPositionImmediate(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        movementLocalPosition = transform.localPosition;
        ApplyMovementPosition();
    }

    public void MoveLocalPosition(Vector3 targetLocalPosition, float smoothTime)
    {
        Vector3 currentPosition = movementLocalPosition;
        Vector3 nextPosition = Vector3.SmoothDamp(
            currentPosition,
            targetLocalPosition,
            ref moveVelocity,
            Mathf.Max(0.01f, smoothTime));
        movementLocalPosition = nextPosition;
        ApplyMovementPosition();
        float horizontalDelta = nextPosition.x - currentPosition.x;

        if (Mathf.Abs(horizontalDelta) > runningThreshold)
        {
            SetFacingDirection(horizontalDelta);
        }

        float thresholdSquared = runningThreshold * runningThreshold;
        bool isStillMoving = (nextPosition - currentPosition).sqrMagnitude > thresholdSquared;
        bool isStillChasingTarget = (targetLocalPosition - nextPosition).sqrMagnitude > thresholdSquared;
        SetRunning(isStillMoving || isStillChasingTarget);
    }

    public void SetFacingDirection(float horizontalDirection)
    {
        if (facingVisualTransform == null || Mathf.Abs(horizontalDirection) <= 0.001f)
        {
            return;
        }

        Vector3 localScale = facingBaseScale;
        localScale.x = Mathf.Abs(facingBaseScale.x) * (horizontalDirection < 0f ? -1f : 1f);
        facingVisualTransform.localScale = localScale;
    }

    public void Consume()
    {
        Destroy(gameObject);
    }

    public void SetSortingOrder(int sortingOrder)
    {
        if (cachedSpriteRenderer == null)
        {
            return;
        }

        cachedSpriteRenderer.sortingOrder = sortingOrder;
    }

    public void SetRunning(bool isRunning)
    {
        this.isRunning = isRunning;

        if (cachedAnimator == null)
        {
            UpdateBounceRestState();
            return;
        }

        cachedAnimator.enabled = isRunning;
        UpdateBounceRestState();
    }

    private void UpdateBounceRestState()
    {
        if (isRunning)
        {
            return;
        }

        bounceTimer = 0f;
        ApplyMovementPosition();
    }

    private void ApplyMovementPosition()
    {
        float bounceOffset = 0f;

        if (isGroundMode && !IsMainSausage && isRunning)
        {
            bounceTimer += Time.deltaTime * Mathf.Max(0.01f, groundBounceSpeed);
            bounceOffset = Mathf.Abs(Mathf.Sin(bounceTimer)) * Mathf.Max(0f, groundBounceHeight);
        }

        if (bounceVisualTransform != null)
        {
            transform.localPosition = movementLocalPosition;
            Vector3 bouncePosition = bounceBaseLocalPosition;
            bouncePosition.y += bounceOffset;
            bounceVisualTransform.localPosition = bouncePosition;
            return;
        }

        Vector3 finalPosition = movementLocalPosition;
        finalPosition.y += bounceOffset;
        transform.localPosition = finalPosition;
    }
}
