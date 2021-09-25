using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace Invector
{
    public abstract class vThirdPersonMotor : vCharacter
    {
        #region Character Variables

        public enum LocomotionType
        {
            FreeWithStrafe,
            OnlyStrafe,
            OnlyFree
        }

        [Header("--- Locomotion Setup ---")]

        public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;
        [Tooltip("Add extra speed for the locomotion movement, keep this value at 0 if you want to use only root motion speed.")]
        [SerializeField] protected float extraMoveSpeed = 0f;
        [Tooltip("Add extra speed for the strafe movement, keep this value at 0 if you want to use only root motion speed.")]
        [SerializeField] protected float extraStrafeSpeed = 0f;
        [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
        [SerializeField] protected bool rotateByWorld = false;
        [Tooltip("Check if you want to use Turn on Spot animations")]
        [SerializeField] protected bool turnOnSpot = true;
        [Tooltip("Check if you want to use QuickStop animation")]
        [SerializeField] protected bool quickStopAnim = true;
        [Tooltip("Speed of the rotation on free directional movement")]
        [SerializeField] protected float rotationSpeed = 10f;
        [Tooltip("Put your Random Idle animations at the AnimatorController and select a value to randomize, 0 is disable.")]
        [SerializeField] protected float randomIdleTime = 0f;

        [Header("--- Grounded Setup ---")]

        protected float groundDistance;
        public RaycastHit groundHit;
        [Tooltip("ADJUST IN PLAY MODE - Offset height limit for sters - GREY Raycast in front of the legs")]
        public float stepOffsetEnd = 0.45f;
        [Tooltip("ADJUST IN PLAY MODE - Offset height origin for sters, make sure to keep slight above the floor - GREY Raycast in front of the legs")]
        public float stepOffsetStart = 0.05f;
        [Tooltip("Higher value will result jittering on ramps, lower values will have difficulty on steps")]
        public float stepSmooth = 2f;
        [Tooltip("Max angle to walk")]
        [SerializeField] protected float slopeLimit = 45f;
        [Tooltip("Apply extra gravity when the character is not grounded")]
        [SerializeField] protected float extraGravity = 4f;
        [Tooltip("Select a VerticalVelocity to trigger the Land High animation")]
        [SerializeField] protected float landHighVel = -5f;
        [Tooltip("Turn the Ragdoll On when falling at high speed (check VerticalVelocity) - leave the value with 0 if you don't want this feature")]
        [SerializeField] protected float ragdollVel = -8f;

        [Header("--- Head Track ---")]

        [Tooltip("The character Head will follow where you look at, leave with 0 if you are using TopDown or 2.5D or don't want to use")]
        [SerializeField] protected float headWeight = 1f;
        [Tooltip("Determines how much the body is involved in the LookAt when in Free Movement")]
        [SerializeField] protected float freeBodyWeight = 0.25f;
        [Tooltip("Determines how much the body is involved in the LookAt when Strafing")]
        [SerializeField] protected float strafeBodyWeight = 0.8f;
        [Tooltip("Max angle to affect the HeadTrack and Body")]
        [SerializeField] protected float maxAngle = 55f;
        [Tooltip("Speed to blend the HeadTrack and Body")]
        [SerializeField] protected float headTrackMultiplier = 1f;

        [Header("--- Debug Info ---")]
        [SerializeField] protected bool debugWindow;
        [Range(0f, 1f)]
        public float timeScale = 1f;
        #endregion

        #region Protected Variables      
        protected CombatID combatID;
        protected GameObject currentCollectable;
        protected v_AIController targetEnemy;
        #endregion

        #region Actions

        // general bools of movement        
        protected bool
            onGround,   stopMove,       autoCrouch,
            quickStop,  quickTurn,      canSprint,
            crouch,     strafing,       landHigh,
            jump,       isJumping,      sliding,
            lockIntoTarget;

        // actions bools, used to turn on/off actions animations *check ThirdPersonAnimator*	        
        protected bool
            jumpOver,
            stepUp,
            climbUp,
            roll,
            enterLadderBottom,
            enterLadderTop,
            usingLadder,
            exitLadderBottom,
            exitLadderTop,
            inAttack,
            blocking,
            hitReaction,
            hitRecoil;

        // one bool to rule then all
        protected bool actions
        {
            get
            {
                return jumpOver || stepUp || climbUp || roll || usingLadder || quickStop || quickTurn || hitReaction || hitRecoil;
            }
        }

        /// <summary>
        ///  DISABLE ACTIONS - call this method in canse you need to turn every action bool false
        /// </summary>
        protected void DisableActions()
        {
            inAttack = false;
            blocking = false;
            quickTurn = false;
            hitReaction = false;
            hitRecoil = false;
            quickStop = false;
            canSprint = false;
            strafing = false;
            landHigh = false;
            jumpOver = false;
            roll = false;
            stepUp = false;
            climbUp = false;
            crouch = false;
            jump = false;
            isJumping = false;
        }

        protected void RemoveComponents()
        {
            if (_capsuleCollider != null) Destroy(_capsuleCollider);
            if (_rigidbody != null) Destroy(_rigidbody);
            if (animator != null) Destroy(animator);            
            var comps = GetComponents<MonoBehaviour>();
            foreach (Component comp in comps) Destroy(comp);
        }

        #endregion

        #region Camera Variables
        // acess camera info
        [HideInInspector]
        public v3rdPersonCamera tpCamera;
        // generic string to change the CameraState
        [HideInInspector]
        public string customCameraState;
        // generic string to change the CameraPoint of the Fixed Point Mode
        [HideInInspector]
        public string customlookAtPoint;
        // generic bool to change the CameraState
        [HideInInspector]
        public bool changeCameraState;
        // generic bool to know if the state will change with or without lerp
        [HideInInspector]
        public bool smoothCameraState;
        // generic variables to find the correct direction 
        [HideInInspector]
        public Quaternion freeRotation;
        // create a offSet base on the character hips 
        [HideInInspector]
        public float offSetPivot;
        // keep the current direction in case you change the cameraState
        [HideInInspector]
        public bool keepDirection;
        [HideInInspector]
        public Vector2 oldInput;
        [HideInInspector]
        public Vector3 cameraForward;
        [HideInInspector]
        public Vector3 cameraRight;
        // get the tpCamera transform
        public Transform cameraTransform
        {
            get
            {
                Transform cT = transform;
                if (Camera.main != null)
                    cT = Camera.main.transform;
                if (tpCamera)
                    cT = tpCamera.transform;
                if (cT == transform)
                {
                    Debug.LogWarning("Invector : Missing TPCamera or MainCamera");
                    this.enabled = false;
                }
                return cT;
            }
        }
        #endregion

        #region HUD variables
        // acess hud controller 
        [HideInInspector]
        public vHUDController hud;
        #endregion

        #region Components & Hide Variables
        // acess action controller information
        [HideInInspector] public vActionsController actionsController;
        // acess melee weapon information
        [HideInInspector] public vMeleeManager meleeManager;
        // physics material
        [HideInInspector] public PhysicMaterial frictionPhysics, slippyPhysics;
        // get capsule collider information
        [HideInInspector] public CapsuleCollider _capsuleCollider;
        // storage capsule collider extra information
        [HideInInspector] public float colliderRadius, colliderHeight;
        // storage the center of the capsule collider info
        [HideInInspector] public Vector3 colliderCenter;
        // access the rigidbody component
        [HideInInspector] public Rigidbody _rigidbody;
        // generate input for the controller
        [HideInInspector] public Vector2 input;
        // lock all the character locomotion 
        [HideInInspector] public bool lockPlayer;
        // general variables to the locomotion
        [HideInInspector] public float speed, direction, verticalVelocity;
        #endregion

        /// <summary>
        /// INITIAL SETUP - prepare the initial information like camera, capsule collider, physics materials, etc...
        /// </summary>
        public void InitialSetup()
        {
            animator = GetComponent<Animator>();
            tpCamera = v3rdPersonCamera.instance;
            hud = vHUDController.instance;
            meleeManager = GetComponent<vMeleeManager>();
            // create a offset pivot to the character, to align camera position when transition to ragdoll
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            offSetPivot = Vector3.Distance(transform.position, hips.position);

            if (tpCamera != null)
            {
                tpCamera.offSetPlayerPivot = offSetPivot;
                tpCamera.target = transform;
            }

            if (hud == null) Debug.LogWarning("Invector : Missing HUDController, please assign on ThirdPersonController");

            // prevents the collider from slipping on ramps
            frictionPhysics = new PhysicMaterial();
            frictionPhysics.name = "frictionPhysics";
            frictionPhysics.staticFriction = 1f;
            frictionPhysics.dynamicFriction = 1f;

            // default physics 
            slippyPhysics = new PhysicMaterial();
            slippyPhysics.name = "slippyPhysics";
            slippyPhysics.staticFriction = 0f;
            slippyPhysics.dynamicFriction = 0f;

            // rigidbody info
            _rigidbody = GetComponent<Rigidbody>();

            // capsule collider 
            _capsuleCollider = GetComponent<CapsuleCollider>();

            // save your collider preferences 
            colliderCenter = GetComponent<CapsuleCollider>().center;
            colliderRadius = GetComponent<CapsuleCollider>().radius;
            colliderHeight = GetComponent<CapsuleCollider>().height;

            // health info
            currentHealth = maxHealth;
            currentHealthRecoveryDelay = healthRecoveryDelay;
            currentStamina = maxStamina;

            if (hud == null)
                return;

            hud.damageImage.color = new Color(0f, 0f, 0f, 0f);

            cameraTransform.SendMessage("Init", SendMessageOptions.DontRequireReceiver);
            UpdateHUD();
        }

        /// <summary>
        /// UPDATE MOTOR - call this method on ThirdPersonController FixedUpdate
        /// </summary>
        protected void UpdateMotor()
        {
            CheckHealth();
            CheckGround();
            CheckRagdoll();
            ControlHeight();
            ControlLocomotion();
            StaminaRecovery();
            HealthRecovery();
        }

        /// <summary>
        /// UPDATE HUD - sycronize the stamina value with the stamina slider and show/hide the damageHUD image effect
        /// </summary>
        public void UpdateHUD()
        {
            if (hud == null)
                return;

            hud.healthSlider.maxValue = maxHealth;
            hud.healthSlider.value = currentHealth;
            hud.staminaSlider.maxValue = maxStamina;
            hud.staminaSlider.value = currentStamina;

            if (hud.damaged)
                hud.damageImage.color = hud.flashColour;
            else
                hud.damageImage.color = Color.Lerp(hud.damageImage.color, Color.clear, hud.flashSpeed * Time.deltaTime);

            hud.damaged = false;
        }

        /// <summary>
        /// CHECK HEALTH - if you have a gameController in the scene, your character will respawn in 4f, otherwise the scene will reload                     
        /// </summary>
        void CheckHealth()
        {
            if (currentHealth <= 0 && !isDead)
            {
                transform.root.SendMessage("DropRightWeapon", SendMessageOptions.DontRequireReceiver);
                transform.root.SendMessage("DropLeftWeapon", SendMessageOptions.DontRequireReceiver);

                tpCamera.ClearTargetLockOn();
                isDead = true;

                if (hud != null)
                    hud.FadeText("You are Dead!", 2f, 0.5f);

                if (vGameController.instance != null && vGameController.instance.playerPrefab != null)
                    vGameController.instance.Invoke("Spawn", vGameController.instance.respawnTimer);
                else if (vGameController.instance != null)
                {
                    //if (vSpawnEnemies.instance != null)
                    //    vSpawnEnemies.instance.Invoke("ShowLoseWindow", vGameController.instance.respawnTimer);
                    //else
                    vGameController.instance.Invoke("ResetScene", vGameController.instance.respawnTimer);
                }
            }
        }

        /// <summary>
        /// Show a custom message if you have a HUD FadeText 
        /// </summary>
        /// <param name="message"></param>
        public void ShowText(string message)
        {
            hud.FadeText(message, 2f, 0.5f);
        }

        /// <summary>
        /// Call this method when executing a action to reduce the stamina 
        /// </summary>
        /// <param name="value"></param>
        protected void ReduceStamina(float value)
        {
            currentStamina -= value;
            if (currentStamina < 0)
                currentStamina = 0;
        }

        /// <summary>
        /// After calling the ReduceStamina, you need to pass a value to the recoveryDelay variable 
        /// example > recoveryDelay = actionsController.jump.recoveryDelay;
        /// </summary>
        void StaminaRecovery()
        {
            if (recoveryDelay > 0)
            {
                recoveryDelay -= Time.deltaTime;
            }
            else
            {
                if (currentStamina > maxStamina)
                    currentStamina = maxStamina;
                if (currentStamina < maxStamina)
                    currentStamina += staminaRecovery;
            }
        }

        /// <summary>        
        /// Slowly recovery Health
        /// </summary>
        void HealthRecovery()
        {
            if (currentHealth <= 0) return;
            if (currentHealthRecoveryDelay > 0)
            {
                currentHealthRecoveryDelay -= Time.deltaTime;
            }
            else
            {
                if (currentHealth > maxHealth)
                    currentHealth = maxHealth;
                if (currentHealth < maxHealth)
                    currentHealth = Mathf.Lerp(currentHealth, maxHealth, healthRecovery * Time.deltaTime);
            }
        }

        /// <summary>
        /// Find the combat ID
        /// </summary>
        /// <param name="id"></param>
        public void SetCombatID(CombatID id)
        {
            combatID = id;
        }

        /// <summary>
        /// knows when you have enter the Attack state and apply the ReduceStamina
        /// </summary>
        public void OnAttackEnter(HitboxFrom hitboxFrom)
        {
            ReduceStamina(meleeManager.CurrentMeleeAttack(hitboxFrom).staminaCost);
            recoveryDelay = meleeManager.CurrentMeleeAttack(hitboxFrom).a_staminaRecoveryDelay;
        }

        /// <summary>
        /// knows if you are attacking
        /// </summary>
        public void InAttacking()
        {
            inAttack = true;
        }

        /// <summary>
        /// knows when you have exit the Attack state
        /// </summary>
        public void OnAttackExit()
        {
            inAttack = false;
        }       

        public void FindTargetLockOn(Transform target)
        {
            var _targetEnemy = target.GetComponent<v_AIController>();
            if (_targetEnemy != null)
                targetEnemy = _targetEnemy;
        }

        public void LostTargetLockOn()
        {
            targetEnemy = null;
            strafing = false;
        }

        /// <summary>
        /// CONTROL LOCOMOTION BEHAVIOUR - change between strafe and free directional movement
        /// </summary>
        void ControlLocomotion()
        {
            if (lockPlayer) return;
            // free directional movement
            if (freeLocomotionConditions)
            {
                // set speed to both vertical and horizontal inputs
                speed = Mathf.Abs(input.x) + Mathf.Abs(input.y);
                speed = Mathf.Clamp(speed, 0, 1);
                // add 0.5f on sprint to change the animation on animator
                if (canSprint) speed += 0.5f;
                if (stopMove) speed = 0f;
                if (input == Vector2.zero && !quickTurn) direction = Mathf.Lerp(direction, 0f, 20f * Time.fixedDeltaTime);

                if ((!actions || quickTurn || quickStop) && !inAttack) FreeRotationMovement();
            }
            // move forward, backwards, strafe left and right
            else
            {
                speed = input.y;
                direction = input.x;
                if (canSprint) speed += 0.5f;
            }
        }

        /// <summary>
        /// Conditions for Free Directional Movement
        /// </summary>
        protected bool freeLocomotionConditions
        {
            get
            {
                if (locomotionType.Equals(LocomotionType.OnlyStrafe)) strafing = true;
                return !strafing && !usingLadder && !landHigh && !locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.OnlyFree);
            }
        }

        /// <summary>
        /// FREE ROTATION - handle the character rotation when on a free directional movement, also activate the turn180 animations.
        /// </summary>
        void FreeRotationMovement()
        {
            if (input != Vector2.zero && !quickTurn && !lockPlayer && targetDirection.magnitude > 0.1f)
            {
                if (inAttack) return;

                freeRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                Vector3 velocity = Quaternion.Inverse(transform.rotation) * targetDirection.normalized;
                direction = Mathf.Atan2(velocity.x, velocity.z) * 180.0f / 3.14159f;

                // activate quickTurn180 based on the directional angle
                var quickTurnConditions = !crouch && direction >= 90 && !jump && onGround || !crouch && direction <= -90 && !jump && onGround;
                if (quickTurnConditions && turnOnSpot)
                    quickTurn = true;

                // apply free directional rotation while not turning180 animations
                if ((!quickTurn && !isJumping) || (isJumping && actionsController.Jump.jumpAirControl))
                {
                    Vector3 lookDirection = targetDirection.normalized;
                    freeRotation = Quaternion.LookRotation(lookDirection, transform.up);
                    var euler = new Vector3(0, freeRotation.eulerAngles.y, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), rotationSpeed * Time.fixedDeltaTime);
                }
                if (!keepDirection)
                    oldInput = input;
                if (Vector2.Distance(oldInput, input) > 0.9f && keepDirection)
                    keepDirection = false;
            }
        }

        /// <summary>
        /// ACTIVATE RAGDOLL - check your verticalVelocity and assign a value on the variable RagdollVel
        /// </summary>
        void CheckRagdoll()
        {
            if (ragdollVel == 0) return;

            if (verticalVelocity <= ragdollVel && groundDistance <= 0.1f)
                transform.SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// CAPSULE COLLIDER HEIGHT CONTROL - controls height, position and radius of CapsuleCollider
        /// </summary>
        void ControlHeight()
        {
            if (crouch || roll)
            {
                _capsuleCollider.center = colliderCenter / 1.4f;
                _capsuleCollider.height = colliderHeight / 1.4f;
            }
            else if (usingLadder)
            {
                _capsuleCollider.radius = colliderRadius / 1.25f;
            }
            else
            {
                // back to the original values
                _capsuleCollider.center = colliderCenter;
                _capsuleCollider.radius = colliderRadius;
                _capsuleCollider.height = colliderHeight;
            }
        }

        /// <summary>
        /// GROUND CHECKER - check if the character is grounded or not	
        /// </summary>	
        void CheckGround()
        {
            CheckGroundDistance();

            // change the physics material to very slip when not grounded
            _capsuleCollider.material = (onGround && GroundAngle() < slopeLimit) ? frictionPhysics : slippyPhysics;
            // we don't want to stick the character grounded if one of these bools is true
            bool groundStickConditions = !jumpOver && !stepUp && !climbUp && !usingLadder && !hitReaction;

            if (groundStickConditions)
            {
                // clear the checkground to free the character to attack on air
                if (inAttack) return;
                var onStep = StepOffset();

                if (groundDistance <= 0.05f)
                {
                    onGround = true;
                    Sliding();
                }
                else
                {
                    if (groundDistance >= groundCheckDistance)
                    {
                        onGround = false;
                        // check vertical velocity
                        verticalVelocity = _rigidbody.velocity.y;
                        // apply extra gravity when falling
                        if (!onStep && !roll)
                            transform.position -= Vector3.up * (extraGravity * Time.deltaTime);
                    }
                    else if (!onStep && !roll && !jump)
                        transform.position -= Vector3.up * (extraGravity * Time.deltaTime);
                }
            }
        }

        void Sliding()
        {
            var onStep = StepOffset();
            var groundAngleTwo = 0f;
            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position, -transform.up);

            if (Physics.Raycast(ray, out hitinfo, 1f, groundLayer))
                groundAngleTwo = Vector3.Angle(Vector3.up, hitinfo.normal);

            if (GroundAngle() > slopeLimit + 1f && GroundAngle() <= 85 &&
                groundAngleTwo > slopeLimit + 1f && groundAngleTwo <= 85 &&
                groundDistance <= 0.05f && !onStep)
            {
                sliding = true;
                onGround = false;
                var slideVelocity = (GroundAngle() - slopeLimit) * 5f;
                slideVelocity = Mathf.Clamp(slideVelocity, 0, 10);
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, -slideVelocity, _rigidbody.velocity.z);
            }
            else
            {
                sliding = false;
                onGround = true;
            }
        }

        /// <summary>
        /// GROUND DISTANCE	- get the distance between the middle of the character to the ground
        /// </summary>
        void CheckGroundDistance()
        {
            if (_capsuleCollider != null)
            {
                // radius of the SphereCast
                float radius = _capsuleCollider.radius * 0.9f;
                var dist = 10f;
                // position of the SphereCast origin starting at the base of the capsule
                Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                // ray for RayCast
                Ray ray1 = new Ray(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down);
                // ray for SphereCast
                Ray ray2 = new Ray(pos, -Vector3.up);
                // raycast for check the ground distance
                if (Physics.Raycast(ray1, out groundHit, 1f, groundLayer))
                    dist = transform.position.y - groundHit.point.y;

                // sphere cast around the base of the capsule to check the ground distance
                if (Physics.SphereCast(ray2, radius, out groundHit, 1f, groundLayer))
                {
                    // check if sphereCast distance is small than the ray cast distance
                    if (dist > (groundHit.distance - _capsuleCollider.radius * 0.1f))
                        dist = (groundHit.distance - _capsuleCollider.radius * 0.1f);
                }
                groundDistance = dist;
            }
        }

        /// <summary>
        /// STEP OFFSET LIMIT - check the height of the object ahead, control by stepOffSet
        /// </summary>
        /// <returns></returns>
        bool StepOffset()
        {
            if (input.sqrMagnitude < 0.1 || !onGround) return false;

            var hit = new RaycastHit();
            Ray rayStep = new Ray((transform.position + new Vector3(0, stepOffsetEnd, 0) + transform.forward * ((_capsuleCollider).radius + 0.05f)), Vector3.down);

            if (Physics.Raycast(rayStep, out hit, stepOffsetEnd - stepOffsetStart, groundLayer))
            {
                if (!stopMove && hit.point.y >= (transform.position.y) && hit.point.y <= (transform.position.y + stepOffsetEnd))
                {
                    var heightPoint = new Vector3(transform.position.x, hit.point.y + 0.1f, transform.position.z);
                    transform.position = Vector3.Lerp(transform.position, heightPoint, (speed * stepSmooth) * Time.fixedDeltaTime);
                    //var heightPoint = new Vector3(_rigidbody.velocity.x, hit.point.y + 0.1f, _rigidbody.velocity.z);
                    //_rigidbody.velocity = heightPoint * 10f * Time.fixedDeltaTime;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// CHECK GROUND ANGLE 
        /// </summary>
        /// <returns></returns>
        float GroundAngle()
        {
            var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            return groundAngle;
        }

        /// <summary>
        /// STOP MOVE - stop the character if hits a wall and apply slope limit to ramps
        /// </summary>
        protected void StopMove()
        {
            if (input.sqrMagnitude < 0.1 || !onGround) return;

            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position + new Vector3(0, stopMoveHeight, 0), transform.forward);

            if (Physics.Raycast(ray, out hitinfo, _capsuleCollider.radius + stopMoveDistance, stopMoveLayer) && !usingLadder)
            {
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                if (hitinfo.distance <= stopMoveDistance && hitAngle > 85)
                    stopMove = true;
                else if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else if (Physics.Raycast(ray, out hitinfo, 1f, groundLayer) && !usingLadder)
            {
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);
                if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else
                stopMove = false;
        }

        /// <summary>
        /// AUTO CROUCH - keep it crouched while you have something above
        /// </summary>
        protected void CheckAutoCrouch()
        {
            // radius of SphereCast
            float radius = _capsuleCollider.radius * 0.9f;
            // Position of SphereCast origin stating in base of capsule
            Vector3 pos = transform.position + Vector3.up * ((colliderHeight * 0.5f) - colliderRadius);
            // ray for SphereCast
            Ray ray2 = new Ray(pos, Vector3.up);

            // sphere cast around the base of capsule for check ground distance
            if (Physics.SphereCast(ray2, radius, out groundHit, headDetect - (colliderRadius * 0.1f), autoCrouchLayer))
            {
                autoCrouch = true;
                crouch = true;
            }
            else
                autoCrouch = false;
        }

        /// <summary>
        /// ACTIONS - raycast to check if there is anything interactable ahead
        /// </summary>
        protected GameObject CheckActionObject()
        {
            bool checkConditions = onGround && !landHigh && !actions && !inAttack;
            GameObject _object = null;

            if (checkConditions)
            {
                RaycastHit hitInfoAction;
                Vector3 yOffSet = new Vector3(0f, actionRayHeight, 0f);
                Vector3 fwd = transform.TransformDirection(Vector3.forward);

                if (Physics.Raycast(transform.position + yOffSet, fwd, out hitInfoAction, distanceOfRayActionTrigger, actionLayer))
                {
                    _object = hitInfoAction.transform.gameObject;
                }
            }
            return currentCollectable != null ? currentCollectable : _object;
        }

        public float distanceOfRayActionTrigger
        {
            get
            {
                if (_capsuleCollider == null) return 0f;
                var dist = _capsuleCollider.radius + actionRayDistance;
                return dist;
            }
        }

        /// <summary>
        /// ROTATE WITH CAMERA - rotate the character with the camera
        /// </summary>
        protected virtual void RotateWithCamera()
        {
            if (strafing && !actions && !lockPlayer)
            {
                // smooth align character with aim position
                if (tpCamera != null && tpCamera.lockTarget)
                {
                    Quaternion rot = Quaternion.LookRotation(tpCamera.lockTarget.position - transform.position);
                    Quaternion newPos = Quaternion.Euler(transform.eulerAngles.x, rot.eulerAngles.y, transform.eulerAngles.z);
                    transform.rotation = Quaternion.Slerp(transform.rotation, newPos, rotationSpeed * Time.fixedDeltaTime);
                }
                else if(input != Vector2.zero)
                {
                    if (!inAttack)
                    {
                        Quaternion newPos = Quaternion.Euler(transform.eulerAngles.x, cameraTransform.eulerAngles.y, transform.eulerAngles.z);
                        transform.rotation = Quaternion.Slerp(transform.rotation, newPos, rotationSpeed * Time.fixedDeltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Find the correct direction based on the camera direction
        /// </summary>
        protected Vector3 targetDirection
        {
            get
            {
                Vector3 refDir = Vector3.zero;
                cameraForward = keepDirection ? cameraForward : cameraTransform.TransformDirection(Vector3.forward);
                cameraForward.y = 0;

                if (tpCamera == null || !tpCamera.currentState.cameraMode.Equals(TPCameraMode.FixedAngle) || !rotateByWorld)
                {
                    //cameraForward = tpCamera.transform.TransformDirection(Vector3.forward);
                    cameraForward = keepDirection ? cameraForward : cameraTransform.TransformDirection(Vector3.forward);
                    cameraForward.y = 0; //set to 0 because of camera rotation on the X axis

                    //get the right-facing direction of the camera
                    cameraRight = keepDirection ? cameraRight : cameraTransform.TransformDirection(Vector3.right);

                    // determine the direction the player will face based on input and the camera's right and forward directions
                    refDir = input.x * cameraRight + input.y * cameraForward;
                }
                else
                {
                    refDir = new Vector3(input.x, 0, input.y);
                }
                return refDir;
            }
        }

        public override void ResetRagdoll()
        {
            tpCamera.offSetPlayerPivot = offSetPivot;
            tpCamera.SetTarget(this.transform);
            lockPlayer = false;
            verticalVelocity = 0f;
            ragdolled = false;
        }

        public override void RagdollGettingUp()
        {
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _capsuleCollider.enabled = true;
        }

        public override void EnableRagdoll()
        {
            tpCamera.offSetPlayerPivot = 0f;
            tpCamera.SetTarget(animator.GetBoneTransform(HumanBodyBones.Hips));
            ragdolled = true;
            _capsuleCollider.enabled = false;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            lockPlayer = true;
        }

        /// <summary>
        /// DEBUG MODE - display information about the controller in real time
        /// </summary>
        protected void DebugMode()
        {
            Time.timeScale = timeScale;

            if (hud == null)
                return;

            if (hud.debugPanel != null)
            {
                if (debugWindow)
                {
                    hud.debugPanel.SetActive(true);
                    float delta = Time.smoothDeltaTime;
                    float fps = 1 / delta;

                    hud.debugPanel.GetComponentInChildren<Text>().text =
                        "FPS " + fps.ToString("#,##0 fps") + "\n" +
                        "CameraState = " + tpCamera.currentStateName.ToString() + "\n" +
                        "Input = " + Mathf.Clamp(input.sqrMagnitude, 0, 1f).ToString("0.0") + "\n" +
                        "Speed = " + speed.ToString("0.0") + "\n" +
                        "Direction = " + direction.ToString("0.0") + "\n" +
                        "VerticalVelocity = " + verticalVelocity.ToString("0.00") + "\n" +
                        "GroundDistance = " + groundDistance.ToString("0.00") + "\n" +
                        "GroundAngle = " + GroundAngle().ToString("0.00") + "\n" +
                        "\n" + "--- Movement Bools ---" + "\n" +
                        "LockPlayer = " + lockPlayer.ToString() + "\n" +
                        "StopMove = " + stopMove.ToString() + "\n" +
                        "Ragdoll = " + ragdolled.ToString() + "\n" +
                        "onGround = " + onGround.ToString() + "\n" +
                        "canSprint = " + canSprint.ToString() + "\n" +
                        "crouch = " + crouch.ToString() + "\n" +
                        "strafe = " + strafing.ToString() + "\n" +
                        "turn180 = " + quickTurn.ToString() + "\n" +
                        "quickStop = " + quickStop.ToString() + "\n" +
                        "landHigh = " + landHigh.ToString() + "\n" +
                        "jump = " + jump.ToString() + "\n" +
                        "\n" + "--- Actions Bools ---" + "\n" +
                        "roll = " + roll.ToString() + "\n" +
                        "stepUp = " + stepUp.ToString() + "\n" +
                        "jumpOver = " + jumpOver.ToString() + "\n" +
                        "climbUp = " + climbUp.ToString() + "\n" +
                        "usingLadder = " + usingLadder.ToString() + "\n" +
                        "blocking = " + blocking.ToString() + "\n" +
                        "InAttack = " + inAttack.ToString() + "\n";

                }
                else
                    hud.debugPanel.SetActive(false);
            }
        }

        int GetDamageResult(int damage, float defenseRate)
        {
            int result = (int)(damage - ((damage * defenseRate) / 100));
            return result;
        }

        /// <summary>
        /// Defense Behaviour when get hit
        /// </summary>
        /// <param name="damage"></param>
        bool BlockAttack(Damage damage)
        {                 
            // while the character is defending 
            var defenceRangeConditions = (meleeManager != null && meleeManager.CurrentMeleeDefense() != null ? meleeManager.CurrentMeleeDefense().AttackInDefenseRange(damage.sender) : false);
            if (blocking && currentStamina > damage.value && !damage.ignoreDefense && defenceRangeConditions)
            {
                var recoil_id = damage.recoil_id;
                var targetRecoil_id = (meleeManager.CurrentMeleeDefense() != null ? meleeManager.CurrentMeleeDefense().Recoil_ID : 0);
                // trigger a recoil animation based on the ID set by the attack of the attacker
                SendMessage("TriggerRecoil", recoil_id, SendMessageOptions.DontRequireReceiver);
                if (meleeManager.CurrentMeleeDefense().breakAttack && damage.sender != null)
                    damage.sender.SendMessage("TriggerRecoil", targetRecoil_id, SendMessageOptions.DontRequireReceiver);

                // reduce the current stamina based on the damage of the attack
                ReduceStamina(damage.value);
                recoveryDelay = meleeManager.CurrentMeleeDefense().d_staminaRecoveryDelay;

                // reduce the current health if your defense rate is lower then 100
                var damageResult = GetDamageResult(damage.value, meleeManager.CurrentMeleeDefense().defenseRate);
                currentHealth -= damageResult;

                // trigger the def sound of your defense weapon
                meleeManager.CurrentMeleeDefense().PlayDEFSound();
                if (damage.sender != null && damageResult > 0)
                    // play the hit sound based on the weapon that attacked you - check the weapon sounds
                    damage.sender.SendMessage("PlayHitSound", SendMessageOptions.DontRequireReceiver);

                // reduce the current health based on the damage of the weapon
                currentHealth -= damageResult;
                currentHealthRecoveryDelay = healthRecoveryDelay;
                return true;           
            }
            return false;
        }

        /// <summary>
        /// APPLY DAMAGE - call this method by a SendMessage with the damage value
        /// </summary>
        /// <param name="damage"> damage to apply </param>
        public override void TakeDamage(Damage damage)
        {
            // don't apply damage if the character is rolling, you can add more conditions here
            if (!damage.ignoreDefense && roll || isDead)
                return;

            // defend attack behaviour
            if(BlockAttack(damage))
                return;

            // only trigger the hitReaction animation when the character is not doing an action, using ladder or jumping
            var hitReactionConditions = ((!actions || (actions && hitReaction)) || (usingLadder)) && !isJumping;
            if (hitReactionConditions)
            {
                // set the ID of the reaction based on the attack animation state of the attacker - Check the MeleeAttackBehaviour script
                animator.SetInteger("Recoil_ID", damage.recoil_id);
                // trigger hitReaction animation
                animator.SetTrigger("HitReaction");
            }

            // instantiate the hitDamage particle - check if your character has a HitDamageParticle component
            var hitrotation = Quaternion.LookRotation(new Vector3(transform.position.x, damage.hitPosition.y, transform.position.z) - damage.hitPosition);
            SendMessage("TriggerHitParticle", new HittEffectInfo(new Vector3(transform.position.x, damage.hitPosition.y, transform.position.z), hitrotation, damage.attackName), SendMessageOptions.DontRequireReceiver);

            // reduce the current health by the damage amount.
            currentHealth -= damage.value;
            currentHealthRecoveryDelay = healthRecoveryDelay;

            // trigger the hit sound 
            if(damage.sender!=null)
            damage.sender.SendMessage("PlayHitSound", SendMessageOptions.DontRequireReceiver);

            // update the hud graphics 
            if (hud != null)
            {
                // set the damaged flag so the screen will flash.
                hud.damageImage.enabled = true;
                hud.damaged = true;
                // set the health bar's value to the current health.
                hud.healthSlider.value = currentHealth;
            }

            // apply vibration on the gamepad             
            transform.SendMessage("GamepadVibration", 0.25f, SendMessageOptions.DontRequireReceiver);
            // turn the ragdoll on if the weapon is checked with 'activeRagdoll' 
            if (damage.activeRagdoll)
                transform.SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
        }
    }
}