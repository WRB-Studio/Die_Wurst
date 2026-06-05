using UnityEngine;

public class SurvivalBird : MonoBehaviour
{
    [SerializeField] private float cameraDestroyPadding = 0.2f;

    private BirdController controller;
    private SpriteRenderer spriteRenderer;
    private Vector3 direction;
    private float speed;
    private bool hasResolvedHit;
    private bool shouldBeRemoved;
    private Rigidbody body;

    public void Initialize(BirdController owner, Vector3 flyDirection, float flySpeed)
    {
        controller = owner;
        direction = flyDirection.normalized;
        speed = flySpeed;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Rigidbody>();
        UpdateSpriteDirection();
    }

    public void TickMovement(float deltaTime)
    {
        Vector3 step = direction * speed * deltaTime;

        if (body != null)
        {
            body.MovePosition(body.position + step);
        }
        else
        {
            transform.position += step;
        }

        if (IsOutsideCamera())
        {
            shouldBeRemoved = true;
        }
    }

    public void StartRetreat()
    {
        direction = -direction;
        hasResolvedHit = true;
        UpdateSpriteDirection();
    }

    public void MarkForRemoval()
    {
        shouldBeRemoved = true;
    }

    public bool ShouldBeRemoved()
    {
        return shouldBeRemoved;
    }

    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = direction.x < 0f;
    }

    private bool IsOutsideCamera()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return false;
        }

        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);

        return viewportPosition.x < -cameraDestroyPadding
            || viewportPosition.x > 1f + cameraDestroyPadding
            || viewportPosition.y < -cameraDestroyPadding
            || viewportPosition.y > 1f + cameraDestroyPadding;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasResolvedHit || controller == null)
        {
            return;
        }

        SurvivalSausage sausage = other.GetComponent<SurvivalSausage>();

        if (sausage == null)
        {
            return;
        }

        if (sausage.TryHitByBird(this))
        {
            hasResolvedHit = true;
        }
    }
}
