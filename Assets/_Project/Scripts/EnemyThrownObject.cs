using UnityEngine;

public class EnemyThrownObject : MonoBehaviour
{
    [SerializeField] private bool consumeOnHit = true;

    private Collider cachedCollider;
    private bool hasHit;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    private void TryHit(Collider other)
    {
        if (hasHit || other == null)
        {
            return;
        }

        SausageChainController chainController = other.GetComponentInParent<SausageChainController>();

        if (chainController == null)
        {
            return;
        }

        hasHit = true;
        GameHandler gameHandler = GameHandler.Instance;

        if (gameHandler != null)
        {
            gameHandler.HandlePlayerHit();
        }
        else
        {
            chainController.ReleaseLastSegment();
        }

        if (consumeOnHit)
        {
            Destroy(gameObject);
            return;
        }

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }
    }
}
