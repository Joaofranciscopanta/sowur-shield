using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using SowurShield.Inventory;
using SowurShield.Combat;

namespace SowurShield.Core
{
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

    // Referência ao inventário
    private SowurShield.Inventory.Inventory inventory;

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<SowurShield.Inventory.Inventory>();

        // Se o interactionPoint não for definido, use a posição do jogador
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

// Atualiza a animação com base no movimento
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
        // Don't interact if any slot is being dragged
        if (InventorySlot.IsAnySlotDragging)
        {
            return; // Don't interact when dragging items
        }

        // Don't interact if TeamAssemblerUI is open
        if (TeamAssemblerUI.Instance != null && TeamAssemblerUI.Instance.IsOpen())
        {
            return; // Don't interact when Team Assembler is open
        }

        // Don't interact if UI is active or if mouse is over UI
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen())
        {
            // Check if mouse is over UI element
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // Don't interact when mouse is over UI
            }
        }

        // Use InteractionManager for proximity-based E key interactions
        // Note: This is different from cursor-based left-click interactions
        if (InteractionManager.Instance != null)
        {
            if (InteractionManager.Instance.CanInteract())
            {
                InteractionManager.Instance.TriggerInteraction();

                // Show interaction effect at the current interactable's position
                var currentInteractable = InteractionManager.Instance.GetCurrentInteractable();
                if (currentInteractable != null && currentInteractable is MonoBehaviour mb)
                {
                    ShowInteractionEffect(mb.transform.position);
                }

                return; // InteractionManager handled the interaction
            }
        }

        // Fallback to old collision-based system if InteractionManager is not available
        // Obter o ponto de origem da interação (pode ser o centro do jogador ou um ponto à frente)
        Vector2 interactionSource = interactionPoint != null ?
            interactionPoint.position : transform.position;

        // Detectar todos os objetos interagíveis dentro do raio
        Collider2D[] colliders = Physics2D.OverlapCircleAll(interactionSource, interactionRadius, interactableLayer);

        // Ordenar por distância (para interagir sempre com o mais próximo primeiro)
        System.Array.Sort(colliders, (a, b) =>
            (Vector2.Distance(interactionSource, a.transform.position)
            .CompareTo(Vector2.Distance(interactionSource, b.transform.position))));

        foreach (Collider2D collider in colliders)
        {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Chamar a interação - cada objeto gerencia sua própria lógica
                interactable.Interact();

                // Mostrar efeito visual de interação (opcional)
                ShowInteractionEffect(collider.transform.position);

                // Parar após a primeira interação
                break;
            }
        }
    }

    private void ShowInteractionEffect(Vector3 position)
    {
        // Se um prefab de efeito estiver configurado, instancia-o
        if (interactionEffectPrefab != null)
        {
            GameObject effect = Instantiate(interactionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f); // Destroi após 1 segundo
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
        rb.linearVelocity = Vector2.zero; // Stop immediately

        // Update animator to idle (guard against missing parameters)
        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "Speed")    animator.SetFloat("Speed", 0f);
                if (param.name == "IsMoving") animator.SetBool("IsMoving", false);
            }
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

    // Método público para obter o inventário
    public SowurShield.Inventory.Inventory GetInventory()
    {
        return inventory;
    }

    // Método para acessar o item atualmente selecionado
    public Item GetSelectedItem()
    {
        if (inventory != null)
        {
            ItemStack selectedStack = inventory.SelectedItem;
            return selectedStack.IsEmpty ? null : selectedStack.item;
        }
        return null;
    }

    // Método para acessar o stack completo do item selecionado
    public ItemStack GetSelectedItemStack()
    {
        return inventory != null ? inventory.SelectedItem : new ItemStack();
    }
}
} // namespace SowurShield.Core
