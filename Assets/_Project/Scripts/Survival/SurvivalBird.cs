using UnityEngine;

public class SurvivalBird : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private float destroyDistance = 18f;

    private SurvivalEndGame endGame;
    private Vector3 direction;
    private Vector3 startPosition;
    private bool hasSnatched;

    public void Initialize(SurvivalEndGame owner, Vector3 flyDirection, float flySpeed)
    {
        endGame = owner;
        direction = flyDirection.normalized;
        speed = flySpeed;
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        if (direction.sqrMagnitude > 0f)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        if (Vector3.Distance(startPosition, transform.position) >= destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasSnatched)
        {
            return;
        }

        SurvivalSausage sausage = other.GetComponent<SurvivalSausage>();

        if (sausage == null || endGame == null)
        {
            return;
        }

        hasSnatched = true;
        endGame.StealSausage(sausage);
        Destroy(gameObject);
    }
}
