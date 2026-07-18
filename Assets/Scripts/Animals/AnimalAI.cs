using UnityEngine;
using System.Collections;

namespace SowurShield.Animals
{

/// <summary>
/// Handles animal AI behaviors: wandering, idle states, and animations.
/// Works in conjunction with Animal.cs for complete animal behavior.
/// </summary>
[RequireComponent(typeof(Animal))]
[RequireComponent(typeof(Rigidbody2D))]
public class AnimalAI : MonoBehaviour
{
    private enum AIState
    {
        Idle,
        Wandering,
        Eating
    }

    private Animal animal;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AnimalData data;

    private AIState currentState = AIState.Idle;
    private Vector2 targetPosition;
    private float stateTimer;
    private bool isMoving = false;

    // Animation parameter names
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsIdleHash = Animator.StringToHash("IsIdle");
    private static readonly int IsEatingHash = Animator.StringToHash("IsEating");

    private void Awake()
    {
        animal = GetComponent<Animal>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Configure rigidbody
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Start()
    {
        if (animal == null || animal.AnimalData == null)
        {
            enabled = false;
            return;
        }

        data = animal.AnimalData;

        // Start with idle state
        ChangeState(AIState.Idle);
    }

    private void Update()
    {
        if (data == null) return;

        // Update state timer
        stateTimer -= Time.deltaTime;

        // State machine
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;

            case AIState.Wandering:
                UpdateWanderingState();
                break;

            case AIState.Eating:
                UpdateEatingState();
                break;
        }

        // Update animations
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (isMoving && currentState == AIState.Wandering)
        {
            MoveTowardsTarget();
        }
        else
        {
            // Stop movement
            rb.linearVelocity = Vector2.zero;
        }
    }

    #region State Management

    private void ChangeState(AIState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case AIState.Idle:
                EnterIdleState();
                break;

            case AIState.Wandering:
                EnterWanderingState();
                break;

            case AIState.Eating:
                EnterEatingState();
                break;
        }
    }

    #endregion

    #region Idle State

    private void EnterIdleState()
    {
        isMoving = false;
        stateTimer = Random.Range(data.minIdleTime, data.maxIdleTime);

        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsIdleHash, true);
        }
    }

    private void UpdateIdleState()
    {
        if (stateTimer <= 0f)
        {
            // Transition to wandering
            ChangeState(AIState.Wandering);
        }
    }

    #endregion

    #region Wandering State

    private void EnterWanderingState()
    {
        // Get random point in zone
        if (animal.AssignedZone != null)
        {
            targetPosition = animal.AssignedZone.GetRandomPointNear(transform.position, data.wanderRadius);
        }
        else
        {
            // Fallback: random point near current position
            Vector2 randomOffset = Random.insideUnitCircle * data.wanderRadius;
            targetPosition = (Vector2)transform.position + randomOffset;
        }

        isMoving = true;
        stateTimer = Random.Range(data.minWalkTime, data.maxWalkTime);

        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, true);
            animator.SetBool(IsIdleHash, false);
        }
    }

    private void UpdateWanderingState()
    {
        // Check if reached target or timer expired
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        if (distanceToTarget < 0.2f || stateTimer <= 0f)
        {
            // Transition back to idle
            ChangeState(AIState.Idle);
        }
    }

    private void MoveTowardsTarget()
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        Vector2 velocity = direction * data.moveSpeed;

        rb.linearVelocity = velocity;

        // Flip sprite based on direction
        if (spriteRenderer != null)
        {
            if (direction.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (direction.x > 0)
            {
                spriteRenderer.flipX = false;
            }
        }
    }

    #endregion

    #region Eating State

    private void EnterEatingState()
    {
        isMoving = false;
        stateTimer = 2f; // Eating duration

        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsIdleHash, false);
            animator.SetBool(IsEatingHash, true);
        }
    }

    private void UpdateEatingState()
    {
        if (stateTimer <= 0f)
        {
            if (animator != null)
            {
                animator.SetBool(IsEatingHash, false);
            }

            // Transition to idle
            ChangeState(AIState.Idle);
        }
    }

    /// <summary>
    /// Trigger eating state from external script (e.g., when fed)
    /// </summary>
    public void TriggerEating()
    {
        ChangeState(AIState.Eating);
    }

    #endregion

    #region Animation Updates

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Update animator parameters based on current state
        // Already handled in state enter methods, but can add runtime updates here if needed
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // Draw wander radius
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, data.wanderRadius);

        // Draw target position when wandering
        if (currentState == AIState.Wandering)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Force the animal to stop and idle
    /// </summary>
    public void ForceIdle()
    {
        ChangeState(AIState.Idle);
    }

    /// <summary>
    /// Get current AI state
    /// </summary>
    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    #endregion
}

} // namespace SowurShield.Animals
