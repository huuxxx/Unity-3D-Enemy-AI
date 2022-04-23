using System.Collections;
using UnityEngine;

public class Spell_FireWave : MonoBehaviour
{
    [SerializeField]
    private float damage, speed, explosionInterval, explosionRadius, overShootDistance;

    [SerializeField]
    private GameObject fireExplosion;

    [SerializeField]
    private LayerMask targetLayer;

    private Vector3 targetPosition;

    private bool targetReached;

    private const float CloseToTarget = 0.1f;

    public void SetTarget(Transform targetTransform)
    {
        gameObject.transform.LookAt(targetTransform.position);
        targetPosition = targetTransform.position;
        StartCoroutine(SpawnExplosion());
    }

    private void Update()
    {
        if (!targetReached && Vector3.Distance(transform.position, targetPosition) < CloseToTarget)
            targetReached = true;

        if (targetReached && Vector3.Distance(transform.position, targetPosition) > overShootDistance)
            Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        gameObject.transform.position += transform.forward * (speed * Time.deltaTime);
    }

    private IEnumerator SpawnExplosion()
    {
        yield return new WaitForSeconds(explosionInterval);
        Vector3 spawnPosition = transform.position;
        spawnPosition.y = Terrain.activeTerrain.SampleHeight(transform.position);
        Instantiate(fireExplosion, spawnPosition, Quaternion.identity);
        Collider[] targetsHit = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);

        foreach (Collider target in targetsHit)
        {
            target.GetComponent<PlayerHealth>().TakeDamage(damage);
        }

        StartCoroutine(SpawnExplosion());
    }
}
