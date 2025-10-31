using System.Collections;
using System.Collections.Generic;
using Mirror;
using Org.BouncyCastle.Asn1.Crmf;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnDataChange))] public PlayerData playerData;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] AnimationCurve airSpeedCurve;

    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float maxFallVel = 20f;

    [SerializeField] float jumpHeight = 10f;
    [SerializeField] float jumpBuffer = 0.1f;
    [SerializeField] float edgeLeniency = 0.1f;

    [SerializeField] float drag = 3f;

    [SerializeField] Vector2 diveForce;
    [SerializeField] Vector2 tackledForce;

    [SerializeField] float tackleStun;
    [SerializeField] float hurtStun;

    [SerializeField] Vector2 throwVelocity;


    [Header("Physics Settings")]

    [SerializeField] float gravityScale = 3;

    [SerializeField] float groundRayLength = 0.2f;
    [SerializeField] float diveRayLength = 0.2f;

    [SerializeField] Vector2 diveColliderSize;
    [SerializeField] Vector2 diveColliderOffset;

    [Header("Refrences")]

    [SerializeField] Transform footTransform;
    [SerializeField] Transform character;

    [SerializeField] Transform eggPlaceHolder;
    [SerializeField] Sprite[] eggSprites;

    [SerializeField] SpriteRenderer hatRenderer;

    [SerializeField] BoxCollider2D hitBox;

    [SerializeField] Sprite[] hatSprites;

    [SerializeField] TMP_Text usernameText;

    [SerializeField] GameObject yolkEffect;
    [SerializeField] GameObject fallingParticles;
    Rigidbody2D rb;
    BoxCollider2D playerCollider;
    public Animator animator;

    public SpriteRenderer fakeHat;

    [Header("Layers Masks")]

    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask diveMask;
    [SerializeField] LayerMask interactableMask;

    // In game settings
    float currentSpeed = 0f;
    float currentMaxSpeed;

    [Header("Chicken Abilites")]
    [SerializeField] float glideFallMultiplier = 0.25f;
    bool canDoubleJump = false;

    Vector2 colliderSize;
    Vector2 colliderOffset;

    private float jumpBufferTimer = 0f;
    private float edgeLeniencyTimer = 0f;

    private float stunTimer = 0f;

    [SyncVar(hook = nameof(OnDirectionChange))] int direction = -1;
    int lastDirection = -1;
    int diveDirection = -1;

    float horizontalMove;

    [SerializeField] bool isGrounded = false;
    bool lastGrounded = false;

    bool onSlope = false;
    float slopeAngle = 0f;

    bool isWall = false;

    Vector2 lastVel;

    bool stateChanged = false;

    bool doVelClamp = true; // Weird variable to make sure client doesn't try to clamp vel when server thinks they should be hurt / in tackle state

    [SerializeField] FMODUnity.EventReference JumpSFX;
    [SerializeField] FMODUnity.EventReference DiveSFX;
    [SerializeField] FMODUnity.EventReference HurtSFX;

    bool playerStarted;

    public enum PlayerState
    {
        Idle,
        Move,
        Jump,
        Fall,
        Dive,
        Hurt,
        Tackle,
        Glide
    }

    public enum TrappedState
    {
        None,
        Slowed,
        Immobile
    }

    [SyncVar(hook = nameof(OnUsernameToggled))] public bool usernameEnabled = true;

    [SyncVar] public bool movementEnabled = false;

    [SyncVar(hook = nameof(OnHatChange))] public HatType equippedHat;

    [SyncVar(hook = nameof(UpdateAnimations))][SerializeField] public GameObject heldObject = null;

    [SyncVar(hook = nameof(OnStateChange))] public PlayerState state;
    [SyncVar(hook = nameof(OnTrapUpdate))] public TrappedState trappedState;
    PlayerState lastState;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        animator = character.GetComponentInChildren<Animator>();

        colliderOffset = playerCollider.offset;
        colliderSize = playerCollider.size;

        currentMaxSpeed = maxSpeed;
        currentSpeed = moveSpeed;

        DontDestroyOnLoad(gameObject);

        hatRenderer.color = Constants.playerColors[playerData.playerIndex];

        if (!isLocalPlayer) return;

        InputManager.instance.jump.performed += OnJump;
        InputManager.instance.dive.performed += OnDive;
        InputManager.instance.interact.performed += OnInteract;
    }

    public override void OnStartLocalPlayer()
    {
        if (!playerStarted)
        {
            playerStarted = true;
            LobbyManager.instance.AddPlayer(this, PlayerPrefs.GetString(Constants.usernameKey, "player"));
        }
    }

    private void Update()
    {
        if (heldObject != null && isLocalPlayer)
        {
            if (heldObject.CompareTag("Chicken"))
            {
                GameManager.instance.UpdateScore(playerData.playerIndex, Time.deltaTime);
            }
            else
            {
                /*heldObject.transform.position = transform.position;*/
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer || !movementEnabled || !NetworkClient.ready) return;

        PhysicsChecks();

        HandleJump();

        EvaluateState();

        horizontalMove = InputManager.instance.move.ReadValue<float>();

        Vector2 moveVector = Vector2.zero;
        // direction = horizontalMove != 0 ? (int)Mathf.Sign(horizontalMove) : direction;

        if (horizontalMove != 0 && state != PlayerState.Hurt && state != PlayerState.Tackle)
        {
            UpdateDirection((int)Mathf.Sign(horizontalMove));
        }

        if (isGrounded)
        {
            canDoubleJump = true;
        }

        //FlipCollider(false);

        rb.gravityScale = gravityScale;

        switch (trappedState)
        {
            case TrappedState.None:
                currentMaxSpeed = maxSpeed;
                break;
            case TrappedState.Slowed:
                currentMaxSpeed = maxSpeed / 4;
                break;
            case TrappedState.Immobile:
                rb.velocity = Vector2.zero;
                currentMaxSpeed = 0;
                break;
        }

        switch (state)
        {
            case PlayerState.Idle:
                break;

            case PlayerState.Move:
                if (isGrounded && trappedState != TrappedState.Immobile)
                {
                    Vector2 move = Vector2.right * horizontalMove * currentSpeed;

                    if (onSlope)
                    {
                        move = Quaternion.AngleAxis(slopeAngle, Vector3.forward) * move;
                    }

                    moveVector = move;
                }
                break;

            case PlayerState.Jump:
                if (!isWall || isGrounded)
                {
                    moveVector += HandleAirMove();
                }

                break;

            case PlayerState.Fall:
                if (!isWall || isGrounded)
                {
                    moveVector += HandleAirMove();
                }
                break;

            case PlayerState.Dive:

                Collider2D[] hits = Physics2D.OverlapBoxAll((Vector2)transform.position + (playerCollider.size.x / 2 + diveRayLength / 2) * diveDirection * Vector2.right, new Vector2(diveRayLength, playerCollider.size.y), 0, diveMask);

                Collider2D hit = null;

                foreach (var newHit in hits)
                {
                    if (newHit.gameObject != gameObject)
                    {
                        hit = newHit;
                        break;
                    }
                }
                if (hit != null)
                {
                    if (hit.CompareTag("Chicken"))
                    {
                        //pickup chicken
                    }
                    else if (hit.CompareTag("Player"))
                    {
                        if (hit.GetComponent<PlayerController>().state != PlayerState.Hurt)
                        {
                            hit.GetComponent<PlayerController>().GetTackled(diveDirection);

                            rb.velocity = new Vector2(rb.velocity.x / 2, (rb.velocity.y < 0) ? rb.velocity.y : 0);

                            UpdateState(PlayerState.Tackle);
                        }
                    }
                    else if (hit.CompareTag("Egg"))
                    {
                        //pickup egg
                    }
                    else
                    {
                        UpdateState(PlayerState.Hurt);
                    }
                }
                else if (isGrounded && rb.velocity.y <= 0)
                {
                    UpdateState(PlayerState.Hurt);
                }
                break;

            case PlayerState.Hurt:
                //stunTimer -= Time.deltaTime;
                /*if (stunTimer <= 0)
                {
                    UpdateState(PlayerState.Idle);
                }*/
                
                if (Mathf.Abs(rb.velocity.x) <= 0.1f && !doVelClamp)
                {
                    UpdateState(PlayerState.Idle);
                }

                break;
            case PlayerState.Tackle:

                //stunTimer -= Time.deltaTime;

                /*if (stunTimer <= 0)
                {
                    UpdateState(PlayerState.Idle);
                }*/

                if (Mathf.Abs(rb.velocity.x) <= 0.1f && !doVelClamp)
                {
                    UpdateState(PlayerState.Idle);
                }

                break;
            case PlayerState.Glide:
                if (!isWall || isGrounded)
                {
                    moveVector += Vector2.right * horizontalMove * currentSpeed;
                    rb.gravityScale = gravityScale * glideFallMultiplier;
                }

                break;
        }


        rb.AddForce(moveVector);

        if (state != PlayerState.Dive && state != PlayerState.Hurt && state != PlayerState.Tackle && doVelClamp)
        {
            ApplyCounterForce();
            ClampVelocity();
        }

        lastGrounded = isGrounded;
        lastState = state;

        lastDirection = direction;
    }

    void OnTrapUpdate(TrappedState oldState, TrappedState newState) {
        if (newState == TrappedState.Slowed)
        {
            yolkEffect.SetActive(true);
        } else
        {
            yolkEffect.SetActive(false);
        }
    }

    void PhysicsChecks()
    {
        Collider2D groundHit = Physics2D.OverlapBox((Vector2)footTransform.position - Vector2.up * groundRayLength / 2, new Vector2(playerCollider.bounds.size.x * 2 - 0.05f, groundRayLength), 0, groundMask);

        Collider2D wallHit = Physics2D.OverlapBox((Vector2)transform.position + Vector2.right * direction * (playerCollider.size.x / 2 + groundRayLength / 2 + 0.05f), new Vector2(groundRayLength, playerCollider.bounds.size.y - 0.05f), 0, groundMask);

        RaycastHit2D slopeHit = Physics2D.Raycast((Vector2)footTransform.position, -Vector2.up, groundRayLength * 2, groundMask);

        slopeAngle = Vector2.Angle(Vector2.up, slopeHit.normal);
        //Debug.Log((slopeAngle != 0) + " / " + isGrounded);
        onSlope = slopeAngle != 0f && isGrounded;
        isGrounded = groundHit != null;
        isWall = wallHit != null;
    }

    void EvaluateState()
    {
        PlayerState newState = state;

        if (state == PlayerState.Hurt || state == PlayerState.Tackle)
        {

        }
        else if (state == PlayerState.Dive)
        {
            newState = PlayerState.Dive;
        }
        else if (isGrounded)
        {
            if (horizontalMove != 0)
            {
                newState = PlayerState.Move;
            }
            else
            {
                newState = PlayerState.Idle;
            }
        }
        else if (rb.velocity.y > 0)
        {
            newState = PlayerState.Jump;
        }
        else if (HasChicken() && InputManager.instance.fastFall.ReadValue<float>() == 0)
        {
            newState = PlayerState.Glide;
        }
        else
        {
            newState = PlayerState.Fall;
        }

        stateChanged = state != newState;

        if (stateChanged)
        {
            UpdateState(newState);
        }
    }

    [Command]
    public void UpdateState(PlayerState newState)
    {
        if (state == PlayerState.Dive && newState != PlayerState.Hurt && newState != PlayerState.Tackle) return;

        state = newState;
    }

    [Command(requiresAuthority = false)]
    public void SetMovement(bool canMove)
    {
        Debug.Log(canMove);
        movementEnabled = canMove;
    }


    [Command(requiresAuthority = false)]
    public void GetTackled(int hitDirection)
    {
        state = PlayerState.Hurt;
        HurtLocalPlayer(GetComponent<NetworkIdentity>().connectionToClient, hitDirection * tackledForce);
    }

    [TargetRpc]
    public void HurtLocalPlayer(NetworkConnectionToClient target, Vector2 force)
    {
        doVelClamp = false;
        rb.velocity = force;
        //rb.AddForce(inheritedVel, ForceMode2D.Impulse);
    }

    [ServerCallback]
    public void UpdateTrapState(TrappedState newTrappedState)
    {
        trappedState = newTrappedState;
    }

    [Command]
    void UpdateDirection(int newDir)
    {
        direction = newDir;
    }

    void UpdateAnimations(GameObject lastObj, GameObject newObj)
    {
        if (heldObject != null)  //implement specific holding animations
        {
            if (heldObject.CompareTag("Chicken"))
            {
                animator.SetInteger("HeldObject", 1);
            }
            else if (heldObject.CompareTag("Egg"))
            {
                animator.SetInteger("HeldObject", 2);


                eggPlaceHolder.parent.gameObject.SetActive(true);

                if (heldObject.GetComponent<BaseEgg>().isMystery)
                {
                    eggPlaceHolder.GetComponent<Animator>().SetInteger("Type", 5);
                } else
                {
                    eggPlaceHolder.GetComponent<Animator>().SetInteger("Type", (int)heldObject.GetComponent<BaseEgg>().type);
                }

                Debug.Log((int)heldObject.GetComponent<BaseEgg>().type);
                

            }
        }
        else
        {
            animator.SetInteger("HeldObject", 0);
            eggPlaceHolder.parent.gameObject.SetActive(false);
        }

        animator.SetInteger("State", (int)state);
    }

    [Command]
    void SetJigServer()
    {
        animator.SetInteger("State", 8);
        SetJigRpc();
    }

    [ClientRpc]
    void SetJigRpc()
    {
        animator.SetInteger("State", 8);
    }

    [ClientRpc]
    public void SpawnPlayer(Vector2 spawnPos, bool inGame)
    {
        StartCoroutine(SpawnRoutine(spawnPos, inGame));
    }

    [Command]
    public void SpawnPlayerServer(bool inGame)
    {
        movementEnabled = false;
        SetTriggerRpc("Reset");
        state = PlayerState.Idle;

        usernameEnabled = !inGame;
    }

    private IEnumerator SpawnRoutine(Vector2 spawnPos, bool inGame)
    {
        yield return new WaitUntil(() => NetworkClient.ready);

        SpawnPlayerServer(inGame);

        if (isLocalPlayer)
        {
            transform.position = spawnPos + Vector2.up * (playerCollider.size.y / 2 + 0.1f);
        }
    }

    [Command]
    void HoldObject(GameObject obj)
    {
        //Debug.Log("Current held obj: "+ heldObject.ToString());
        if (heldObject == null)
        {
            Debug.Log("Passed null check");


            if (obj.CompareTag("Chicken"))
            {
                Debug.Log("Chicken Attempted(!");
                if (obj.GetComponent<NewChicken>().GetOwner() == null)
                {
                    obj.GetComponent<NewChicken>().PickUp(this);
                    //obj.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
                    heldObject = obj;
                }
                else
                {
                    Debug.Log("Failed!");
                }

            }
            else if (obj.CompareTag("Egg"))
            {
                Debug.Log("Was an egg");
                obj.GetComponent<BaseEgg>().PickUp(this);
                //obj.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);

                /*eggPlaceHolder.parent.gameObject.SetActive(true);
                eggPlaceHolder.GetComponent<SpriteRenderer>().sprite = obj.GetComponent<SpriteRenderer>().sprite;*/

                heldObject = obj;
            }
        }
    }

    void OnStun(float oldTime, float newTime)
    {
        stunTimer = newTime;
    }
    IEnumerator FallParticles()
    {
        fallingParticles.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        fallingParticles.SetActive(false);
    }
    void OnStateChange(PlayerState oldState, PlayerState newState)
    {
        UpdateAnimations(null, null);
        if (oldState == PlayerState.Fall && newState != oldState)
        {
            if (!fallingParticles.activeInHierarchy)
            {
                StartCoroutine(FallParticles());
            }
            
        }
        switch (newState)
        {
            case PlayerState.Dive:
                FlipCollider(true);
                FMODUnity.RuntimeManager.PlayOneShot(DiveSFX, transform.position);
                break;
                
            case PlayerState.Jump:
                FMODUnity.RuntimeManager.PlayOneShot(JumpSFX, transform.position);
                break;
        }

        if (!isLocalPlayer) return;

        if (newState == PlayerState.Hurt)
        {
            if (HasChicken())
            {
                DropChicken();
            }

            StartCoroutine(HurtRoutine());
        }

        if (newState == PlayerState.Tackle) 
        {
            StartCoroutine(TackleRoutine());
        }

        if (newState == PlayerState.Move)
        {
            doVelClamp = true;
        }

        if (oldState == PlayerState.Tackle || oldState == PlayerState.Hurt)
        {
            FlipCollider(false);
            doVelClamp = true;
            UpdateDirection(diveDirection);
        }
    }

    IEnumerator HurtRoutine()
    {
        yield return new WaitForSeconds(4);

        if (state == PlayerState.Hurt)
        {
            UpdateState(PlayerState.Idle);
        }
    }

    IEnumerator TackleRoutine()
    {
        yield return new WaitForSeconds(4);

        if (state == PlayerState.Tackle)
        {
            UpdateState(PlayerState.Idle);
        }
    }

    void OnDirectionChange(int lastDir, int newDir)
    {
        if (state != PlayerState.Dive && state != PlayerState.Hurt && state != PlayerState.Tackle)
        {
            character.localScale = new Vector2(Mathf.Abs(character.localScale.x) * -direction, character.localScale.y);
            playerCollider.offset = new Vector2(colliderOffset.x * -direction, colliderOffset.y);
        }

        if (heldObject != null)
        {
            if (heldObject.TryGetComponent<NewChicken>(out NewChicken chicken))
            {
                chicken.SetDirection(newDir);
            }
        }
    }

    void ApplyCounterForce()
    {
        if (horizontalMove == 0)
        {
            Vector2 counterForce = Vector2.right * rb.velocity.x * -1 * drag;

            if (onSlope)
            {
                counterForce = Quaternion.AngleAxis(slopeAngle, Vector3.forward) * counterForce * 2f;
            }

            rb.AddForce(counterForce);
        }
    }

    void ClampVelocity()
    {
        //Consider clamping magnitude instead

        if (Mathf.Abs(rb.velocity.x) > currentMaxSpeed && state != PlayerState.Dive)
        {
            rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * currentMaxSpeed, rb.velocity.y);
        }

        if (rb.velocity.y < maxFallVel)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxFallVel);
        }
    }

    Vector2 HandleAirMove()
    {
        return Vector2.right * horizontalMove * currentSpeed * airSpeedCurve.Evaluate(Mathf.Clamp01(rb.velocity.y / maxFallVel));
    }

    void HandleJump()
    {
        jumpBufferTimer -= Time.deltaTime;
        edgeLeniencyTimer -= Time.deltaTime;

        if (state == PlayerState.Jump) return;

        if (lastGrounded && !isGrounded)
        {

            edgeLeniencyTimer = edgeLeniency;
        }

        if (isGrounded && jumpBufferTimer > 0)
        {

            Jump();
        }
    }

    void Jump()
    {

        jumpBufferTimer = 0;
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * Mathf.Sqrt(-2.0f * Physics2D.gravity.y * gravityScale * jumpHeight), ForceMode2D.Impulse);
    }

    void OnJump(InputAction.CallbackContext context)
    {

        if ((state == PlayerState.Jump && !HasChicken()) || state == PlayerState.Hurt || state == PlayerState.Tackle || state == PlayerState.Dive || trappedState != TrappedState.None || !movementEnabled) return;

        if (canDoubleJump && HasChicken() && !isGrounded)
        {
            Jump();
            canDoubleJump = false;
        }
        else if (edgeLeniencyTimer > 0 && !isGrounded)
        {
            edgeLeniencyTimer = 0;
            Jump();
        }
        else
        {
            jumpBufferTimer = jumpBuffer;
        }
    }

    void OnDive(InputAction.CallbackContext context)
    {
        if (state != PlayerState.Dive && state != PlayerState.Hurt && state != PlayerState.Tackle && horizontalMove != 0 && movementEnabled)
        {
            if (HasChicken()) return;

            diveDirection = direction;
            UpdateState(PlayerState.Dive);

            FlipCollider(true);
            doVelClamp = false;
            rb.AddForce(new Vector2(diveForce.x * (float)diveDirection, diveForce.y), ForceMode2D.Impulse);

        }
    }
    [Command]
    void GetRidOfEgg()
    {
        heldObject = null;
    }
    void OnInteract(InputAction.CallbackContext context)
    {
        if (!movementEnabled) return;

        if (heldObject != null)
        {
            if (heldObject.CompareTag("Chicken"))
            {
                SetJigServer();
            }
            else if (heldObject.CompareTag("Egg"))
            {
                SetTrigger("Throw");
            }
        }
        else
        {
            SetTrigger("PickUp");
            ProcessInteract();
        }
    }

    void OnHatChange(HatType oldHat, HatType newHat)
    {
        hatRenderer.sprite = hatSprites[(int)newHat];
    }

    void OnDataChange(PlayerData oldData, PlayerData newData)
    {
        hatRenderer.color = Constants.playerColors[newData.playerIndex];
        fakeHat.color = Constants.playerColors[newData.playerIndex];

        usernameText.text = newData.userName;
    }

    [Command]
    public void SetUsernameEnabled(bool enabled) 
    {
        usernameEnabled = enabled;
    }

    void OnUsernameToggled(bool oldEnabled, bool newEnabled)
    {
        usernameText.transform.parent.gameObject.SetActive(newEnabled);
    }


    [Command]
    void SetTrigger(string triggerName)
    {
        SetTriggerRpc(triggerName);
    }

    [ClientRpc]
    public void SetTriggerRpc(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    public void ProcessInteract()
    {
        if (!isLocalPlayer) return;

        if (heldObject != null)
        {
            if (heldObject.CompareTag("Chicken"))
            {

            }
            else if (heldObject.CompareTag("Egg"))
            {
                heldObject.GetComponent<BaseEgg>().Throw(new Vector2(throwVelocity.x * direction, throwVelocity.y));
                GetRidOfEgg();
            }
        }
        else
        {

            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, hitBox.size, 0, interactableMask);

            if (hits.Length > 0)
            {
                GameObject obj = null;

                foreach (Collider2D hit in hits)
                {

                    if (hit.CompareTag("Chicken"))
                    {
                        if (hit.gameObject.GetComponent<NewChicken>().GetOwner() == null)
                        {
                            obj = hit.gameObject;
                        }

                        break;
                    }
                    else if (hit.gameObject.CompareTag("Egg"))
                    {
                        if (hit.gameObject.GetComponent<BaseEgg>().owner == null)
                        {
                            obj = hit.gameObject;
                        }

                        break;
                    }
                    else if (hit.TryGetComponent<HatRack>(out HatRack rack))
                    {
                        rack.ExchangeHat(this);
                    }
                    //Debug.Log("Changed to: " + obj);
                }
                // Debug.Log("Object is: " + obj);
                if (obj != null)
                {
                    HoldObject(obj);
                }
            }

        }
    }

    [Command]
    void FlipCollider(bool horizontal)
    {
        FlipColliderClientRPC(horizontal);
    }

    [ClientRpc]
    void FlipColliderClientRPC(bool horizontal)
    {
        if (playerCollider == null) return;


        if (horizontal)
        {
            playerCollider.offset = diveColliderOffset;
            playerCollider.size = diveColliderSize;
        }
        else
        {
            playerCollider.offset = new Vector2(colliderOffset.x * -direction, colliderOffset.y);
            playerCollider.size = colliderSize;
        }

        footTransform.localPosition =  Vector2.up * (playerCollider.offset.y - playerCollider.size.y / 2);
    }

    [Command]
    void DropChicken()
    {
        heldObject.GetComponent<NewChicken>().Drop();
        heldObject = null;

    }

    public bool HasChicken()
    {
        return heldObject != null && heldObject.CompareTag("Chicken");
    }

    public void ScheduleTrape(float trappedTime, TrappedState newTrapState)
    {
        StartCoroutine(TrapRoutine(trappedTime, newTrapState));
    }

    private IEnumerator TrapRoutine(float trappedTime, TrappedState newTrapState)
    {
        UpdateTrapState(newTrapState);

        yield return new WaitForSeconds(trappedTime);

        if (trappedState == newTrapState) 
        {
            UpdateTrapState(TrappedState.None);
        }
    }

    public void OnEatFinished()
    {
        GameManager.instance.EnableWinScreen();
    }

    public void OnHatBestowFinished()
    {
        GameManager.instance.OnWinnerAnimOver();
    }

    public void OnHitBoxEnter(Collider2D other)
    {

        /*if (other.CompareTag("Player"))
        {
            //Debug.Log(other.GetComponent<PlayerController>().state);
            if (other.GetComponent<PlayerController>().state == PlayerState.Dive || other.GetComponent<PlayerController>().state == PlayerState.Tackle)
            {
                Vector2 incomingVel = other.GetComponent<Rigidbody2D>().velocity;



                UpdateState(PlayerState.Hurt);

                if (HasChicken()) {
                    DropChicken();
                
                }
            }
        }*/
    }
}