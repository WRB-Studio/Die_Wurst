using UnityEngine;

public class SurvivalSausage : MonoBehaviour
{
    [SerializeField] private float wobbleAngle = 8f;
    [SerializeField] private float wobbleSpeed = 5f;
    [SerializeField] private float driftAmount = 0.08f;
    [SerializeField] private float driftSpeed = 3f;

    private Vector3 baseLocalPosition;
    private float randomOffset;

    private void Awake()
    {
        randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float wobble = Mathf.Sin(Time.time * wobbleSpeed + randomOffset) * wobbleAngle;
        float driftX = Mathf.Sin(Time.time * driftSpeed + randomOffset) * driftAmount;
        float driftY = Mathf.Cos(Time.time * driftSpeed * 0.7f + randomOffset) * driftAmount;

        transform.localPosition = baseLocalPosition + new Vector3(driftX, driftY, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, wobble);
    }

    public void SetBaseLocalPosition(Vector3 position)
    {
        baseLocalPosition = position;
        transform.localPosition = baseLocalPosition;
    }
}
