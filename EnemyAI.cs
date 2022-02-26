using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [SerializeField, Tooltip("How much rage the player gains from killing this")]
    private float rageGain;

    [SerializeField, Tooltip("Range to engage the player")]
    private float sightRange, attackRange;

    [SerializeField, Tooltip("How far the AI will search for a new random patrol location")]
    private float patrolRange;

    [SerializeField, Tooltip("Enemy hit points")]
    private float hitPoints;

    [SerializeField, Tooltip("How much damage attack [x] deals")]
    private float[] attackDamage;

    [SerializeField, Tooltip("The full animation clip duration of attack animation [x]")]
    private float[] attackCooldown;

    [SerializeField, Tooltip("The apex of attack animation [x] - when to deal damage")]
    private float[] attackTiming;

    [SerializeField, Tooltip("The central location where the attack sphere will be created")]
    private Transform attackPosition;

    [SerializeField, Tooltip("The speed which the enemy moves while patroling")]
    private float walkSpeed;

    [SerializeField, Tooltip("The step timing of the walk animation")]
    private float walkSoundInterval;

    [SerializeField, Tooltip("The speed which the enemy moves while pursuing the player")]
    private float runSpeed;

    [SerializeField, Tooltip("The step timing of the run animation")]
    private float runSoundInterval;

    [SerializeField, Range(1, 10), Tooltip("How many death animations are attached to the controller")]
    private int numberOfDeathAnims;

    [SerializeField, Range(1, 10), Tooltip("How many enemy hit animations are attached to the controller")]
    private int numberOfHitAnims;

    [SerializeField, Range(1, 10), Tooltip("How often between idle SFX triggers")]
    private float idleSoundsCoolDown;

    [SerializeField, Tooltip("Enemy attack SFX (on attack)")]
    private AudioClip[] attackSounds;

    [SerializeField, Tooltip("Enemy death SFX (on death")]
    private AudioClip[] deathSounds;

    [SerializeField, Tooltip("Enemy threaten SFX (on chase)")]
    private AudioClip[] threatenSounds;

    [SerializeField, Tooltip("Enemy pain SFX (on receieve damage)")]
    private AudioClip[] painSounds;

    [SerializeField, Tooltip("Enemy idle SFX (on patrol)")]
    private AudioClip[] idleSounds;

    [SerializeField, Tooltip("Enemy footstep SFX")]
    private AudioClip[] footstepSounds;

    [SerializeField]
    private LayerMask groundLayer, playerLayer;
    public enum EnemyState { Patroling, Chasing, Attacking, TakenDamage, Dead };

    public EnemyState enemyState;

    private NavMeshAgent agent;

    private Transform playerTransform;

    private Animator animator;

    private CapsuleCollider sphereCollider;

    private Rigidbody rigidbody;

    private AudioSource audio;

    private PlayerRage playerRage;

    private Vector3 walkPoint;

    private bool playerInSightRange, playerInAttackRange, patrolPointSet, isAttacking, threatenSoundPlayed, painSoundPlaying, idleSoundPlaying, footstepSoundPlaying;

    private string currentAnimation;

    private int updateInterval = 3;

    private void Awake()
    {
        GameObject player = GameObject.Find("Player");
        playerTransform = player.transform;
        playerRage = player.GetComponent<PlayerRage>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        sphereCollider = GetComponent<CapsuleCollider>();
        rigidbody = GetComponent<Rigidbody>();
        audio = GetComponent<AudioSource>();
        patrolPointSet = false;
        playerInSightRange = false;
        playerInAttackRange = false;
        isAttacking = false;
        threatenSoundPlayed = false;
        painSoundPlaying = false;
        footstepSoundPlaying = false;
    }

    private void Update()
    {
        if (Time.frameCount % this.updateInterval != 0) return;
        
        if (enemyState != EnemyState.Dead)
        {
            playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playerLayer);
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerLayer);

            if (playerInSightRange && playerInAttackRange)
            {
                enemyState = EnemyState.Attacking;
                Attacking();
                yield return new WaitForSeconds(LogicLoopInterval);
                StartCoroutine(LogicLoop());
            }
            
            if (playerInSightRange && !playerInAttackRange)
            {
                enemyState = EnemyState.Chasing;
                Chasing();
                yield return new WaitForSeconds(LogicLoopInterval);
                StartCoroutine(LogicLoop());
            }
            
            if (!playerInSightRange && !playerInAttackRange)
            {
                enemyState = EnemyState.Patroling;
                Patroling();
                yield return new WaitForSeconds(LogicLoopInterval);
                StartCoroutine(LogicLoop());
            }
        }
    }

    private void Patroling()
    {
        agent.speed = walkSpeed;
        AnimationTriggerHelper("walk");

        if (!footstepSoundPlaying)
        {
            footstepSoundPlaying = true;
            audio.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)]);
            StartCoroutine(FootStepsSoundCooldown(walkSoundInterval));
        }

        if (!idleSoundPlaying && idleSounds.Length > 0)
        {
            idleSoundPlaying = true;
            int randomClip = Random.Range(0, idleSounds.Length);
            audio.PlayOneShot(idleSounds[randomClip]);
            StartCoroutine(IdleSoundCooldown(idleSounds[randomClip].length));
        }

        if (!patrolPointSet) SearchPath();

        if (patrolPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToPatrolPoint = transform.position - walkPoint;

        if (distanceToPatrolPoint.magnitude < 1f)
            patrolPointSet = false;
    }

    private void SearchPath()
    {
        float randomZ = Random.Range(-patrolRange, patrolRange);
        float randomX = Random.Range(-patrolRange, patrolRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, Vector3.down, groundLayer))
            patrolPointSet = true;
    }

    private void Chasing()
    {
        if (!footstepSoundPlaying)
        {
            footstepSoundPlaying = true;
            audio.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)]);
            StartCoroutine(FootStepsSoundCooldown(runSoundInterval));
        }

        if (!threatenSoundPlayed)
        {
            threatenSoundPlayed = true;
            if (threatenSounds.Length > 0)
                audio.PlayOneShot(threatenSounds[Random.Range(0, threatenSounds.Length)]);
        }

        agent.speed = runSpeed;
        agent.SetDestination(playerTransform.position);
        AnimationTriggerHelper("run");
    }

    private void Attacking()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            agent.SetDestination(transform.position);
            transform.LookAt(playerTransform);
            int attackId = Random.Range(0, (attackDamage.Length - 1));
            AnimationTriggerHelper("attack" + (attackId + 1));
            StartCoroutine(AttackSphere(attackDamage[attackId], attackTiming[attackId]));
            StartCoroutine(AttackCooldown(attackCooldown[attackId]));
        }
    }

    private IEnumerator AttackSphere(float damage, float timing)
    {
        yield return new WaitForSeconds(timing);
        Collider[] hitPlayer = Physics.OverlapSphere(attackPosition.position, (attackRange / 2.5f), playerLayer);
        foreach (Collider player in hitPlayer)
        {
            player.GetComponent<PlayerHealth>().TakeDamage(damage);
        }
    }

    private IEnumerator AttackCooldown(float cooldown)
    {
        audio.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)]);
        yield return new WaitForSeconds(cooldown);
        isAttacking = false;
    }

    private void Dead(bool? gainRageFromKill = true)
    {
        audio.PlayOneShot(deathSounds[Random.Range(0, deathSounds.Length)]);
        rigidbody.freezeRotation = true;
        rigidbody.isKinematic = true;
        AnimationTriggerHelper("death" + Random.Range(1, numberOfDeathAnims));
        sphereCollider.enabled = false;

        if ((bool)gainRageFromKill)
            playerRage.AddRage(rageGain);

        agent.SetDestination(transform.position);
    }

    public void TakeDamage(float damage, bool? gainRageFromKill = true)
    {
        hitPoints -= damage;

        if (hitPoints <= 0)
        {
            enemyState = EnemyState.Dead;
            Dead(gainRageFromKill);
            return;
        }

        if (!painSoundPlaying)
        {
            painSoundPlaying = true;
            int randomClip = Random.Range(0, painSounds.Length);
            audio.PlayOneShot(painSounds[randomClip]);
            StartCoroutine(PainSoundCooldown(painSounds[randomClip].length));
        }
    }

    public void AlertEnemy()
    {
        sightRange = 1000f;
    }

    private IEnumerator FootStepsSoundCooldown(float duration)
    {
        yield return new WaitForSeconds(duration);
        footstepSoundPlaying = false;
    }

    private IEnumerator IdleSoundCooldown(float duration)
    {
        yield return new WaitForSeconds(duration + idleSoundsCoolDown);
        idleSoundPlaying = false;
    }

    private IEnumerator PainSoundCooldown(float duration)
    {
        yield return new WaitForSeconds(duration);
        painSoundPlaying = false;
    }
    private IEnumerator DamageAnimationDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        enemyState = EnemyState.Chasing;
    }

    private void AnimationTriggerHelper(string animToPlay)
    {
        if (currentAnimation != null)
            animator.SetBool(currentAnimation, false);

        animator.SetBool(animToPlay, true);
        currentAnimation = animToPlay;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(attackPosition.transform.position, (attackRange / 2.5f));
    }
}
