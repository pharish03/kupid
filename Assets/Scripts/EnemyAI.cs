using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    // Patrols
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    public float timeBetweenAttacks;
    public bool alreadyAttacked;

    public float health;
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;

    public Animator anim;

    private bool isStaggered = false;
    private bool isDead = false;
    private bool isScared = false;
    public float staggerDuration = 0.5f;
    public float fleeDuration = 5f;
    private float fleeTimer = 0f;

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Stop all AI logic if dead or staggered
        if (isDead || isStaggered) return;

        // lil bro is hurt and needs to run away
        if (isScared)
        {
            FleePlayer();
            return;
        }

        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInSightRange && !playerInAttackRange) Patrolling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInAttackRange && playerInSightRange) AttackPlayer();
    }

    private void SetAnimationState(bool patrolling, bool chasing, bool attacking, bool gotHit, bool scared, bool died)
    {
        anim.SetBool("Patrolling", patrolling);
        anim.SetBool("Chasing", chasing);
        anim.SetBool("Attacking", attacking);
        anim.SetBool("gotHit", gotHit);
        anim.SetBool("Scared", scared);
        anim.SetBool("Died", died);
    }

    private void Patrolling()
    {
        if (!walkPointSet) SearchWalkPoint();
        if (walkPointSet) agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        agent.speed = 2f;

        SetAnimationState(true, false, false, false, false, false);

        if (distanceToWalkPoint.magnitude < 1f) walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
            walkPointSet = true;
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
        agent.speed = 7.5f;

        SetAnimationState(false, true, false, false, false, false);
    }

    private void AttackPlayer()
    {
        agent.SetDestination(transform.position);
        agent.speed = 0f;
        transform.LookAt(player);

        SetAnimationState(false, false, true, false, false, false);

        if (!alreadyAttacked)
        {
            // TODO: Implement sword attack HERE
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void FleePlayer()
    {
        fleeTimer += Time.deltaTime;

        if (fleeTimer >= fleeDuration)
        {
            isScared = false;
            fleeTimer = 0f;
            return;
        }

        Vector3 fleeDirection = (transform.position - player.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDirection * 10f;
        agent.SetDestination(fleeTarget);
        agent.speed = 7.5f;

        SetAnimationState(false, false, false, false, true, false);
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            // Dead
            isDead = true;
            agent.ResetPath();
            agent.speed = 0f;
            SetAnimationState(false, false, false, false, false, true);
            Invoke(nameof(DestroyEnemy), 2f);
        }
        else if (health <= 25 && health > 0)
        {
            // Scared — flee the player permanently
            isScared = true;
            fleeTimer = 0f;
            SetAnimationState(false, false, false, false, true, false);
        }
        else
        {
            // Stagger hit
            isStaggered = true;
            agent.ResetPath();
            agent.speed = 0f;
            SetAnimationState(false, false, false, true, false, false);
            Invoke(nameof(ResetStagger), staggerDuration);
        }
    }

    private void ResetStagger()
    {
        isStaggered = false;
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }
}