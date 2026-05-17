using UnityEngine;

public class SurvivalBird : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private float destroyDistance = 18f;
    [SerializeField] private float frameTime = 0.16f;

    private SurvivalEndGame endGame;
    private SpriteRenderer spriteRenderer;
    private Sprite frameA;
    private Sprite frameB;
    private Vector3 direction;
    private Vector3 startPosition;
    private float frameTimer;
    private bool showFrameB;
    private bool hasSnatched;

    public void Initialize(SurvivalEndGame owner, Vector3 flyDirection, float flySpeed, Sprite firstFrame = null, Sprite secondFrame = null)
    {
        endGame = owner;
        direction = flyDirection.normalized;
        speed = flySpeed;
        startPosition = transform.position;
        frameA = firstFrame;
        frameB = secondFrame;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frameA != null ? frameA : spriteRenderer.sprite;
            spriteRenderer.flipX = direction.x < 0f;
        }
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        UpdateAnimation();

        if (Vector3.Distance(startPosition, transform.position) >= destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateAnimation()
    {
        if (spriteRenderer == null || frameA == null || frameB == null)
        {
            return;
        }

        frameTimer -= Time.deltaTime;

        if (frameTimer > 0f)
        {
            return;
        }

        frameTimer = frameTime;
        showFrameB = !showFrameB;
        spriteRenderer.sprite = showFrameB ? frameB : frameA;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasSnatched)
        {
            return;
        }

        SurvivalSausage sausage = other.GetComponent<SurvivalSausage>();

        if (sausage == null || endGame == null)
        {
            return;
        }

        hasSnatched = true;
        endGame.StealSausage(sausage);
        Destroy(gameObject);
    }
}
