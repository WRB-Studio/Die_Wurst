using System.Collections.Generic;
using UnityEngine;

public class SausageChainController : MonoBehaviour
{
    private class ChainSegment
    {
        public Transform Transform;
        public bool IsJoining;
        public Vector3 JoinStartPosition;
        public float JoinElapsed;
    }

    [Header("Chain")]
    [SerializeField] private float segmentSpacing = 1.1f;
    [SerializeField] private float segmentYOffset = 0f;
    [SerializeField] private float followSmoothness = 12f;
    [SerializeField] private float joinDuration = 0.25f;
    [SerializeField] private float joinArcHeight = 0.35f;
    [SerializeField] private float grinderDropX = -3.5f;
    [SerializeField] private float chainBonusPerSegment = 0.12f;
    [SerializeField] private Transform segmentParent;

    private readonly List<ChainSegment> collectedSegments = new();
    private SausageMovement sausageMovement;

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
        ApplyChainBonus();
    }

    private void LateUpdate()
    {
        UpdateSegments();
    }

    public void AddSegment(Transform segment)
    {
        if (segment == null)
        {
            return;
        }

        if (ContainsSegment(segment))
        {
            return;
        }

        if (segmentParent != null)
        {
            segment.SetParent(segmentParent);
        }

        collectedSegments.Add(new ChainSegment
        {
            Transform = segment,
            IsJoining = true,
            JoinStartPosition = segment.position,
            JoinElapsed = 0f
        });

        AudioManager.Instance.PlaySFX("sfx_yay", false);

        ApplyChainBonus();
    }

    public bool ReleaseLastSegment()
    {
        for (int i = collectedSegments.Count - 1; i >= 0; i--)
        {
            ChainSegment segmentData = collectedSegments[i];

            if (segmentData.Transform == null)
            {
                collectedSegments.RemoveAt(i);
                continue;
            }

            collectedSegments.RemoveAt(i);

            CollectableSausage collectableSausage = segmentData.Transform.GetComponent<CollectableSausage>();

            if (collectableSausage != null)
            {
                collectableSausage.ReleaseFromChain(transform);
            }

            ApplyChainBonus();
            return true;
        }

        ApplyChainBonus();
        return false;
    }

    private void UpdateSegments()
    {
        if (collectedSegments.Count == 0)
        {
            return;
        }

        for (int i = 0; i < collectedSegments.Count; i++)
        {
            ChainSegment segmentData = collectedSegments[i];
            Transform segment = segmentData.Transform;

            if (segment == null)
            {
                continue;
            }

            Transform leader = i == 0 ? transform : collectedSegments[i - 1].Transform;
            Vector3 targetPosition = GetTargetPositionBehindLeader(leader, segment.position);
            targetPosition.y += segmentYOffset;

            if (segmentData.IsJoining)
            {
                UpdateJoiningSegment(segmentData, targetPosition);
                continue;
            }

            segment.position = Vector3.MoveTowards(segment.position, targetPosition, followSmoothness * Time.deltaTime);

            if (segment.position.x <= grinderDropX)
            {
                ReleaseSegmentAt(i);
                i--;
            }
        }
    }

    private Vector3 GetTargetPositionBehindLeader(Transform leader, Vector3 currentSegmentPosition)
    {
        Vector3 direction = currentSegmentPosition - leader.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -leader.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.left;
        }

        Vector3 targetPosition = leader.position + direction.normalized * segmentSpacing;
        targetPosition.y = leader.position.y;
        return targetPosition;
    }

    private void UpdateJoiningSegment(ChainSegment segmentData, Vector3 targetPosition)
    {
        Transform segment = segmentData.Transform;
        segmentData.JoinElapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(segmentData.JoinElapsed / Mathf.Max(joinDuration, 0.01f));
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
        Vector3 position = Vector3.Lerp(segmentData.JoinStartPosition, targetPosition, easedProgress);
        position.y += 4f * joinArcHeight * progress * (1f - progress);

        segment.position = position;

        if (progress >= 1f)
        {
            segmentData.IsJoining = false;
            segment.position = targetPosition;
        }
    }

    private bool ContainsSegment(Transform segment)
    {
        foreach (ChainSegment collectedSegment in collectedSegments)
        {
            if (collectedSegment.Transform == segment)
            {
                return true;
            }
        }

        return false;
    }

    private void ReleaseSegmentAt(int index)
    {
        if (index < 0 || index >= collectedSegments.Count)
        {
            return;
        }

        ChainSegment segmentData = collectedSegments[index];
        collectedSegments.RemoveAt(index);

        if (segmentData.Transform == null)
        {
            ApplyChainBonus();
            return;
        }

        CollectableSausage collectableSausage = segmentData.Transform.GetComponent<CollectableSausage>();

        if (collectableSausage != null)
        {
            collectableSausage.ReleaseFromChain(transform);
        }

        ApplyChainBonus();
    }

    private void ApplyChainBonus()
    {
        if (sausageMovement != null)
        {
            sausageMovement.SetChainBonus(ChainBonus);
        }
    }
}
