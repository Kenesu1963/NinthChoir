using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpForceMultiplier = 0.707f; // 0.707 = half the height of the first jump

    [Header("Gravity")]
    [SerializeField] private float baseGravity = 3f;
    [SerializeField] private float lowJumpMultiplier = 2.5f;
    [SerializeField] private float fallMultiplier = 2f;
    [SerializeField] private float maxFallSpeed = 25f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 1f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeightMultiplier = 0.75f; // hitbox is 3/4 of its normal height
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;   // walking speed while crouched

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugHUD = true;

    private Rigidbody2D rb;
    private float moveInput;
    private float verticalInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool facingRight = true;

    private bool isDashing;
    private bool airDashAvailable = true;
    private bool doubleJumpAvailable = true;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    // Crouch
    private BoxCollider2D boxCollider;
    private CapsuleCollider2D capsuleCollider;
    private Collider2D bodyCollider;
    private Vector2 standingSize;
    private Vector2 standingOffset;
    private bool isCrouching;

    // Recoil (pushback after a rapier hit)
    private float recoilVelocityX;
    private float recoilTimer;
    private float recoilTotal;

    // --- Read by PlayerCombat ---
    public bool IsDashing => isDashing;
    public bool IsCrouching => isCrouching;
    public bool IsGrounded => isGrounded;
    public bool FacingRight => facingRight;
    public float SpeedMultiplier { get; set; } = 1f;

    // --- Clinging to a wall (used by the Pick tool) ---
    private bool isClinging;
    private bool clingLockout; // after jumping off, you must let go of the button and press it again

    public bool IsClinging => isClinging;
    public Bounds BodyBounds => bodyCollider != null ? bodyCollider.bounds : new Bounds(transform.position, Vector3.one);

    /// <summary>Sticks the player to a wall. wallSide: -1 = wall on the left, +1 = wall on the right.</summary>
    public bool TryBeginCling(int wallSide)
    {
        if (isClinging) return true;
        if (clingLockout || isDashing || InputLock.IsLocked) return false;

        isClinging = true;
        if (wallSide > 0 && !facingRight) Flip();
        else if (wallSide < 0 && facingRight) Flip();
        rb.linearVelocity = Vector2.zero;
        return true;
    }

    /// <summary>Moves the player somewhere instantly and clears dashing, clinging, recoil and speed. Used when respawning.</summary>
    public void TeleportTo(Vector2 position)
    {
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        isClinging = false;
        clingLockout = false;
        recoilTimer = 0f;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;

        if (isCrouching)
        {
            isCrouching = false;
            ApplyColliderHeight(1f);
        }

        rb.linearVelocity = Vector2.zero;
        rb.position = position;
        transform.position = position;
    }

    /// <summary>Stops clinging (the wall disappeared). Does not reset the jump lockout.</summary>
    public void StopCling()
    {
        isClinging = false;
    }

    /// <summary>The pick button was released: stop clinging and allow clinging again next time.</summary>
    public void ReleaseCling()
    {
        isClinging = false;
        clingLockout = false;
    }

    /// <summary>Direction of the held keys (8 directions). With no direction held, the way you're facing.</summary>
    public Vector2 AimDirection
    {
        get
        {
            Vector2 dir = new Vector2(moveInput, verticalInput);
            if (isGrounded && dir.y < 0f) dir.y = 0f; // on the ground, "down" means crouch, not aiming into the floor
            if (dir == Vector2.zero) dir = facingRight ? Vector2.right : Vector2.left;
            return dir.normalized;
        }
    }

    /// <summary>Pushes the player away briefly. X fades out over the duration; Y is applied once (small hop).</summary>
    public void ApplyRecoil(Vector2 recoil, float duration)
    {
        if (Mathf.Abs(recoil.x) > 0.01f)
        {
            recoilVelocityX = recoil.x;
            recoilTotal = Mathf.Max(duration, 0.01f);
            recoilTimer = recoilTotal;
        }
        if (Mathf.Abs(recoil.y) > 0.01f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, recoil.y);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        boxCollider = GetComponent<BoxCollider2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (boxCollider != null)
        {
            bodyCollider = boxCollider;
            standingSize = boxCollider.size;
            standingOffset = boxCollider.offset;
        }
        else if (capsuleCollider != null)
        {
            bodyCollider = capsuleCollider;
            standingSize = capsuleCollider.size;
            standingOffset = capsuleCollider.offset;
        }
        else
        {
            Debug.LogWarning("PlayerController: crouch needs a BoxCollider2D or CapsuleCollider2D on the Player.");
        }
    }

    private void Update()
    {
        // --- Input: read keys directly ---
        moveInput = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput -= 1f;

        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) verticalInput -= 1f;

        jumpPressed = Input.GetKeyDown(KeyCode.Space);
        jumpHeld = Input.GetKey(KeyCode.Space);

        // --- Talking to an NPC / reading a sign: the player stands still ---
        if (InputLock.IsLocked)
        {
            moveInput = 0f;
            verticalInput = 0f;
            jumpPressed = false;
            jumpHeld = false;
        }

        // --- Ground check ---
        isGrounded = CheckGrounded();
        if ((isGrounded || isClinging) && !isDashing) // clinging counts like standing on something
        {
            airDashAvailable = true;
            doubleJumpAvailable = true;
        }

        // --- Crouch ---
        UpdateCrouch();

        // --- Timers ---
        if (isGrounded || isClinging) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        dashCooldownTimer -= Time.deltaTime;

        // --- Flip sprite ---
        if (moveInput > 0f && !facingRight) Flip();
        else if (moveInput < 0f && facingRight) Flip();

        // --- Start dash (Shift) ---
        bool dashPressed = (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && !InputLock.IsLocked;
        if (dashPressed && !isDashing && dashCooldownTimer <= 0f && (isGrounded || airDashAvailable))
        {
            StartDash();
        }

        // --- While dashing ---
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();
            return;
        }

        // --- Jump ---
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // use rb.velocity on Unity 2022 or older
            if (isClinging)
            {
                isClinging = false;
                clingLockout = true; // jumping off a wall: no sticking again until you re-press the button
            }
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
        // --- Double jump ---
        else if (enableDoubleJump && jumpPressed && !isGrounded && coyoteCounter <= 0f && doubleJumpAvailable)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * doubleJumpForceMultiplier);
            doubleJumpAvailable = false;
            jumpBufferCounter = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        if (isClinging)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speed = moveSpeed * SpeedMultiplier * (isCrouching ? crouchSpeedMultiplier : 1f);
        float xVelocity = moveInput * speed;
        if (recoilTimer > 0f)
        {
            recoilTimer -= Time.fixedDeltaTime;
            xVelocity = recoilVelocityX * Mathf.Clamp01(recoilTimer / recoilTotal);
        }
        rb.linearVelocity = new Vector2(xVelocity, rb.linearVelocity.y);

        float yVel = rb.linearVelocity.y;
        if (yVel > 0.01f)
            rb.gravityScale = jumpHeld ? baseGravity : baseGravity * lowJumpMultiplier;
        else if (yVel < -0.01f)
            rb.gravityScale = jumpHeld ? baseGravity : baseGravity * fallMultiplier;
        else
            rb.gravityScale = baseGravity;

        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    // ---------------- Crouch ----------------

    private void UpdateCrouch()
    {
        if (bodyCollider == null) return;

        bool wantsCrouch = GameControls.Held(GameAction.Crouch) && isGrounded && !isDashing && !isClinging && !InputLock.IsLocked;

        if (wantsCrouch && !isCrouching)
        {
            isCrouching = true;
            ApplyColliderHeight(crouchHeightMultiplier);
        }
        else if (!wantsCrouch && isCrouching && HasRoomToStand())
        {
            isCrouching = false;
            ApplyColliderHeight(1f);
        }
    }

    // Shrinks the collider from the top so the feet stay on the floor.
    private void ApplyColliderHeight(float multiplier)
    {
        Vector2 size = new Vector2(standingSize.x, standingSize.y * multiplier);
        Vector2 offset = new Vector2(standingOffset.x, standingOffset.y - (standingSize.y - size.y) * 0.5f);

        if (boxCollider != null)
        {
            boxCollider.size = size;
            boxCollider.offset = offset;
        }
        else if (capsuleCollider != null)
        {
            capsuleCollider.size = size;
            capsuleCollider.offset = offset;
        }
    }

    // Prevents standing up inside a low ceiling.
    private bool HasRoomToStand()
    {
        Bounds b = bodyCollider.bounds;
        float standingHeight = b.size.y / crouchHeightMultiplier;
        float extra = standingHeight - b.size.y;

        Vector2 center = new Vector2(b.center.x, b.max.y + extra * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.9f, extra * 0.9f);

        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            if (hit.isTrigger) continue;
            if (hit.attachedRigidbody == rb) continue;
            return false;
        }
        return true;
    }

    // ---------------- Ground / dash / flip ----------------

    private bool CheckGrounded()
    {
        if (groundCheck == null) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.isTrigger) continue;
            if (hit.attachedRigidbody == rb) continue;
            return true;
        }
        return false;
    }

    private void StartDash()
    {
        Vector2 dir = new Vector2(moveInput, verticalInput);
        if (dir == Vector2.zero) dir = facingRight ? Vector2.right : Vector2.left;

        dashDirection = dir.normalized;
        isClinging = false;
        isDashing = true;
        dashTimer = dashDuration;
        if (!isGrounded) airDashAvailable = false;
    }

    private void EndDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = Vector2.zero;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void OnGUI()
    {
        if (!showDebugHUD) return;
        GUI.color = Color.black;
        GUI.Label(new Rect(12, 10, 900, 30),
            "grounded: " + isGrounded +
            "   crouch: " + isCrouching +
            "   horiz: " + moveInput +
            "   vert: " + verticalInput +
            "   space: " + jumpHeld +
            "   dashing: " + isDashing +
            "   airJump: " + doubleJumpAvailable);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}