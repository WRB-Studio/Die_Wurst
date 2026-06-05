using UnityEngine;

public class SurvivalSausage : MonoBehaviour
{
    [SerializeField] private float runningThreshold = 0.01f;
    [SerializeField] private Transform facingTransform;

    private SurvivalEndGame owner;
    private Vector3 moveVelocity;
    private Animator cachedAnimator;
    private SpriteRenderer cachedSpriteRenderer;
    private Vector3 facingBaseScale = Vector3.one;

    public bool IsMainSausage { get; private set; }
    public int InitialSortingOrder => cachedSpriteRenderer != null ? cachedSpriteRenderer.sortingOrder : 0;

    private void Awake()
    {
        cachedAnimator = GetComponentInChildren<Animator>();
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (facingTransform == null)
        {
            facingTransform = transform;
        }

        facingBaseScale = facingTransform.localScale;
    }

    public void Initialize(SurvivalEndGame gameOwner, bool isMainSausage)
    {
        owner = gameOwner;
        IsMainSausage = isMainSausage;
        SetRunning(false);
    }

    public bool TryHitByBird(SurvivalBird bird)
    {
        return owner != null && owner.HandleBirdHit(this, bird);
    }

    public void EnterGroundMode()
    {
        transform.localRotation = Quaternion.identity;
        SetFacingDirection(1f);
        SetRunning(false);
    }

    public void SetLocalPositionImmediate(Vector3 localPosition)
    {
        transform.localPosition = localPosition;
        SetRunning(false);
    }

    public void MoveLocalPosition(Vector3 targetLocalPosition, float smoothTime)
    {
        Vector3 currentPosition = transform.localPosition;
        Vector3 nextPosition = Vector3.SmoothDamp(
            currentPosition,
            targetLocalPosition,
            ref moveVelocity,
            Mathf.Max(0.01f, smoothTime));
        transform.localPosition = nextPosition;
        float horizontalDelta = nextPosition.x - currentPosition.x;

        if (Mathf.Abs(horizontalDelta) > runningThreshold)
        {
            SetFacingDirection(horizontalDelta);
        }

        SetRunning((nextPosition - currentPosition).sqrMagnitude > runningThreshold * runningThreshold);
    }

    public void SetFacingDirection(float horizontalDirection)
    {
        if (facingTransform == null || Mathf.Abs(horizontalDirection) <= 0.001f)
        {
            return;
        }

        Vector3 localScale = facingBaseScale;
        localScale.x = Mathf.Abs(facingBaseScale.x) * (horizontalDirection < 0f ? -1f : 1f);
        facingTransform.localScale = localScale;
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
        if (cachedAnimator == null)
        {
            return;
        }

        cachedAnimator.enabled = isRunning;
    }
}
