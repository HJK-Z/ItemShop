using UnityEngine;
using UnityEngine.Events;

[
    RequireComponent(
        typeof (CharacterController),
        typeof (PlayerInputHandler),
        typeof (AudioSource))
]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Tooltip("Audio source for footsteps, jump, etc...")]
    public AudioSource audioSource;

    [Header("General")]
    public float gravityDownForce = 20f;

    public LayerMask groundCheckLayers = -1;

    public float groundCheckDistance = 0.05f;

    [Header("Movement")]
    public float maxSpeedOnGround = 10f;

    public float movementSharpnessOnGround = 15;

    [Range(0, 1)]
    public float maxSpeedCrouchedRatio = 0.5f;

    public float maxSpeedInAir = 10f;

    public float accelerationSpeedInAir = 25f;

    public float sprintSpeedModifier = 2f;

    public float killHeight = -50f;

    [Header("Rotation")]
    public float rotationSpeed = 2000f;

    [Header("Jump")]
    public float jumpForce = 9f;

    [Header("Stance")]
    public float cameraHeightRatio = 0.9f;

    public float capsuleHeightStanding = 1.8f;

    public float capsuleHeightCrouching = 0.9f;

    public float crouchingSharpness = 10f;

    [Header("Audio")]
    public float footstepSFXFrequency = 1f;

    public float footstepSFXFrequencyWhileSprinting = 1f;

    public AudioClip footstepSFX;

    public AudioClip jumpSFX;

    public AudioClip landSFX;

    public AudioClip fallDamageSFX;

    [Header("Fall Damage")]
    public bool recievesFallDamage;

    public float minSpeedForFallDamage = 10f;

    public float maxSpeedForFallDamage = 30f;

    public float fallDamageAtMinSpeed = 10f;

    public float fallDamageAtMaxSpeed = 50f;

    public PlayerInventory inventory;

    public UnityAction<bool> onStanceChanged;

    public Vector3 characterVelocity { get; set; }

    public bool isGrounded { get; private set; }

    public bool hasJumpedThisFrame { get; private set; }

    public bool isDead { get; private set; }

    public bool isCrouching { get; private set; }

    public bool isInventoryOpen { get; private set; }

    Health m_Health;

    PlayerInputHandler m_InputHandler;

    CharacterController m_Controller;

    Actor m_Actor;

    Vector3 m_GroundNormal;

    Vector3 m_CharacterVelocity;

    Vector3 m_LatestImpactSpeed;

    float m_LastTimeJumped = 0f;

    float m_CameraVerticalAngle = 0f;

    float m_footstepDistanceCounter;

    float m_TargetCharacterHeight;

    const float k_JumpGroundingPreventionTime = 0.2f;

    const float k_GroundCheckDistanceInAir = 0.07f;

    void Start()
    {
        // fetch components on the same gameObject
        m_Controller = GetComponent<CharacterController>();
        DebugUtility
            .HandleErrorIfNullGetComponent
            <CharacterController, PlayerCharacterController>(m_Controller,
            this,
            gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility
            .HandleErrorIfNullGetComponent
            <PlayerInputHandler, PlayerCharacterController>(m_InputHandler,
            this,
            gameObject);

        m_Health = GetComponent<Health>();
        DebugUtility
            .HandleErrorIfNullGetComponent
            <Health, PlayerCharacterController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility
            .HandleErrorIfNullGetComponent
            <Actor, PlayerCharacterController>(m_Actor, this, gameObject);

        m_Controller.enableOverlapRecovery = true;

        m_Health.onDie += OnDie;

        // force the crouch state to false when starting
        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);
    }

    void Update()
    {
        // check for Y kill
        if (!isDead && transform.position.y < killHeight)
        {
            m_Health.Kill();
        }

        hasJumpedThisFrame = false;

        bool wasGrounded = isGrounded;
        GroundCheck();

        // landing
        if (isGrounded && !wasGrounded)
        {
            // Fall damage
            float fallSpeed =
                -Mathf.Min(characterVelocity.y, m_LatestImpactSpeed.y);
            float fallSpeedRatio =
                (fallSpeed - minSpeedForFallDamage) /
                (maxSpeedForFallDamage - minSpeedForFallDamage);
            if (recievesFallDamage && fallSpeedRatio > 0f)
            {
                float dmgFromFall =
                    Mathf
                        .Lerp(fallDamageAtMinSpeed,
                        fallDamageAtMaxSpeed,
                        fallSpeedRatio);
                m_Health.TakeDamage(dmgFromFall, null);

                audioSource.PlayOneShot (fallDamageSFX);
            }
            else
            {
                audioSource.PlayOneShot (landSFX);
            }
        }

        // crouching
        if (m_InputHandler.GetCrouchInputDown())
        {
            SetCrouchingState(!isCrouching, false);
        }

        // inventory
        if (m_InputHandler.GetInventoryInputDown())
        {
            SetInventoryState(!isInventoryOpen);
        }

        UpdateCharacterHeight(false);

        HandleCharacterMovement();
    }

    void OnDie()
    {
        isDead = true;
    }

    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance =
            isGrounded
                ? (m_Controller.skinWidth + groundCheckDistance)
                : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        isGrounded = false;
        m_GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
        {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (
                Physics
                    .CapsuleCast(GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(m_Controller.height),
                    m_Controller.radius,
                    Vector3.down,
                    out RaycastHit hit,
                    chosenGroundCheckDistance,
                    groundCheckLayers,
                    QueryTriggerInteraction.Ignore)
            )
            {
                // storing the upward direction for the surface found
                m_GroundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (
                    Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(m_GroundNormal)
                )
                {
                    isGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > m_Controller.skinWidth)
                    {
                        m_Controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    void HandleCharacterMovement()
    {
        // horizontal character rotation
        {
            // rotate the transform with the input speed around its local Y axis
            transform
                .Rotate(new Vector3(0f,
                    (m_InputHandler.GetLookInputsHorizontal() * rotationSpeed),
                    0f),
                Space.Self);
        }

        // vertical camera rotation
        {
            // add vertical inputs to the camera's vertical angle
            m_CameraVerticalAngle +=
                m_InputHandler.GetLookInputsVertical() * rotationSpeed;

            // limit the camera's vertical angle to min/max
            m_CameraVerticalAngle =
                Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

            // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
            playerCamera.transform.localEulerAngles =
                new Vector3(m_CameraVerticalAngle, 0, 0);
        }

        // character movement handling
        bool isSprinting = m_InputHandler.GetSprintInputHeld();
        {
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }

            float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

            // converts move input to a worldspace vector based on our character's transform orientation
            Vector3 worldspaceMoveInput =
                transform.TransformVector(m_InputHandler.GetMoveInput());

            // handle grounded movement
            if (isGrounded)
            {
                // calculate the desired velocity from inputs, max speed, and current slope
                Vector3 targetVelocity =
                    worldspaceMoveInput * maxSpeedOnGround * speedModifier;

                // reduce speed if crouching by crouch speed ratio
                if (isCrouching) targetVelocity *= maxSpeedCrouchedRatio;
                targetVelocity =
                    GetDirectionReorientedOnSlope(targetVelocity.normalized,
                    m_GroundNormal) *
                    targetVelocity.magnitude;

                // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                characterVelocity =
                    Vector3
                        .Lerp(characterVelocity,
                        targetVelocity,
                        movementSharpnessOnGround * Time.deltaTime);

                // jumping
                if (isGrounded && m_InputHandler.GetJumpInputDown())
                {
                    // force the crouch state to false
                    if (SetCrouchingState(false, false))
                    {
                        // start by canceling out the vertical component of our velocity
                        characterVelocity =
                            new Vector3(characterVelocity.x,
                                0f,
                                characterVelocity.z);

                        // then, add the jumpSpeed value upwards
                        characterVelocity += Vector3.up * jumpForce;

                        // play sound
                        audioSource.PlayOneShot (jumpSFX);

                        // remember last time we jumped because we need to prevent snapping to ground for a short time
                        m_LastTimeJumped = Time.time;
                        hasJumpedThisFrame = true;

                        // Force grounding to false
                        isGrounded = false;
                        m_GroundNormal = Vector3.up;
                    }
                }

                // footsteps sound
                float chosenFootstepSFXFrequency =
                    (
                    isSprinting
                        ? footstepSFXFrequencyWhileSprinting
                        : footstepSFXFrequency
                    );
                if (m_footstepDistanceCounter >= 1f / chosenFootstepSFXFrequency
                )
                {
                    m_footstepDistanceCounter = 0f;
                    audioSource.PlayOneShot (footstepSFX);
                }

                // keep track of distance traveled for footsteps sound
                m_footstepDistanceCounter +=
                    characterVelocity.magnitude * Time.deltaTime;
            }
            else
            // handle air movement
            {
                // add air acceleration
                characterVelocity +=
                    worldspaceMoveInput *
                    accelerationSpeedInAir *
                    Time.deltaTime;

                // limit air speed to a maximum, but only horizontally
                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity =
                    Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                horizontalVelocity =
                    Vector3
                        .ClampMagnitude(horizontalVelocity,
                        maxSpeedInAir * speedModifier);
                characterVelocity =
                    horizontalVelocity + (Vector3.up * verticalVelocity);

                // apply the gravity to the velocity
                characterVelocity +=
                    Vector3.down * gravityDownForce * Time.deltaTime;
            }
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove =
            GetCapsuleTopHemisphere(m_Controller.height);
        m_Controller.Move(characterVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        m_LatestImpactSpeed = Vector3.zero;
        if (
            Physics
                .CapsuleCast(capsuleBottomBeforeMove,
                capsuleTopBeforeMove,
                m_Controller.radius,
                characterVelocity.normalized,
                out RaycastHit hit,
                characterVelocity.magnitude * Time.deltaTime,
                -1,
                QueryTriggerInteraction.Ignore)
        )
        {
            // We remember the last impact speed because the fall damage logic might need it
            m_LatestImpactSpeed = characterVelocity;

            characterVelocity =
                Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * m_Controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position +
        (transform.up * (atHeight - m_Controller.radius));
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3
    GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    void UpdateCharacterHeight(bool force)
    {
        // Update height instantly
        if (force)
        {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition =
                Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        } // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight)
        {
            // resize the capsule and adjust camera position
            m_Controller.height =
                Mathf
                    .Lerp(m_Controller.height,
                    m_TargetCharacterHeight,
                    crouchingSharpness * Time.deltaTime);
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition =
                Vector3
                    .Lerp(playerCamera.transform.localPosition,
                    Vector3.up * m_TargetCharacterHeight * cameraHeightRatio,
                    crouchingSharpness * Time.deltaTime);
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
    }

    // returns false if there was an obstruction
    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        // set appropriate heights
        if (crouched)
        {
            m_TargetCharacterHeight = capsuleHeightCrouching;
        }
        else
        {
            // Detect obstructions
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps =
                    Physics
                        .OverlapCapsule(GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(capsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != m_Controller)
                    {
                        return false;
                    }
                }
            }

            m_TargetCharacterHeight = capsuleHeightStanding;
        }

        if (onStanceChanged != null)
        {
            onStanceChanged.Invoke (crouched);
        }

        isCrouching = crouched;
        return true;
    }

    void SetInventoryState(bool opened)
    {
        isInventoryOpen = opened;
        inventory.SetState (opened);
        if (opened)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void Reset()
    {
        isDead = false;
        characterVelocity = Vector3.zero;
        m_Health.Reset();
        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);
    }
}
