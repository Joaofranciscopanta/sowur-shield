using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float walkSpeed = 5f;
    [SerializeField] public float sprintSpeed = 12f;
    [SerializeField] public float rotationSpeed = 720f;


    [Header("Dash Settings")]
    [SerializeField] private TrailRenderer tr;
    [SerializeField] private float dashingTime = 0.1f;
    [SerializeField] private float dashingPower = 8f;
    [SerializeField] private float dashingCooldown = 1f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform interactionPoint; // Optional: point from which to check interaction
    [SerializeField] private GameObject interactionEffectPrefab; // Optional: visual effect for interaction

    private Vector2 moveInput;
    private Rigidbody2D rb;
    private Animator animator;
    private bool facingRight = true;
    private bool isSprinting = false;
    private bool canDash = true;
    private bool isDashing;
    private bool movementEnabled = true;

    private Inventory inventory;

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<Inventory>();

        if (interactionPoint == null)
            interactionPoint = transform;
    }

    public void Update()
    {
        if (isDashing)
        {
            return;
        }

        if (moveInput != Vector2.zero)
{
    float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
    transform.rotation = Quaternion.RotateTowards(
        transform.rotation,
        Quaternion.Euler(0, 0, targetAngle),
        rotationSpeed * Time.deltaTime
    );
}

        animator.SetBool("isWalking", moveInput != Vector2.zero);
    }

    public void FixedUpdate()
    {
        if (!movementEnabled || isDashing)
        {
            return;
        }

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        rb.linearVelocity = moveInput * currentSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
{
    if (!movementEnabled)
    {
        moveInput = Vector2.zero;
        return;
    }
    
    moveInput = context.ReadValue<Vector2>();
    if (moveInput != Vector2.zero)
    {
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
    }

}

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isSprinting = !isSprinting;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!movementEnabled || !context.started || !canDash)
            return;
            
        StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        Vector2 dashDirection = moveInput.normalized;
        if (dashDirection == Vector2.zero)
            dashDirection = facingRight ? Vector2.right : Vector2.left;

        rb.linearVelocity = dashDirection * dashingPower;

        tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        tr.emitting = false;

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DetectAndInteract();
        }
    }

    private void DetectAndInteract()
    {
        if (InventorySlot.IsAnySlotDragging)
        {
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen())
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
        }

        // Use InteractionManager for proximity-based E key interactions (not cursor-based clicks)
        if (InteractionManager.Instance != null)
        {
            if (InteractionManager.Instance.CanInteract())
            {
                InteractionManager.Instance.TriggerInteraction();

                var currentInteractable = InteractionManager.Instance.GetCurrentInteractable();
                if (currentInteractable != null && currentInteractable is MonoBehaviour mb)
                {
                    ShowInteractionEffect(mb.transform.position);
                }

                return;
            }
        }

        // Fallback collision-based detection if InteractionManager unavailable
        Vector2 interactionSource = interactionPoint != null ?
            interactionPoint.position : transform.position;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(interactionSource, interactionRadius, interactableLayer);

        System.Array.Sort(colliders, (a, b) =>
            (Vector2.Distance(interactionSource, a.transform.position)
            .CompareTo(Vector2.Distance(interactionSource, b.transform.position))));

        foreach (Collider2D collider in colliders)
        {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                ShowInteractionEffect(collider.transform.position);
                break;
            }
        }
    }

    private void ShowInteractionEffect(Vector3 position)
    {
        if (interactionEffectPrefab != null)
        {
            GameObject effect = Instantiate(interactionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    // Movement control methods for UI interactions
    public void DisableMovement()
    {
        movementEnabled = false;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsMoving", false);
        }
    }

    public void EnableMovement()
    {
        movementEnabled = true;
    }

    public bool IsMovementEnabled()
    {
        return movementEnabled;
    }

    public Inventory GetInventory()
    {
        return inventory;
    }

    public Item GetSelectedItem()
    {
        if (inventory != null)
        {
            ItemStack selectedStack = inventory.SelectedItem;
            return selectedStack.IsEmpty ? null : selectedStack.item;
        }
        return null;
    }

    public ItemStack GetSelectedItemStack()
    {
        return inventory != null ? inventory.SelectedItem : new ItemStack();
    }
}
