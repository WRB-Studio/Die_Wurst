using System.Collections.Generic;
using UnityEngine;

public class SausageChainController : MonoBehaviour
{
    [Header("Chain")]
    [SerializeField] private float segmentSpacing = 1.1f;
    [SerializeField] private float followSmoothness = 12f;
    [SerializeField] private float historyResolution = 0.1f;
    [SerializeField] private float chainBonusPerSegment = 0.12f;
    [SerializeField] private Transform segmentParent;

    private readonly List<Transform> collectedSegments = new();
    private readonly List<Vector3> positionHistory = new();
    private SausageMovement sausageMovement;
    private Vector3 lastRecordedPosition;

    public int SegmentCount => collectedSegments.Count;
    public float ChainBonus => collectedSegments.Count * chainBonusPerSegment;

    private void Awake()
    {
        sausageMovement = GetComponent<SausageMovement>();

        if (segmentParent == null)
        {
            segmentParent = transform.parent;
        }
    }

    private void Start()
    {
        lastRecordedPosition = transform.position;
        positionHistory.Add(transform.position);
        ApplyChainBonus();
    }

    private void LateUpdate()
    {
        RecordPosition();
        UpdateSegments();
    }

    public void AddSegment(Transform segment)
    {
        if (segment == null)
        {
            return;
        }

        if (collectedSegments.Contains(segment))
        {
            return;
        }

        collectedSegments.Add(segment);

        if (segmentParent != null)
        {
            segment.SetParent(segmentParent);
        }

        ApplyChainBonus();
    }

    private void RecordPosition()
    {
        if (Vector3.Distance(lastRecordedPosition, transform.position) < historyResolution)
        {
            return;
        }

        positionHistory.Insert(0, transform.position);
        lastRecordedPosition = transform.position;

        int maxHistoryCount = Mathf.Max(10, Mathf.CeilToInt((collectedSegments.Count + 2) * segmentSpacing / Mathf.Max(historyResolution, 0.01f)));

        while (positionHistory.Count > maxHistoryCount)
        {
            positionHistory.RemoveAt(positionHistory.Count - 1);
        }
    }

    private void UpdateSegments()
    {
        if (collectedSegments.Count == 0 || positionHistory.Count == 0)
        {
            return;
        }

        for (int i = 0; i < collectedSegments.Count; i++)
        {
            Transform segment = collectedSegments[i];

            if (segment == null)
            {
                continue;
            }

            int historyIndex = GetHistoryIndexForSegment(i);
            Vector3 targetPosition = positionHistory[Mathf.Min(historyIndex, positionHistory.Count - 1)];
            segment.position = Vector3.Lerp(segment.position, targetPosition, followSmoothness * Time.deltaTime);

            Vector3 lookDirection = GetLookDirection(segment.position, targetPosition);

            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                segment.rotation = Quaternion.Lerp(segment.rotation, Quaternion.LookRotation(lookDirection), followSmoothness * Time.deltaTime);
            }
        }
    }

    private int GetHistoryIndexForSegment(int segmentIndex)
    {
        float distanceBack = (segmentIndex + 1) * segmentSpacing;
        return Mathf.RoundToInt(distanceBack / Mathf.Max(historyResolution, 0.01f));
    }

    private Vector3 GetLookDirection(Vector3 currentPosition, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - currentPosition;
        direction.y = 0f;
        return direction;
    }

    private void ApplyChainBonus()
    {
        if (sausageMovement != null)
        {
            sausageMovement.SetChainBonus(ChainBonus);
        }
    }
}
