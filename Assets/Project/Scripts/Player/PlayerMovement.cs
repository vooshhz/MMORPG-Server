using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    // Input handling
    private Vector2 moveInput;
    private PlayerControls controls;
    
    private void Awake()
    {
        controls = new PlayerControls();
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
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }
}