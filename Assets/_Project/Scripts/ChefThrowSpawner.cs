using UnityEngine;

public class ChefThrowSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChefPatrol chefPatrol;
    [SerializeField] private Animator cookAnimator;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private Transform[] laneTargets;
    [SerializeField] private GameObject[] throwablePrefabs;
    [SerializeField] private GameObject extraSausagePrefab;
    [SerializeField] private GameObject defaultThrowablePrefab;
    [SerializeField] private Texture2D throwSpriteSheet;
    [SerializeField] private Sprite[] autoThrowSprites;

    [Header("Animation")]
    [SerializeField] private string idleAnimationStateName = "Metzger Animation";
    [SerializeField] private string throwAnimationStateName = "werfen";

    [Header("Throw")]
    [SerializeField] private float minTargetXOffset = -1f;
    [SerializeField] private float maxTargetXOffset = 1f;
    [SerializeField] private float targetYOffset = 0f;
    [SerializeField] private float throwDuration = 0.45f;
    [SerializeField] private float throwArcHeight = 2f;
    [SerializeField] private float extraSausageThrowChance = 0.25f;

    [Header("Conveyor")]
    [SerializeField] private float destroyAtX = -20f;

    [Header("Shredder")]
    [SerializeField] private float shredderPullDistance = 0.4f;
    [SerializeField] private float shredderDropDistance = 2.5f;
    [SerializeField] private float shredderFallSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private bool logThrowSpawn = false;
    [SerializeField] private float fallbackThrowInterval = 1.2f;
    [SerializeField] private float fallbackThrowIntervalRandomOffset = 0.2f;

    private float throwTimer;
    private bool wasWaitingLastFrame;
    private bool isPlayingThrowAnimation;

    private void Awake()
    {
        if (chefPatrol == null)
        {
            chefPatrol = GetComponent<ChefPatrol>();
        }

        if (cookAnimator == null)
        {
            cookAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        ResetThrowTimer();
    }

    private void Update()
    {
        UpdateThrowAnimation();

        if (chefPatrol == null)
        {
            wasWaitingLastFrame = false;
            return;
        }

        bool isWaiting = chefPatrol.IsWaiting;

        if (isWaiting && !wasWaitingLastFrame)
        {
            ResetThrowTimer();
        }

        wasWaitingLastFrame = isWaiting;

        if (!isWaiting)
        {
            return;
        }

        if (laneTargets == null || laneTargets.Length == 0 || !HasAnyThrowableSource())
        {
            return;
        }

        throwTimer -= Time.deltaTime;

        if (throwTimer > 0f)
        {
            return;
        }

        ThrowRandomObject();
        ResetThrowTimer();
    }

    private void ThrowRandomObject()
    {
        Transform laneTarget = GetRandomLaneTarget();

        if (laneTarget == null)
        {
            return;
        }

        GameObject instance = CreateRandomThrowableInstance();

        if (instance == null)
        {
            return;
        }

        Transform currentThrowOrigin = GetThrowOrigin();
        Vector3 spawnPosition = currentThrowOrigin.position;
        Vector3 targetPosition = GetTargetPosition(laneTarget);
        LaneThrownObject laneThrownObject = instance.GetComponent<LaneThrownObject>();

        instance.transform.SetPositionAndRotation(spawnPosition, instance.transform.rotation);

        if (laneThrownObject == null)
        {
            laneThrownObject = instance.AddComponent<LaneThrownObject>();
        }

        AudioManager.Instance?.PlayKnifeSfx();
        PlayThrowAnimation();

        laneThrownObject.Initialize(
            spawnPosition,
            targetPosition,
            throwDuration,
            throwArcHeight,
            GetConveyorSpeed(),
            destroyAtX,
            shredderPullDistance,
            shredderDropDistance,
            shredderFallSpeed);

        if (logThrowSpawn)
        {
            Debug.Log($"Chef throw spawn from {spawnPosition} to {targetPosition}", this);
        }
    }

    public bool TryThrowSpecialObjectToWindow(
        Vector3 impactPosition,
        int minLaneIndex,
        int maxLaneIndex,
        float minLaneXOffset,
        float maxLaneXOffset,
        float laneYOffset,
        System.Action impactReachedCallback)
    {
        if (!TryGetLaneTargetPosition(minLaneIndex, maxLaneIndex, minLaneXOffset, maxLaneXOffset, laneYOffset, out Vector3 landingPosition))
        {
            return false;
        }

        return TryThrowSpecialObject(impactPosition, landingPosition, impactReachedCallback);
    }

    public bool TryThrowSpecialObject(Vector3 impactPosition, Vector3 landingPosition, System.Action impactReachedCallback)
    {
        if (!HasAnyThrowableSource())
        {
            return false;
        }

        GameObject instance = CreateRandomThrowableInstance();

        if (instance == null)
        {
            return false;
        }

        Transform currentThrowOrigin = GetThrowOrigin();
        Vector3 spawnPosition = currentThrowOrigin.position;
        LaneThrownObject laneThrownObject = instance.GetComponent<LaneThrownObject>();

        instance.transform.SetPositionAndRotation(spawnPosition, instance.transform.rotation);

        if (laneThrownObject == null)
        {
            laneThrownObject = instance.AddComponent<LaneThrownObject>();
        }

        AudioManager.Instance?.PlayKnifeSfx();
        PlayThrowAnimation();

        laneThrownObject.InitializeWithImpact(
            spawnPosition,
            impactPosition,
            landingPosition,
            throwDuration,
            throwArcHeight,
            GetConveyorSpeed(),
            destroyAtX,
            shredderPullDistance,
            shredderDropDistance,
            shredderFallSpeed,
            impactReachedCallback);

        if (logThrowSpawn)
        {
            Debug.Log($"Chef special throw spawn from {spawnPosition} to window {impactPosition} and lane {landingPosition}", this);
        }

        ResetThrowTimer();
        return true;
    }

    private Vector3 GetTargetPosition(Transform laneTarget)
    {
        Vector3 targetPosition = laneTarget.position;
        targetPosition.x += Random.Range(minTargetXOffset, maxTargetXOffset);
        targetPosition.y += targetYOffset;
        return targetPosition;
    }

    private bool HasAnyThrowableSource()
    {
        return GetValidPrefabCount() > 0 || (defaultThrowablePrefab != null && GetValidAutoSpriteCount() > 0);
    }

    private GameObject CreateRandomThrowableInstance()
    {
        if (ShouldThrowExtraSausage())
        {
            return Instantiate(extraSausagePrefab);
        }

        int prefabCount = GetValidPrefabCount();
        int spriteCount = defaultThrowablePrefab != null ? GetValidAutoSpriteCount() : 0;
        int totalCount = prefabCount + spriteCount;

        if (totalCount == 0)
        {
            return null;
        }

        int selectionIndex = Random.Range(0, totalCount);

        if (selectionIndex < prefabCount)
        {
            GameObject prefab = GetValidPrefabAt(selectionIndex);
            return prefab != null ? Instantiate(prefab) : null;
        }

        return CreateSpriteBasedThrowable(selectionIndex - prefabCount);
    }

    private bool ShouldThrowExtraSausage()
    {
        if (extraSausagePrefab == null)
        {
            return false;
        }

        return Random.value < extraSausageThrowChance;
    }

    private GameObject CreateSpriteBasedThrowable(int spriteIndex)
    {
        Sprite sprite = GetValidAutoSpriteAt(spriteIndex);

        if (defaultThrowablePrefab == null || sprite == null)
        {
            return null;
        }

        GameObject instance = Instantiate(defaultThrowablePrefab);
        SpriteRenderer spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }

        return instance;
    }

    private int GetValidPrefabCount()
    {
        if (throwablePrefabs == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < throwablePrefabs.Length; i++)
        {
            if (throwablePrefabs[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private GameObject GetValidPrefabAt(int index)
    {
        if (throwablePrefabs == null || index < 0)
        {
            return null;
        }

        int currentIndex = 0;

        for (int i = 0; i < throwablePrefabs.Length; i++)
        {
            GameObject prefab = throwablePrefabs[i];

            if (prefab == null)
            {
                continue;
            }

            if (currentIndex == index)
            {
                return prefab;
            }

            currentIndex++;
        }

        return null;
    }

    private int GetValidAutoSpriteCount()
    {
        if (autoThrowSprites == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < autoThrowSprites.Length; i++)
        {
            if (autoThrowSprites[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private Sprite GetValidAutoSpriteAt(int index)
    {
        if (autoThrowSprites == null || index < 0)
        {
            return null;
        }

        int currentIndex = 0;

        for (int i = 0; i < autoThrowSprites.Length; i++)
        {
            Sprite sprite = autoThrowSprites[i];

            if (sprite == null)
            {
                continue;
            }

            if (currentIndex == index)
            {
                return sprite;
            }

            currentIndex++;
        }

        return null;
    }

    private Transform GetRandomLaneTarget()
    {
        int index = Random.Range(0, laneTargets.Length);
        return laneTargets[index];
    }

    private bool TryGetLaneTargetPosition(
        int minLaneIndex,
        int maxLaneIndex,
        float minLaneXOffset,
        float maxLaneXOffset,
        float laneYOffsetOverride,
        out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        if (laneTargets == null || laneTargets.Length == 0)
        {
            return false;
        }

        int clampedMinIndex = Mathf.Clamp(minLaneIndex, 0, laneTargets.Length - 1);
        int clampedMaxIndex = Mathf.Clamp(maxLaneIndex, clampedMinIndex, laneTargets.Length - 1);
        int laneIndex = Random.Range(clampedMinIndex, clampedMaxIndex + 1);
        Transform laneTarget = laneTargets[laneIndex];

        if (laneTarget == null)
        {
            return false;
        }

        float minOffset = Mathf.Min(minLaneXOffset, maxLaneXOffset);
        float maxOffset = Mathf.Max(minLaneXOffset, maxLaneXOffset);
        targetPosition = laneTarget.position;
        targetPosition.x += Random.Range(minOffset, maxOffset);
        targetPosition.y += laneYOffsetOverride;
        return true;
    }

    private void ResetThrowTimer()
    {
        float currentThrowInterval = GetCurrentThrowInterval();
        float throwIntervalRandomOffset = GetThrowIntervalRandomOffset();
        float minThrowInterval = Mathf.Max(0.01f, currentThrowInterval - throwIntervalRandomOffset);
        float maxThrowInterval = Mathf.Max(minThrowInterval, currentThrowInterval + throwIntervalRandomOffset);
        throwTimer = Random.Range(minThrowInterval, maxThrowInterval);
    }

    private float GetCurrentThrowInterval()
    {
        if (GameHandler.Instance != null)
        {
            return GameHandler.Instance.GetCurrentChefThrowInterval();
        }

        return Mathf.Max(0.01f, fallbackThrowInterval);
    }

    private float GetThrowIntervalRandomOffset()
    {
        if (GameHandler.Instance != null)
        {
            return GameHandler.Instance.GetChefThrowRandomOffset();
        }

        return Mathf.Max(0f, fallbackThrowIntervalRandomOffset);
    }

    private float GetConveyorSpeed()
    {
        if (GameHandler.Instance != null)
        {
            return GameHandler.Instance.GetConveyorSpeed();
        }

        return 0f;
    }

    public float GetMinTargetXOffset()
    {
        return minTargetXOffset;
    }

    public float GetMaxTargetXOffset()
    {
        return maxTargetXOffset;
    }

    public float GetTargetYOffset()
    {
        return targetYOffset;
    }

    private void PlayThrowAnimation()
    {
        if (cookAnimator == null || string.IsNullOrWhiteSpace(throwAnimationStateName))
        {
            return;
        }

        cookAnimator.Play(throwAnimationStateName, 0, 0f);
        isPlayingThrowAnimation = true;
    }

    private void UpdateThrowAnimation()
    {
        if (cookAnimator == null || !isPlayingThrowAnimation)
        {
            return;
        }

        AnimatorStateInfo stateInfo = cookAnimator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(throwAnimationStateName))
        {
            return;
        }

        if (stateInfo.normalizedTime < 1f)
        {
            return;
        }

        isPlayingThrowAnimation = false;

        if (string.IsNullOrWhiteSpace(idleAnimationStateName))
        {
            return;
        }

        cookAnimator.Play(idleAnimationStateName, 0, 0f);
    }

    private Transform GetThrowOrigin()
    {
        if (throwOrigin != null)
        {
            return throwOrigin;
        }

        if (chefPatrol != null)
        {
            return chefPatrol.transform;
        }

        return transform;
    }

    private void OnDrawGizmosSelected()
    {
        Transform currentThrowOrigin = GetThrowOrigin();

        if (!drawDebugGizmos || currentThrowOrigin == null || laneTargets == null)
        {
            return;
        }

        Gizmos.color = Color.green;

        foreach (Transform laneTarget in laneTargets)
        {
            if (laneTarget == null)
            {
                continue;
            }

            Gizmos.DrawLine(currentThrowOrigin.position, laneTarget.position);
        }
    }

    private void OnValidate()
    {
        fallbackThrowInterval = Mathf.Max(0.01f, fallbackThrowInterval);
        fallbackThrowIntervalRandomOffset = Mathf.Max(0f, fallbackThrowIntervalRandomOffset);
        extraSausageThrowChance = Mathf.Clamp01(extraSausageThrowChance);

#if UNITY_EDITOR
        RefreshAutoThrowSprites();
#endif
    }

#if UNITY_EDITOR
    private void RefreshAutoThrowSprites()
    {
        if (throwSpriteSheet == null)
        {
            autoThrowSprites = null;
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(throwSpriteSheet);
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        int spriteCount = 0;

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite)
            {
                spriteCount++;
            }
        }

        Sprite[] sprites = new Sprite[spriteCount];
        int spriteIndex = 0;

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                sprites[spriteIndex] = sprite;
                spriteIndex++;
            }
        }

        System.Array.Sort(sprites, (left, right) => string.CompareOrdinal(left.name, right.name));
        autoThrowSprites = sprites;
    }
#endif
}
