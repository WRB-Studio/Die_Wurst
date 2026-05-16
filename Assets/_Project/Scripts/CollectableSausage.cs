using UnityEngine;

public class CollectableSausage : MonoBehaviour
{
    [SerializeField] private bool disableColliderOnCollect = true;

    private Collider cachedCollider;
    private Rigidbody cachedRigidbody;
    private LaneThrownObject laneThrownObject;
    private bool isCollected;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        cachedRigidbody = GetComponent<Rigidbody>();
        laneThrownObject = GetComponent<LaneThrownObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
        {
            return;
        }

        SausageChainController chainController = other.GetComponentInParent<SausageChainController>();

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
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        if (laneThrownObject != null)
        {
            laneThrownObject.enabled = false;
        }

        chainController.AddSegment(transform);
    }
}
