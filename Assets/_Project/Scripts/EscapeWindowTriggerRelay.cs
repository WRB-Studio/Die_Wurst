using UnityEngine;

public class EscapeWindowTriggerRelay : MonoBehaviour
{
    [SerializeField] private EscapeWindowExit owner;

    private void Awake()
    {
        if (owner == null)
        {
            owner = FindFirstObjectByType<EscapeWindowExit>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        owner?.HandleWindowTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        owner?.HandleWindowTrigger(other);
    }
}
