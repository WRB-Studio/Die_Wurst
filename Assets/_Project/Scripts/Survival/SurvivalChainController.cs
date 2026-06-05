using System.Collections.Generic;
using UnityEngine;

public class SurvivalChainController : MonoBehaviour
{
    private enum ChainPhase
    {
        Air,
        Ground
    }

    [Header("Air")]
    [SerializeField] private int initialExtraSausages = 7;
    [SerializeField] private float airInnerRadius = 0.85f;
    [SerializeField] private float airOuterRadius = 1.55f;
    [SerializeField] private float airFollowSmoothTime = 0.12f;
    [SerializeField] private float airSmoothnessVariance = 0.025f;
    [SerializeField] private float airMinimumSpacing = 0.35f;
    [SerializeField] private int airSpawnAttempts = 24;
    [SerializeField] private Color airGizmoColor = new Color(1f, 0.65f, 0.1f, 0.9f);

    [Header("Ground")]
    [SerializeField] private float groundMainSegmentSpacing = 1.15f;
    [SerializeField] private float minGroundSegmentSpacing = 0.8f;
    [SerializeField] private float maxGroundSegmentSpacing = 1f;
    [SerializeField] private float groundFollowSmoothTime = 0.09f;
    [SerializeField] private float groundSmoothnessVariance = 0.02f;
    [SerializeField] private float groundDirectionChangeSpeed = 3.5f;
    [SerializeField] private float groundRowHeight = 0.28f;
    [SerializeField] private float groundVerticalJitter = 0.08f;
    [SerializeField] private float minGroundBounceHeight = 0.05f;
    [SerializeField] private float maxGroundBounceHeight = 0.1f;
    [SerializeField] private float minGroundBounceSpeed = 6f;
    [SerializeField] private float maxGroundBounceSpeed = 9f;

    private readonly List<SurvivalSausage> sausages = new List<SurvivalSausage>();
    private readonly Dictionary<SurvivalSausage, Vector3> airPositionsBySausage = new Dictionary<SurvivalSausage, Vector3>();
    private readonly Dictionary<SurvivalSausage, float> smoothTimeBySausage = new Dictionary<SurvivalSausage, float>();
    private readonly Dictionary<SurvivalSausage, float> groundSpacingFactorBySausage = new Dictionary<SurvivalSausage, float>();
    private readonly Dictionary<SurvivalSausage, float> groundLaneFactorBySausage = new Dictionary<SurvivalSausage, float>();
    private readonly Dictionary<SurvivalSausage, float> groundJitterFactorBySausage = new Dictionary<SurvivalSausage, float>();
    private readonly Dictionary<SurvivalSausage, float> groundBounceHeightBySausage = new Dictionary<SurvivalSausage, float>();
    private readonly Dictionary<SurvivalSausage, float> groundBounceSpeedBySausage = new Dictionary<SurvivalSausage, float>();
    private ChainPhase phase = ChainPhase.Air;
    private float groundDirection = 1f;
    private float targetGroundDirection = 1f;
    private float groundDirectionVelocity;

    public int InitialExtraSausages => initialExtraSausages;
    public Transform MainSausageTransform => sausages.Count > 0 && sausages[0] != null ? sausages[0].transform : null;

    public void SetInitialExtraSausages(int count)
    {
        initialExtraSausages = Mathf.Max(0, count);
    }

    public Vector3 GetMainWorldPosition()
    {
        return MainSausageTransform != null ? MainSausageTransform.position : transform.position;
    }

    public void SetMainWorldPosition(Vector3 worldPosition)
    {
        if (sausages.Count == 0 || sausages[0] == null)
        {
            return;
        }

        sausages[0].SetWorldPositionImmediate(worldPosition);
    }

    public void RegisterSausage(SurvivalSausage sausage)
    {
        if (sausage == null || sausages.Contains(sausage))
        {
            return;
        }

        sausages.Add(sausage);

        if (!sausage.IsMainSausage)
        {
            airPositionsBySausage[sausage] = CreateAirPosition();
            smoothTimeBySausage[sausage] = CreateSmoothnessOffset();
            groundSpacingFactorBySausage[sausage] = Random.Range(0f, 1f);
            groundLaneFactorBySausage[sausage] = CreateGroundLaneFactor();
            groundJitterFactorBySausage[sausage] = Random.Range(-1f, 1f);
            groundBounceHeightBySausage[sausage] = Random.Range(minGroundBounceHeight, maxGroundBounceHeight);
            groundBounceSpeedBySausage[sausage] = Random.Range(minGroundBounceSpeed, maxGroundBounceSpeed);
            ApplyGroundBounceSettings(sausage);
        }
    }

    public void RemoveSausage(SurvivalSausage sausage)
    {
        if (sausage == null)
        {
            return;
        }

        sausages.Remove(sausage);
        airPositionsBySausage.Remove(sausage);
        smoothTimeBySausage.Remove(sausage);
        groundSpacingFactorBySausage.Remove(sausage);
        groundLaneFactorBySausage.Remove(sausage);
        groundJitterFactorBySausage.Remove(sausage);
        groundBounceHeightBySausage.Remove(sausage);
        groundBounceSpeedBySausage.Remove(sausage);
    }

    public void ReplaceSausage(SurvivalSausage oldSausage, SurvivalSausage newSausage)
    {
        if (oldSausage == null || newSausage == null)
        {
            return;
        }

        int index = sausages.IndexOf(oldSausage);

        if (index < 0)
        {
            RegisterSausage(newSausage);
            return;
        }

        sausages[index] = newSausage;

        if (airPositionsBySausage.TryGetValue(oldSausage, out Vector3 airPosition))
        {
            airPositionsBySausage.Remove(oldSausage);
            airPositionsBySausage[newSausage] = airPosition;
        }

        if (smoothTimeBySausage.TryGetValue(oldSausage, out float smoothTime))
        {
            smoothTimeBySausage.Remove(oldSausage);
            smoothTimeBySausage[newSausage] = smoothTime;
        }

        if (groundSpacingFactorBySausage.TryGetValue(oldSausage, out float spacingFactor))
        {
            groundSpacingFactorBySausage.Remove(oldSausage);
            groundSpacingFactorBySausage[newSausage] = spacingFactor;
        }

        if (groundLaneFactorBySausage.TryGetValue(oldSausage, out float laneFactor))
        {
            groundLaneFactorBySausage.Remove(oldSausage);
            groundLaneFactorBySausage[newSausage] = laneFactor;
        }

        if (groundJitterFactorBySausage.TryGetValue(oldSausage, out float jitterFactor))
        {
            groundJitterFactorBySausage.Remove(oldSausage);
            groundJitterFactorBySausage[newSausage] = jitterFactor;
        }

        if (groundBounceHeightBySausage.TryGetValue(oldSausage, out float bounceHeight))
        {
            groundBounceHeightBySausage.Remove(oldSausage);
            groundBounceHeightBySausage[newSausage] = bounceHeight;
        }

        if (groundBounceSpeedBySausage.TryGetValue(oldSausage, out float bounceSpeed))
        {
            groundBounceSpeedBySausage.Remove(oldSausage);
            groundBounceSpeedBySausage[newSausage] = bounceSpeed;
        }

        if (!newSausage.IsMainSausage)
        {
            ApplyGroundBounceSettings(newSausage);
        }
    }

    public void SetAirPhase()
    {
        phase = ChainPhase.Air;
    }

    public void SetGroundPhase()
    {
        phase = ChainPhase.Ground;

        for (int i = 0; i < sausages.Count; i++)
        {
            if (!sausages[i].IsMainSausage)
            {
                ApplyGroundBounceSettings(sausages[i]);
            }

            sausages[i].EnterGroundMode();
        }
    }

    public void SetGroundDirection(float direction)
    {
        if (Mathf.Abs(direction) <= 0.01f)
        {
            return;
        }

        targetGroundDirection = Mathf.Sign(direction);
    }

    private void LateUpdate()
    {
        if (sausages.Count == 0)
        {
            return;
        }

        if (phase == ChainPhase.Air)
        {
            UpdateAirPhase();
            return;
        }

        UpdateGroundDirection();
        UpdateGroundPhase();
    }

    private void UpdateAirPhase()
    {
        Vector3 mainLocalPosition = sausages[0].transform.localPosition;

        for (int i = 1; i < sausages.Count; i++)
        {
            SurvivalSausage sausage = sausages[i];
            sausage.MoveLocalPosition(
                mainLocalPosition + GetAirLocalPosition(sausage),
                GetSmoothTimeForSausage(sausage, airFollowSmoothTime));
        }
    }

    private void UpdateGroundPhase()
    {
        Vector3 mainLocalPosition = sausages[0].transform.localPosition;

        for (int i = 1; i < sausages.Count; i++)
        {
            SurvivalSausage sausage = sausages[i];
            Vector3 targetLocalPosition = mainLocalPosition + GetGroundLocalPosition(sausage, i);
            sausage.MoveLocalPosition(
                targetLocalPosition,
                GetSmoothTimeForSausage(sausage, groundFollowSmoothTime));
        }

        UpdateGroundSortingOrder();
    }

    private void UpdateGroundSortingOrder()
    {
        List<SurvivalSausage> extraSausages = new List<SurvivalSausage>();

        for (int i = 1; i < sausages.Count; i++)
        {
            if (sausages[i] != null)
            {
                extraSausages.Add(sausages[i]);
            }
        }

        if (extraSausages.Count == 0)
        {
            return;
        }

        extraSausages.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
        int baseSortingOrder = extraSausages[0].InitialSortingOrder;

        for (int i = 0; i < extraSausages.Count; i++)
        {
            extraSausages[i].SetSortingOrder(baseSortingOrder + i);
        }
    }

    private Vector3 GetGroundLocalPosition(SurvivalSausage sausage, int fallbackIndex)
    {
        int index = GetExtraIndex(sausage, fallbackIndex);
        float spacingOffset = GetGroundSpacingOffset(sausage, index);
        float verticalOffset = GetGroundVerticalOffset(sausage, index);
        return new Vector3(-groundDirection * spacingOffset, verticalOffset, 0f);
    }

    private Vector3 GetAirLocalPosition(SurvivalSausage sausage)
    {
        if (sausage == null)
        {
            return Vector3.zero;
        }

        if (!airPositionsBySausage.TryGetValue(sausage, out Vector3 airPosition))
        {
            airPosition = CreateAirPosition();
            airPositionsBySausage[sausage] = airPosition;
        }

        return airPosition;
    }

    private Vector3 CreateAirPosition()
    {
        int attempts = Mathf.Max(1, airSpawnAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = GetRandomAirPosition();

            if (!IsTooCloseToExisting(candidate))
            {
                return candidate;
            }
        }

        return GetRandomAirPosition();
    }

    private Vector3 GetRandomAirPosition()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float innerRadiusSquared = airInnerRadius * airInnerRadius;
        float outerRadiusSquared = airOuterRadius * airOuterRadius;
        float radius = Mathf.Sqrt(Random.Range(innerRadiusSquared, outerRadiusSquared));
        return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
    }

    private bool IsTooCloseToExisting(Vector3 candidate)
    {
        float minimumSpacingSquared = airMinimumSpacing * airMinimumSpacing;

        foreach (Vector3 existingPosition in airPositionsBySausage.Values)
        {
            if ((existingPosition - candidate).sqrMagnitude < minimumSpacingSquared)
            {
                return true;
            }
        }

        return false;
    }

    private int GetExtraIndex(SurvivalSausage sausage, int fallbackIndex)
    {
        int index = 1;

        for (int i = 1; i < sausages.Count; i++)
        {
            if (sausages[i] == sausage)
            {
                return index;
            }

            index++;
        }

        return fallbackIndex;
    }

    private float GetSmoothTimeForSausage(SurvivalSausage sausage, float baseSmoothTime)
    {
        if (sausage == null)
        {
            return baseSmoothTime;
        }

        if (!smoothTimeBySausage.TryGetValue(sausage, out float offset))
        {
            offset = CreateSmoothnessOffset();
            smoothTimeBySausage[sausage] = offset;
        }

        float variance = phase == ChainPhase.Air ? airSmoothnessVariance : groundSmoothnessVariance;
        return Mathf.Max(0.01f, baseSmoothTime + Mathf.Clamp(offset, -variance, variance));
    }

    private float GetGroundSpacingOffset(SurvivalSausage sausage, int fallbackIndex)
    {
        float totalSpacing = groundMainSegmentSpacing;

        for (int i = 1; i <= fallbackIndex; i++)
        {
            SurvivalSausage currentSausage = i < sausages.Count ? sausages[i] : null;

            if (currentSausage == null)
            {
                totalSpacing += minGroundSegmentSpacing;
                continue;
            }

            if (!groundSpacingFactorBySausage.TryGetValue(currentSausage, out float spacingFactor))
            {
                spacingFactor = Random.Range(0f, 1f);
                groundSpacingFactorBySausage[currentSausage] = spacingFactor;
            }

            float spacing = Mathf.Lerp(minGroundSegmentSpacing, maxGroundSegmentSpacing, spacingFactor);
            totalSpacing += spacing;
        }

        return totalSpacing;
    }

    private float GetGroundVerticalOffset(SurvivalSausage sausage, int extraIndex)
    {
        if (!groundLaneFactorBySausage.TryGetValue(sausage, out float laneFactor))
        {
            laneFactor = CreateGroundLaneFactor();
            groundLaneFactorBySausage[sausage] = laneFactor;
        }

        if (!groundJitterFactorBySausage.TryGetValue(sausage, out float jitterFactor))
        {
            jitterFactor = Random.Range(-1f, 1f);
            groundJitterFactorBySausage[sausage] = jitterFactor;
        }

        float verticalOffset = laneFactor * groundRowHeight + jitterFactor * groundVerticalJitter;

        int totalExtras = Mathf.Max(0, sausages.Count - 1);
        int groupCount = GetGroundGroupCount(totalExtras);
        int safeIndex = Mathf.Max(0, extraIndex - 1);
        int laneIndex = groupCount <= 1 ? 0 : safeIndex % groupCount;
        float centeredLane = laneIndex - (groupCount - 1) * 0.5f;
        float groupWaveOffset = centeredLane * groundRowHeight;
        return verticalOffset + groupWaveOffset;
    }

    private int GetGroundGroupCount(int totalExtras)
    {
        if (totalExtras <= 1)
        {
            return 1;
        }

        if (totalExtras <= 2)
        {
            return 2;
        }

        if (totalExtras <= 4)
        {
            return 2;
        }

        if (totalExtras <= 7)
        {
            return 3;
        }

        if (totalExtras <= 11)
        {
            return 4;
        }

        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(totalExtras)) + 1, 4, 6);
    }

    private float CreateGroundLaneFactor()
    {
        float laneChoice = Random.Range(0f, 1f);

        if (laneChoice < 0.34f)
        {
            return -1f;
        }

        if (laneChoice < 0.67f)
        {
            return 0f;
        }

        return 1f;
    }

    private void UpdateGroundDirection()
    {
        float smoothTime = 1f / Mathf.Max(0.01f, groundDirectionChangeSpeed);
        groundDirection = Mathf.SmoothDamp(
            groundDirection,
            targetGroundDirection,
            ref groundDirectionVelocity,
            smoothTime);
    }

    private float CreateSmoothnessOffset()
    {
        float variance = Mathf.Max(airSmoothnessVariance, groundSmoothnessVariance);
        return Random.Range(-variance, variance);
    }

    private void ApplyGroundBounceSettings(SurvivalSausage sausage)
    {
        if (sausage == null || sausage.IsMainSausage)
        {
            return;
        }

        if (!groundBounceHeightBySausage.TryGetValue(sausage, out float bounceHeight))
        {
            bounceHeight = Random.Range(minGroundBounceHeight, maxGroundBounceHeight);
            groundBounceHeightBySausage[sausage] = bounceHeight;
        }

        if (!groundBounceSpeedBySausage.TryGetValue(sausage, out float bounceSpeed))
        {
            bounceSpeed = Random.Range(minGroundBounceSpeed, maxGroundBounceSpeed);
            groundBounceSpeedBySausage[sausage] = bounceSpeed;
        }

        sausage.ConfigureGroundBounce(bounceHeight, bounceSpeed);
    }

    private void OnValidate()
    {
        initialExtraSausages = Mathf.Max(0, initialExtraSausages);
        airInnerRadius = Mathf.Max(0f, airInnerRadius);
        airOuterRadius = Mathf.Max(airInnerRadius, airOuterRadius);
        airFollowSmoothTime = Mathf.Max(0.01f, airFollowSmoothTime);
        airSmoothnessVariance = Mathf.Max(0f, airSmoothnessVariance);
        airMinimumSpacing = Mathf.Max(0f, airMinimumSpacing);
        airSpawnAttempts = Mathf.Max(1, airSpawnAttempts);
        groundMainSegmentSpacing = Mathf.Max(0f, groundMainSegmentSpacing);
        minGroundSegmentSpacing = Mathf.Max(0f, minGroundSegmentSpacing);
        maxGroundSegmentSpacing = Mathf.Max(minGroundSegmentSpacing, maxGroundSegmentSpacing);
        groundFollowSmoothTime = Mathf.Max(0.01f, groundFollowSmoothTime);
        groundSmoothnessVariance = Mathf.Max(0f, groundSmoothnessVariance);
        groundDirectionChangeSpeed = Mathf.Max(0.01f, groundDirectionChangeSpeed);
        groundRowHeight = Mathf.Max(0f, groundRowHeight);
        groundVerticalJitter = Mathf.Max(0f, groundVerticalJitter);
        minGroundBounceHeight = Mathf.Max(0f, minGroundBounceHeight);
        maxGroundBounceHeight = Mathf.Max(minGroundBounceHeight, maxGroundBounceHeight);
        minGroundBounceSpeed = Mathf.Max(0.01f, minGroundBounceSpeed);
        maxGroundBounceSpeed = Mathf.Max(minGroundBounceSpeed, maxGroundBounceSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Transform mainTransform = MainSausageTransform != null ? MainSausageTransform : transform;
        Vector3 center = mainTransform.position;

        Gizmos.color = airGizmoColor;
        DrawCircle(center, airInnerRadius);
        DrawCircle(center, airOuterRadius);
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        const int segments = 48;

        if (radius <= 0f)
        {
            return;
        }

        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
