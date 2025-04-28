using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.0f; // Slightly faster movement
    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRenderer;

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRenderer = GetComponent<SpriteRenderer>();

        // Setup input handling
        playerControls.Player.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => movement = Vector2.zero;
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Update()
    {
        HandleAnimation();
        AdjustPlayerFacingDirection();
    }

    private void Move()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void HandleAnimation()
    {
        myAnimator.SetFloat("MoveX", movement.x);
        myAnimator.SetFloat("MoveY", movement.y);
        myAnimator.SetFloat("Speed", movement.sqrMagnitude);
    }


    private void AdjustPlayerFacingDirection()
    {
        if (movement.x < 0)
            mySpriteRenderer.flipX = true;
        else if (movement.x > 0)
            mySpriteRenderer.flipX = false;
    }
}

