using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMove : NetworkBehaviour  
{
    [SerializeField] float movementSpeedBase = 5;

    private Rigidbody2D rb;
    private float movementSpeedMultiplier = 1f;
    private Vector2 currentMoveDirection;
    private Vector2 moveInput;
    private PlayerInput playerInput;
    public int playerScore;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        
        // Disable input initially, will be enabled in OnNetworkSpawn for owner
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        
        // Enable input for the owner with a small delay to ensure proper setup
        StartCoroutine(EnableInputAfterSpawn());
    }
    
    private IEnumerator EnableInputAfterSpawn()
    {
        // Wait a frame to ensure everything is properly initialized
        yield return null;
        
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }
        else
        {
            // Try to get the component again if it was null
            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        Move();
    }

    private void Move()
    {
        if (rb == null) return;
        
        Vector2 moveVector = moveInput.normalized * movementSpeedBase * movementSpeedMultiplier;

        // Use transform.Translate instead of rigidbody for more control
        Vector3 movement = new Vector3(moveVector.x, moveVector.y, 0) * Time.deltaTime;
        transform.Translate(movement);

        if (moveVector != Vector2.zero)
        {
            currentMoveDirection = new Vector2(moveVector.normalized.x, moveVector.normalized.y);
        }
    }
    
    // Called by the new Input System
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        
        moveInput = value.Get<Vector2>();
    }
}