using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] protected float m_walkSpeed;
    [SerializeField] protected float m_runSpeed;
    [SerializeField] protected float m_airborneAccelMult;
    [SerializeField] protected float m_gravityMultiplier;
    [SerializeField] protected float m_jumpHeight;
    [SerializeField] protected float m_rotateTime;

    private bool m_isRunning;
    private bool m_isGrounded;
    private bool m_isJumping;
    private bool m_isActive;
    private bool m_isRotating;
    private bool m_inPortal;

    private float m_forwardVel;
    private float m_rotLerp;
    private float m_targetYAngle;
    private float m_currentYAngle;

    private Vector2 m_moveDir;

    private CollisionFlags m_collisionFlags;
    private CharacterController m_controller;
    private PlayerManager m_playerManager;
    private AudioManager m_audioManager;
    private LayerMask m_levelLayer;

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        m_controller = GetComponent<CharacterController>();
        m_playerManager = FindObjectOfType<PlayerManager>();
        m_audioManager = FindObjectOfType<AudioManager>();
    }

    void Update()
    {
        DoMovement();
    }

    private void DoMovement()
    {
        bool tryJump = false;
        if (m_isActive)
        {
            //Get user inputs
            m_isRunning = Input.GetButton("Run");
            m_moveDir.x = Input.GetAxis("Horizontal") * (!m_isRunning ? m_walkSpeed : m_runSpeed);
            tryJump = Input.GetButtonDown("Jump");
        }
        else
        {
            //Stop inactive player movement when grounded
            if (m_isGrounded)
            {
                m_isRunning = false;
                m_moveDir.x = 0f;
            }
        }

        //Handle gravity and jumping
        m_isGrounded = CheckGround();
        if (m_isGrounded)
        {
            //Preserve forward velocity for when the player is not grounded
            m_forwardVel = m_moveDir.x;

            if (tryJump && !m_isJumping) //Do Jump
            {
                m_isJumping = true;
                m_moveDir.y = Mathf.Sqrt(-2f * m_jumpHeight * Physics.gravity.y * m_gravityMultiplier);
            }
            else
            {
                if (m_moveDir.y <= 0f) //Clamp player to ground
                {
                    m_isJumping = false;
                    m_moveDir.y = Physics.gravity.y * m_gravityMultiplier / 2f;
                }
            }
        }
        else
        {
            //Allow player to change direction in the air
            m_moveDir.x *= Time.deltaTime * m_airborneAccelMult;
            m_forwardVel += m_moveDir.x; //Preserve forward veloctiy after any acceleration
            m_forwardVel = Mathf.Clamp(m_forwardVel, -m_walkSpeed, m_walkSpeed);
            m_moveDir.x = m_forwardVel;

            //Apply gravity when not grounded
            m_moveDir.y += Physics.gravity.y * m_gravityMultiplier * Time.deltaTime;
        }

        //Animate player to rotate in the direction of movement
        if (!Mathf.Approximately(m_moveDir.x, 0f))
        {
            if (!Mathf.Approximately(m_targetYAngle, (m_moveDir.x > 0f) ? 0f : 180f))
            {
                if (m_isRotating)
                {
                    m_rotLerp = 1 - m_rotLerp; //Reverse lerp if direction changes before finishing
                }

                m_currentYAngle = (m_moveDir.x > 0) ? 180f : 0f;
                m_targetYAngle = (m_moveDir.x > 0) ? 0f : 180f;
            }

        }

        //Lean character forward based on speed
        float xAngle = Mathf.Lerp(0f, 25f, Mathf.Abs(m_moveDir.x / m_walkSpeed));

        float yAngle;
        if(!Mathf.Approximately(m_targetYAngle, transform.localEulerAngles.y))
        {
            m_isRotating = true;
            m_rotLerp += Time.deltaTime / m_rotateTime;
            yAngle = Mathf.Lerp(m_currentYAngle, m_targetYAngle, m_rotLerp);
        }
        else
        {
            m_isRotating = false;
            m_rotLerp = 0f;
            yAngle = m_targetYAngle;
        }
        transform.localEulerAngles = new Vector3(xAngle, yAngle, 0f);

        //Send movement vector to the character controller
        Vector3 movementVector = Vector3.zero + Vector3.up * m_moveDir.y + Vector3.forward * m_moveDir.x;
        movementVector += Vector3.right * -transform.localPosition.x * 5f; //Clamp player to 0 local x coordinate
        m_collisionFlags = m_controller.Move(movementVector * Time.deltaTime);
    }

    private bool CheckGround()
    {
        //Create physics box skinWidth/2 units under player with a depth of checkDepth to check for grounding
        float boxSize = m_controller.radius * 0.75f;
        float checkDepth = 0.2f;
        float boxOffset = m_controller.skinWidth / 2f;
        Vector3 boxCenter = m_controller.transform.position - Vector3.up * ((((m_controller.height > 2f * m_controller.radius) ? m_controller.height : 2f * m_controller.radius) * transform.localScale.y + checkDepth) / 2f + boxOffset);
        Vector3 boxDimensions = Vector3.right * boxSize + Vector3.forward * boxSize + Vector3.up * checkDepth;
        return Physics.CheckBox(boxCenter, boxDimensions, transform.rotation, m_levelLayer);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Ignore physics for objects stood on
        if (m_collisionFlags == CollisionFlags.Below)
        {
            return;
        }
        else if (m_collisionFlags == CollisionFlags.Above)
        {
            //Start falling if a collision above player occurs while jumping
            if (m_moveDir.y > 0)
            {
                m_moveDir.y = 0f;
            }
        }
        else
        {
            //Stop player's forward velocity if something is hit while jumping
            m_forwardVel = 0f;
        }

        //Apply physics betweeen player and other rigidbodies
        Rigidbody body = hit.collider.attachedRigidbody;

        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(m_controller.velocity * 0.1f, hit.point, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Portal"))
        {
            m_inPortal = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Portal"))
        {
            m_inPortal = false;
        }
    }

    public void SetActive(bool state)
    {
        m_isActive = state;
    }

    public void SetLayers(string level, string playerLevel, string otherPlayer)
    {
        //Layers to collide and check grounding with
        m_levelLayer = LayerMask.GetMask(level, playerLevel, otherPlayer);
    }

    public void IgnoreLayers(string player, string otherPlayerLevel, string otherPlayerTriggers)
    {
        //Ignore collisions between these layers
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer(player), LayerMask.NameToLayer(otherPlayerLevel));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer(player), LayerMask.NameToLayer(otherPlayerTriggers));
    }

    public bool AtPortal()
    {
        return m_inPortal;
    }

}
