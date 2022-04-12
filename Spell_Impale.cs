using UnityEngine;

public class Spell_Impale : MonoBehaviour
{
    [SerializeField, Tooltip("Speed the spikes travel"), Range(10, 25)]
    private float speed;

    [SerializeField, Tooltip("The distance and interval between spikes"), Range(1, 5)]
    private float spawnRate;

    [SerializeField, Tooltip("The overall time the spell takes to complete"), Range(0.5f, 5)]
    private float spawnDuration;

    [SerializeField, Tooltip("Randomness in spawn positioning of individual spikes"), Range(0.5f, 2)]
    private float positionOffset;

    [SerializeField, Tooltip("Target Game Object")]
    private GameObject targetObj;

    [SerializeField, Tooltip("Layer assigned to the target")]
    private LayerMask targetLayer;

    [SerializeField]
    private GameObject spikePrefab;

    private float spellTimer, startSpeed, spawnDur, impaleDamage;
    private Vector3 targetPosition;
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position;
    }

    public void CastImpale(float damage)
    {
        impaleDamage = damage;
        startSpeed = speed;
        spawnDur = spawnDuration;
        targetPosition = targetObj.transform.position;
        transform.position = originalPosition;
        transform.LookAt(targetObj.transform);
        spellTimer = 0;
    }

    private void Update()
    {
        spawnDur -= Time.deltaTime;
        transform.position += transform.forward * (startSpeed * Time.deltaTime);
        spellTimer += Time.deltaTime;

        Vector3 direction = transform.position - targetPosition;
        float distance = direction.magnitude;

        if (distance > spawnRate && spawnDur > 0)
        {
            if (spikePrefab != null)
            {
                Vector3 randomPosition = new Vector3(Random.Range(-positionOffset, positionOffset), 0, Random.Range(-positionOffset, positionOffset));
                Vector3 targetPosition = transform.position + (randomPosition * spellTimer);

                if (Terrain.activeTerrain != null)
                    targetPosition.y = Terrain.activeTerrain.SampleHeight(transform.position);

                GameObject craterInstance = Instantiate(spikePrefab, targetPosition, Quaternion.identity);
                ParticleSystem craterParticle = craterInstance.GetComponent<ParticleSystem>();

                if (Physics.CheckSphere(targetPosition, 1, targetLayer))
                {
                    // Deal damage to your target here
                }

                if (craterParticle != null)
                {
                    Destroy(craterInstance, craterParticle.main.duration);
                }
                else
                {
                    ParticleSystem childParticle = craterInstance.transform.GetChild(0).GetComponent<ParticleSystem>();
                    Destroy(craterInstance, childParticle.main.duration);
                }
            }

            targetPosition = transform.position;
        }
    }
}
