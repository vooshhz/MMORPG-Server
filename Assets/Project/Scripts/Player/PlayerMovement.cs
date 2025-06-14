using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{

    private PlayerFacing previousFacing = PlayerFacing.Down;
    private Vector2 lastNonZeroInput = Vector2.down; // Default to facing down
    private Vector2 previousInput = Vector2.zero;
    [SerializeField] private float moveSpeed = 5f;
    
    // Input handling
    private Vector2 moveInput;
    private PlayerControls controls;
    
    // Animation handling
    private CharacterAnimator characterAnimator;
    
    // Current facing direction (initialized to down)
    [SyncVar] 
    private PlayerFacing currentFacing = PlayerFacing.Down;
    
    // Current movement state
    [SyncVar(hook = nameof(OnCharacterStateChanged))] 
    private CharacterState currentState = CharacterState.Idle;
    
    // Track previous state to detect changes
    // private CharacterState previousState = CharacterState.Idle;
    
    private void Awake()
    {
        controls = new PlayerControls();
        characterAnimator = GetComponentInChildren<CharacterAnimator>();
        
        if (characterAnimator == null)
        {
            Debug.LogError("CharacterAnimator component not found! Make sure it's attached to this GameObject or a child.");
        }
    }
    
    private void OnEnable()
    {
        controls.Enable();
    }
    
    private void OnDisable()
    {
        controls.Disable();
    }
    
    public override void OnStartAuthority()
    {
        // Subscribe to input events
        controls.Movement.Move.performed += OnMovePerformed;
        controls.Movement.Move.canceled += OnMoveCanceled;
    }
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        moveInput = context.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        moveInput = Vector2.zero;
    }
    
    private void Update()
    {
        if (isLocalPlayer)
        {
            MovePlayer();
        }
    }
    
  private void MovePlayer()
    {
        // Calculate movement
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;
        
        // Determine if moving or idle
        bool isMoving = moveInput.magnitude > 0.1f;
        
        // Update character state
        CharacterState newState = isMoving ? CharacterState.Running : CharacterState.Idle;
        
        // Determine facing direction when moving
        PlayerFacing newFacing = currentFacing; // Start with current facing
        
        if (isMoving)
        {
            // Get the current input directions (non-zero components)
            bool hasHorizontalInput = Mathf.Abs(moveInput.x) > 0.1f;
            bool hasVerticalInput = Mathf.Abs(moveInput.y) > 0.1f;
            
            // Determine primary facing direction based on input magnitudes
            if (hasHorizontalInput && hasVerticalInput)
            {
                // If both directions have input, use whichever has larger magnitude
                if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                {
                    newFacing = moveInput.x > 0 ? PlayerFacing.Right : PlayerFacing.Left;
                }
                else
                {
                    newFacing = moveInput.y > 0 ? PlayerFacing.Up : PlayerFacing.Down;
                }
            }
            else if (hasHorizontalInput)
            {
                // Only horizontal movement
                newFacing = moveInput.x > 0 ? PlayerFacing.Right : PlayerFacing.Left;
            }
            else if (hasVerticalInput)
            {
                // Only vertical movement
                newFacing = moveInput.y > 0 ? PlayerFacing.Up : PlayerFacing.Down;
            }
        }
        
        // Update animation state ONLY if state or facing changed
        bool stateChanged = newState != currentState;
        bool facingChanged = newFacing != currentFacing;
        
        if (stateChanged || facingChanged)
        {
            // Apply the changes
            currentState = newState;
            currentFacing = newFacing;
            
            // Apply the animation
            UpdateAnimation();
            
            // If we have authority, update the server with our new state
            if (isLocalPlayer)
            {
                CmdUpdateCharacterState(currentState, currentFacing);
            }
        }
    }
    // Apply the current animation based on state and facing
    private void UpdateAnimation()
    {
        if (characterAnimator == null) return;
        
        // Apply animation based on state
        characterAnimator.ApplyCharacterState(currentState, currentFacing);
    }
    
    // Called on all clients when the synced character state changes
    private void OnCharacterStateChanged(CharacterState oldState, CharacterState newState)
    {
        // Update visual state on all clients
        UpdateAnimation();
    }
    
    // Command to update the character's state on the server
    [Command]
    private void CmdUpdateCharacterState(CharacterState newState, PlayerFacing newFacing)
    {
        // Update the synced state properties
        currentState = newState;
        currentFacing = newFacing;
    }
}