using UnityEngine;

public class CollectableSausage : MonoBehaviour
{
    [SerializeField] private bool disableColliderOnCollect = true;
    [SerializeField] private bool forceTriggerCollider = true;
    [SerializeField] private bool addKinematicRigidbodyIfMissing = true;
    [SerializeField] private float releaseBackwardOffset = 1f;

    private Collider cachedCollider;
    private Rigidbody cachedRigidbody;
    private LaneThrownObject laneThrownObject;
    private bool isCollected;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        cachedRigidbody = GetComponent<Rigidbody>();
        laneThrownObject = GetComponent<LaneThrownObject>();

        EnsureTriggerSetup();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCollect(collision.collider);
    }

    private void EnsureTriggerSetup()
    {
        if (cachedCollider != null && forceTriggerCollider)
        {
            cachedCollider.isTrigger = true;
        }

        if (cachedRigidbody == null && addKinematicRigidbodyIfMissing)
        {
            cachedRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
        }
    }

    private void TryCollect(Collider other)
    {
        if (isCollected || other == null)
        {
            return;
        }

        SausageChainController chainController = GetOrCreateChainController(other);

        if (chainController == null)
        {
            return;
        }

        isCollected = true;

        if (disableColliderOnCollect && cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.isKinematic = true;
        }

        if (laneThrownObject != null)
        {
            laneThrownObject.enabled = false;
        }

        chainController.AddSegment(transform);
    }

    public void ReleaseFromChain(Transform chainRoot)
    {
        isCollected = false;

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;

            if (forceTriggerCollider)
            {
                cachedCollider.isTrigger = true;
            }
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
        }

        if (laneThrownObject == null)
        {
            laneThrownObject = gameObject.AddComponent<LaneThrownObject>();
        }

        laneThrownObject.enabled = true;

        Vector3 releasePosition = transform.position;

        if (chainRoot != null)
        {
            releasePosition += -chainRoot.forward * releaseBackwardOffset;
        }
        else
        {
            releasePosition += Vector3.left * releaseBackwardOffset;
        }

        transform.position = releasePosition;
        laneThrownObject.ResumeOnLane(releasePosition);
    }

    private SausageChainController GetOrCreateChainController(Collider other)
    {
        SausageChainController chainController = other.GetComponentInParent<SausageChainController>();

        if (chainController != null)
        {
            return chainController;
        }

        SausageMovement sausageMovement = other.GetComponentInParent<SausageMovement>();

        if (sausageMovement == null)
        {
            return null;
        }

        chainController = sausageMovement.GetComponent<SausageChainController>();

        if (chainController == null)
        {
            chainController = sausageMovement.gameObject.AddComponent<SausageChainController>();
        }

        return chainController;
    }
}
