using System.Collections.Generic;
using UnityEngine;

public class BirdController : MonoBehaviour
{
    [Header("Bird Setup")]
    [SerializeField] private GameObject birdPrefab;
    [SerializeField] private float minBirdInterval = 0.45f;
    [SerializeField] private float maxBirdInterval = 0.9f;
    [SerializeField] private float minBirdSpeed = 6f;
    [SerializeField] private float maxBirdSpeed = 8f;
    [SerializeField] private float birdSpawnOffset = 1f;
    [SerializeField] private float minBirdSpawnY = -3.5f;
    [SerializeField] private float maxBirdSpawnY = 2.5f;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.8f, 0.1f, 0.9f);

    private readonly List<SurvivalBird> birds = new List<SurvivalBird>();

    private SurvivalEndGame owner;
    private float birdSpawnTimer;
    private bool spawningEnabled;

    public void Initialize(SurvivalEndGame survivalOwner)
    {
        owner = survivalOwner;
        minBirdInterval = Mathf.Max(0.05f, minBirdInterval);
        maxBirdInterval = Mathf.Max(minBirdInterval, maxBirdInterval);
        minBirdSpeed = Mathf.Max(0.1f, minBirdSpeed);
        maxBirdSpeed = Mathf.Max(minBirdSpeed, maxBirdSpeed);
        maxBirdSpawnY = Mathf.Max(minBirdSpawnY, maxBirdSpawnY);
        spawningEnabled = true;
        ResetSpawnTimer();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateSpawning(deltaTime);
        UpdateBirds(deltaTime);
    }

    public void SetSpawningEnabled(bool isEnabled)
    {
        spawningEnabled = isEnabled;

        if (spawningEnabled)
        {
            ResetSpawnTimer();
        }
    }

    public void ClearBirds()
    {
        for (int i = 0; i < birds.Count; i++)
        {
            if (birds[i] != null)
            {
                Destroy(birds[i].gameObject);
            }
        }

        birds.Clear();
    }

    private void UpdateSpawning(float deltaTime)
    {
        if (!spawningEnabled || owner == null)
        {
            return;
        }

        birdSpawnTimer -= deltaTime;

        if (birdSpawnTimer > 0f)
        {
            return;
        }

        SpawnBird();
        ResetSpawnTimer();
    }

    private void UpdateBirds(float deltaTime)
    {
        for (int i = birds.Count - 1; i >= 0; i--)
        {
            SurvivalBird bird = birds[i];

            if (bird == null)
            {
                birds.RemoveAt(i);
                continue;
            }

            bird.TickMovement(deltaTime);

            if (!bird.ShouldBeRemoved())
            {
                continue;
            }

            Destroy(bird.gameObject);
            birds.RemoveAt(i);
        }
    }

    private void SpawnBird()
    {
        if (birdPrefab == null)
        {
            return;
        }

        bool fromLeft = Random.value < 0.5f;
        float spawnX = owner.GetHorizontalCameraEdge(fromLeft) + (fromLeft ? -birdSpawnOffset : birdSpawnOffset);
        float spawnY = Random.Range(minBirdSpawnY, maxBirdSpawnY);
        float birdSpeed = Random.Range(minBirdSpeed, maxBirdSpeed);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);
        Vector3 direction = fromLeft ? Vector3.right : Vector3.left;

        GameObject birdObject = CreateBirdObject(spawnPosition);
        SurvivalBird bird = birdObject.GetComponent<SurvivalBird>();

        if (bird == null)
        {
            bird = birdObject.AddComponent<SurvivalBird>();
        }

        bird.Initialize(this, direction, birdSpeed);
        birds.Add(bird);
    }

    private GameObject CreateBirdObject(Vector3 spawnPosition)
    {
        GameObject birdObject = Instantiate(birdPrefab, spawnPosition, Quaternion.identity);
        birdObject.name = "Survival Bird";
        birdObject.transform.position = spawnPosition;
        return birdObject;
    }

    private void ResetSpawnTimer()
    {
        birdSpawnTimer = Random.Range(minBirdInterval, maxBirdInterval);
    }

    private void OnValidate()
    {
        minBirdInterval = Mathf.Max(0.05f, minBirdInterval);
        maxBirdInterval = Mathf.Max(minBirdInterval, maxBirdInterval);
        minBirdSpeed = Mathf.Max(0.1f, minBirdSpeed);
        maxBirdSpeed = Mathf.Max(minBirdSpeed, maxBirdSpeed);
        maxBirdSpawnY = Mathf.Max(minBirdSpawnY, maxBirdSpawnY);
    }

    private void OnDrawGizmosSelected()
    {
        if (owner == null && !Application.isPlaying)
        {
            owner = FindFirstObjectByType<SurvivalEndGame>();
        }

        if (owner == null)
        {
            return;
        }

        float leftX = owner.GetHorizontalCameraEdge(true) - birdSpawnOffset;
        float rightX = owner.GetHorizontalCameraEdge(false) + birdSpawnOffset;
        Vector3 leftBottom = new Vector3(leftX, minBirdSpawnY, 0f);
        Vector3 leftTop = new Vector3(leftX, maxBirdSpawnY, 0f);
        Vector3 rightBottom = new Vector3(rightX, minBirdSpawnY, 0f);
        Vector3 rightTop = new Vector3(rightX, maxBirdSpawnY, 0f);

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(leftBottom, leftTop);
        Gizmos.DrawLine(rightBottom, rightTop);
        Gizmos.DrawSphere(leftBottom, 0.12f);
        Gizmos.DrawSphere(leftTop, 0.12f);
        Gizmos.DrawSphere(rightBottom, 0.12f);
        Gizmos.DrawSphere(rightTop, 0.12f);
    }
}
