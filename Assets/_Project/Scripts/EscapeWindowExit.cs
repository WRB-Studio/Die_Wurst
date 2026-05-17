using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeWindowExit : MonoBehaviour
{
    private const string CollectedSausageCountKey = "CollectedSausageCount";

    [Header("Scene")]
    [SerializeField] private string survivalSceneName = "SausageSurvivalEnd";

    [Header("Window")]
    [SerializeField] private bool createWindowOnStart = true;
    [SerializeField] private Vector3 windowPosition = new Vector3(6.95f, 1.25f, 2.25f);
    [SerializeField] private Vector3 windowSize = new Vector3(0.18f, 1.8f, 2.8f);
    [SerializeField] private Color windowColor = new Color(0.52f, 0.82f, 1f, 0.55f);

    private bool isLoading;

    private void Start()
    {
        if (createWindowOnStart)
        {
            CreateWindowTrigger();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryLoadSurvivalScene(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryLoadSurvivalScene(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryLoadSurvivalScene(collision != null ? collision.collider : null);
    }

    private void TryLoadSurvivalScene(Collider other)
    {
        if (isLoading || other == null)
        {
            return;
        }

        SausageMovement sausageMovement = other.GetComponentInParent<SausageMovement>();

        if (sausageMovement == null)
        {
            return;
        }

        SaveCollectedSausageCount(sausageMovement);
        isLoading = true;
        SceneManager.LoadScene(survivalSceneName);
    }

    private void SaveCollectedSausageCount(SausageMovement sausageMovement)
    {
        SausageChainController chainController = sausageMovement.GetComponent<SausageChainController>();
        int collectedCount = chainController != null ? chainController.SegmentCount : 0;
        int survivalSausageCount = collectedCount + 1;

        PlayerPrefs.SetInt(CollectedSausageCountKey, survivalSausageCount);
        PlayerPrefs.Save();
    }

    private void CreateWindowTrigger()
    {
        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "Escape Window";
        window.transform.position = windowPosition;
        window.transform.localScale = windowSize;

        Collider windowCollider = window.GetComponent<Collider>();

        if (windowCollider != null)
        {
            windowCollider.isTrigger = true;
        }

        Renderer renderer = window.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material.color = windowColor;
        }

        EscapeWindowExit trigger = window.AddComponent<EscapeWindowExit>();
        trigger.survivalSceneName = survivalSceneName;
        trigger.createWindowOnStart = false;
    }
}
