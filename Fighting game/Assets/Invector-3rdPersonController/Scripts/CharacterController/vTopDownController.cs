using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

namespace Invector.CharacterController
{
    public class vTopDownController : vThirdPersonAnimator
    {
        private static vTopDownController _instance;
        public static vTopDownController instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<vTopDownController>();
                    //Tell unity not to destroy this object when loading a new scene
                    //DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        private bool isAxisInUse;
        private RaycastHit hit;

        public enum GameplayInputStyle
        {
            ClickAndMove,
            DirectionalInput            
        }

        [Header("--- Topdown Input Type ---")]
        public GameplayInputStyle gameplayInputStyle = GameplayInputStyle.DirectionalInput;
        [HideInInspector]   public TopDownCursor topDownCursor;

        void Awake()
        {
            StartCoroutine("UpdateRaycast");	// limit raycasts calls for better performance
        }

        void Start()
        {
            InitialSetup();						// setup the basic information, created on Character.cs	
        }

        void FixedUpdate()
        {
		    UpdateMotor();					// call ThirdPersonMotor methods
		    UpdateAnimator();				// update animations on the Animator and their methods
		    UpdateHUD();                    // update HUD elements like health bar, texts, etc
            ControlCameraState();			// change CameraStates
        }

        void LateUpdate()
        {
            InputHandle();					// handle input from controller, keyboard&mouse or touch
            RotateWithCamera();				// rotate the character with the camera    
		    DebugMode();					// display information about the character on PlayMode
        }


        //**********************************************************************************//
        // INPUT  		      																//
        // here you can set up everything that recieve input								//   
        //**********************************************************************************//
        void InputHandle()
        {
            CloseApp();
            CameraInput();
            if (!lockPlayer && !ragdolled)
            {
                ControllerInput();

                // we have mapped the 360 controller as our Default gamepad, 
                // you can change the keyboard inputs by chaging the Alternative Button on the InputManager.                
                InteractInput();
                JumpInput();
                RollInput();
                CrouchInput();
                AttackInput();
                DefenseInput();
                SprintInput();
                LockOnInput();
                DropWeaponInput("D-Pad Horizontal");
            }
            else
                LockPlayer();
        }

        /// <summary>
        /// Conditions to lock the Controller Input's and reset some variables.
        /// </summary>
        void LockPlayer()
        {
            input = Vector2.zero;
            speed = 0f;
            canSprint = false;
            if (hud != null) hud.HideActionText();
        }

        //**********************************************************************************//
        // UPDATE RAYCASTS																	//
        // handles a separate update for better performance									//
        //**********************************************************************************//
        public IEnumerator UpdateRaycast()
	    {
		    while (true)
		    {
			    yield return new WaitForEndOfFrame();
			
			    CheckAutoCrouch();
			    StopMove();
            }
	    }

        //**********************************************************************************//
        // CAMERA STATE    		      														//
        // you can change de CameraState here, the bool means if you want lerp of not		//
        // make sure to use the same CameraState String that you named on TPCameraListData  //
        //**********************************************************************************//
        void ControlCameraState()
        {
            tpCamera.ChangeState("Default", true);
        }

	    //**********************************************************************************//
	    // CONTROLLER INPUT		      														//
	    // gets input from keyboard, gamepad or mobile touch								//
	    //**********************************************************************************//
        void ControllerInput()
        {
            if (gameplayInputStyle == GameplayInputStyle.ClickAndMove)
            {
                if (topDownCursor == null)
                {
                    var go = GameObject.Instantiate(Resources.Load("TopDownCursor"), transform.position, Quaternion.identity) as GameObject;
                    topDownCursor = go.GetComponent<TopDownCursor>();
                }
               
                var dir = (topDownCursor.transform.position - transform.position).normalized;
                if (Input.GetMouseButton(0))
                {
                    if (Physics.Raycast(tpCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity))
                    {
                        //if(hit.collider.CompareTag("Finish"))
                        //{
                            topDownCursor.Enable();
                            topDownCursor.transform.position = hit.point;
                        //}                        
                    }
                }
                if (!topDownCursor.Near(transform.position, 0.5f))
                    input = Vector2.Lerp(input, new Vector2(dir.x, dir.z), rotationSpeed * Time.deltaTime);
                else
                {
                    topDownCursor.Disable();
                    input = Vector2.Lerp(input, Vector3.zero, 20 * Time.deltaTime);
                }
            }
            else if(gameplayInputStyle == GameplayInputStyle.DirectionalInput)
            {
                if (inputType == InputType.Mobile)
                    input = new Vector2(CrossPlatformInputManager.GetAxis("Horizontal"), CrossPlatformInputManager.GetAxis("Vertical"));
                else if (inputType == InputType.MouseKeyboard)
                    input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                else if (inputType == InputType.Controler)
                {
                    float deadzone = 0.25f;
                    input = new Vector2(Input.GetAxis("LeftAnalogHorizontal"), Input.GetAxis("LeftAnalogVertical"));
                    if (input.magnitude < deadzone)
                        input = Vector2.zero;
                    else
                        input = input.normalized * ((input.magnitude - deadzone) / (1 - deadzone));
                }
            }           
        }

        //**********************************************************************************//
        // CAMERA INPUT	 		      														//
        //**********************************************************************************//
        void CameraInput()
        {
            if (inputType == InputType.Mobile)
            {
                tpCamera.RotateCamera(CrossPlatformInputManager.GetAxis("Mouse X"), CrossPlatformInputManager.GetAxis("Mouse Y"));                
            }
            else if (inputType == InputType.MouseKeyboard)
            {
                tpCamera.RotateCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                tpCamera.Zoom(Input.GetAxis("Mouse ScrollWheel"));
            }
            else if (inputType == InputType.Controler)
            {
                tpCamera.RotateCamera(Input.GetAxis("RightAnalogHorizontal"), Input.GetAxis("RightAnalogVertical"));
            }
        }

        void SprintInput()
        {
            if (!actionsController.Sprint.use) return;
            var _input = actionsController.Sprint.input.ToString();

            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButtonDown(_input) && currentStamina > 0 && input.sqrMagnitude > 0.1f)
                {
                    strafing = false;
                    if (onGround && !crouch)
                        canSprint = !canSprint;
                }
                else if (currentStamina <= 0 || input.sqrMagnitude < 0.1f)
                    canSprint = false;
            }
            else
            {
                if (Input.GetButtonDown(_input) && currentStamina > 0 && input.sqrMagnitude > 0.1f)
                {
                    if (onGround && !crouch)
                        canSprint = !canSprint;
                }
                else if (currentStamina <= 0 || input.sqrMagnitude < 0.1f)
                    canSprint = false;
            }

            if (canSprint)
            {
                recoveryDelay = actionsController.Sprint.recoveryDelay;
                ReduceStamina(actionsController.Sprint.staminaCost);
            }
        }

        void CrouchInput()
        {
            if (!actionsController.Crouch.use) return;
            var _input = actionsController.Crouch.input.ToString();

            if (autoCrouch)
                crouch = true;
            else if (actionsController.Crouch.pressToCrouch)
            {
                if (inputType == InputType.Mobile)
                    crouch = (CrossPlatformInputManager.GetButton(_input) && onGround);
                else
                    crouch = Input.GetButton(_input) && onGround && !actions;
            }
            else
            {
                if (inputType == InputType.Mobile)
                {
                    if (CrossPlatformInputManager.GetButtonDown(_input) && onGround)
                        crouch = !crouch;
                }
                else
                {
                    if (Input.GetButtonDown(_input) && onGround && !actions)
                        crouch = !crouch;
                }
            }
        }

        void JumpInput()
        {
            if (!actionsController.Jump.use) return;
            var _input = actionsController.Jump.input.ToString();

            bool staminaConditions = currentStamina > actionsController.Jump.staminaCost;
            bool jumpConditions = !crouch && onGround && !actions && staminaConditions && !strafing && !inAttack;

            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButtonDown(_input) && jumpConditions)
                {
                    jump = true;
                    ReduceStamina(actionsController.Jump.staminaCost);
                    recoveryDelay = actionsController.Jump.recoveryDelay;
                }
            }
            else
            {
                if (Input.GetButtonDown(_input) && jumpConditions)
                {
                    jump = true;
                    ReduceStamina(actionsController.Jump.staminaCost);
                    recoveryDelay = actionsController.Jump.recoveryDelay;
                }
            }
        }

        void StrafeInput()
        {
            if (!actionsController.Strafe.use) return;
            var _input = actionsController.Strafe.input.ToString();

            if (!locomotionType.Equals(LocomotionType.OnlyFree))
            {
                if (inputType == InputType.Mobile)
                {
                    if (CrossPlatformInputManager.GetButtonDown(_input) && !actions)
                    {
                        animator.SetFloat("Direction", 0f);
                        strafing = !strafing;
                    }
                }
                else
                {
                    if (Input.GetButtonDown(_input) && !actions)
                    {
                        animator.SetFloat("Direction", 0f);
                        strafing = !strafing;
                    }
                }
            }
        }

        void LockOnInput()
        {
            if (!actionsController.LockOn.use) return;
            var _input = actionsController.LockOn.input.ToString();

            if (!locomotionType.Equals(LocomotionType.OnlyFree))
            {
                if (inputType == InputType.Mobile)
                {
                    if (CrossPlatformInputManager.GetButtonDown(_input) && !actions)
                    {
                        lockIntoTarget = !lockIntoTarget;
                        tpCamera.gameObject.SendMessage("UpdateLockOn", lockIntoTarget, SendMessageOptions.DontRequireReceiver);
                    }
                }
                else
                {
                    if (Input.GetButtonDown(_input) && !actions)
                    {
                        lockIntoTarget = !lockIntoTarget;
                        tpCamera.gameObject.SendMessage("UpdateLockOn", lockIntoTarget, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }

            if (tpCamera.lockTarget)
            {
                // Switch between targets using Keyboard
                if (inputType == InputType.MouseKeyboard)
                {
                    if (Input.GetKey(KeyCode.X))
                        tpCamera.gameObject.SendMessage("ChangeTarget", 1, SendMessageOptions.DontRequireReceiver);
                    else if (Input.GetKey(KeyCode.Z))
                        tpCamera.gameObject.SendMessage("ChangeTarget", -1, SendMessageOptions.DontRequireReceiver);
                }
                // Switch between targets using GamePad
                else if (inputType == InputType.Controler)
                {
                    var value = Input.GetAxisRaw("RightAnalogHorizontal");
                    if (value == 1)
                        tpCamera.gameObject.SendMessage("ChangeTarget", 1, SendMessageOptions.DontRequireReceiver);
                    else if (value == -1f)
                        tpCamera.gameObject.SendMessage("ChangeTarget", -1, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void RollInput()
        {
            if (!actionsController.Roll.use) return;
            var _input = actionsController.Roll.input.ToString();

            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButtonDown(_input))
                    Rolling();
            }
            else
            {
                if (Input.GetButtonDown(_input))
                    Rolling();
            }
        }

        /// <summary>
        /// ATTACK INPUT - trigger a attack animation if the character are equipped with a attack weapon 
        /// </summary>        
        void AttackInput()
        {
            if (!actionsController.Attack.use) return;
            if (meleeManager == null) return;

            var _input = actionsController.Attack.input.ToString();

            // attack conditions
            bool weaponStaminaConditions = meleeManager.CurrentMeleeAttack() != null && currentStamina > meleeManager.CurrentMeleeAttack().staminaCost;
            bool attackConditions = (!actions || roll) && onGround && weaponStaminaConditions && !isJumping && !jump;

            // attack input
            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButtonDown(_input) && attackConditions)
                    animator.SetTrigger("MeleeAttack");
            }
            else
            {
                if (Input.GetButtonDown(_input) && attackConditions)
                    animator.SetTrigger("MeleeAttack");
            }
        }

        /// <summary>
        /// ATTACK INPUT - trigger a defense animation if the character are equipped with a defense weapon 
        /// </summary>  
        void DefenseInput()
        {
            if (!actionsController.Defense.use) return;
            if (meleeManager == null) return;

            var _input = actionsController.Defense.input.ToString();

            // defense contitions    
            bool shieldStaminaConditions = meleeManager.CurrentMeleeDefense() != null; //|| meleeManager.CurrentMeleeDefense(HitboxFrom.RightArm) != null;
            bool defenseConditions = (!actions || roll || hitRecoil) && onGround && shieldStaminaConditions && !inAttack;

            // defense input
            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButton(_input) && defenseConditions)
                    blocking = true;
                else
                    blocking = false;
            }
            else
            {
                if (Input.GetButton(_input) && defenseConditions)
                    blocking = true;
                else
                    blocking = false;
            }
        }

        /// <summary>
        /// ACTIONS - WHITE raycast to check if there is anything interactable ahead        
        /// </summary>
        void InteractInput()
        {
            if (!actionsController.Interact.use) return;
            var _input = actionsController.Interact.input.ToString();

            var hitObject = CheckActionObject();
            if (hitObject != null)
            {
                try
                {
                    if (hitObject.CompareTag("ClimbUp"))
                        DoAction(hitObject, ref climbUp, _input);
                    else if (hitObject.CompareTag("StepUp"))
                        DoAction(hitObject, ref stepUp, _input);
                    else if (hitObject.CompareTag("JumpOver"))
                        DoAction(hitObject, ref jumpOver, _input);
                    else if (hitObject.CompareTag("AutoCrouch"))
                        autoCrouch = true;
                    else if (hitObject.CompareTag("EnterLadderBottom") && !jump)
                        DoAction(hitObject, ref enterLadderBottom, _input);
                    else if (hitObject.CompareTag("EnterLadderTop") && !jump)
                        DoAction(hitObject, ref enterLadderTop, _input);
                    else if (hitObject.CompareTag("Weapon"))
                        PickUpEquipmentInput(hitObject, "D-Pad Horizontal");
                }
                catch (UnityException e)
                {
                    Debug.LogWarning(e.Message);
                }
            }
            else if (hud != null)
            {
                if (hud.showInteractiveText)
                    hud.HideActionText();
                if (hud.showEquipText)
                    hud.HideEquipText();
            }
        }

        /// <summary>
        /// DO ACTION - execute a action when press the action button, use with TriggerAction script
        /// </summary>
        /// <param name="hitObject"> gameobject with the component TriggerAction</param>
        /// <param name="action"> action bool </param>
        void DoAction(GameObject hitObject, ref bool action, string _input)
        {
            var triggerAction = hitObject.transform.GetComponent<vTriggerAction>();
            if (!triggerAction)
            {
                Debug.LogWarning("Missing TriggerAction Component on " + hitObject.transform.name + "Object");
                return;
            }
            if (hud != null && !triggerAction.autoAction) hud.ShowActionText(triggerAction.message);

            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButtonDown(_input) && !actions || triggerAction.autoAction && !actions)
                {
                    if (hud != null) hud.HideActionText();
                    matchTarget = triggerAction.target;
                    var rot = hitObject.transform.rotation;
                    transform.rotation = rot;
                    animator.SetTrigger("ResetState");
                    action = true;
                }
            }
            else
            {
                if (Input.GetButtonDown(_input) && !actions || triggerAction.autoAction && !actions)
                {
                    // turn the action bool true and call the animation
                    action = true;
                    // disable the text and sprite 
                    if (hud != null) hud.HideActionText();
                    // find the cursorObject height to match with the character animation
                    matchTarget = triggerAction.target;
                    // align the character rotation with the object rotation
                    var rot = hitObject.transform.rotation;
                    animator.SetTrigger("ResetState");
                    transform.rotation = rot;
                }
            }
        }

        /// <summary>
        /// The method CheckActionObject() will return a gameobject type of CollectableItem if you stay inside the Trigger area
        /// </summary>
        /// <param name="collectableItem"></param>
        void PickUpEquipmentInput(GameObject collectableItem, string _input)
        {
            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButtonDown("EquipLeftMobile") && !actions)
                {
                    if (isAxisInUse == false)
                    {
                        PickUpEquip(collectableItem, -1);
                        isAxisInUse = true;
                    }
                }
                else if (CrossPlatformInputManager.GetButtonUp("EquipLeftMobile"))
                    isAxisInUse = false;

                if (CrossPlatformInputManager.GetButtonDown("EquipRightMobile") && !actions)
                {
                    if (isAxisInUse == false)
                    {
                        PickUpEquip(collectableItem, 1);
                        isAxisInUse = true;
                    }
                }
                else if (CrossPlatformInputManager.GetButtonUp("EquipRightMobile"))
                    isAxisInUse = false;
            }
            else
            {
                if (Input.GetAxisRaw(_input) == 1f && !actions)
                {
                    if (isAxisInUse == false)
                    {
                        PickUpEquip(collectableItem, 1);
                        isAxisInUse = true;
                    }
                }
                else if (Input.GetAxisRaw(_input) == -1f && !actions)
                {
                    if (isAxisInUse == false)
                    {
                        PickUpEquip(collectableItem, -1);
                        isAxisInUse = true;
                    }
                }
                if (Input.GetAxisRaw(_input) == 0)
                    isAxisInUse = false;
            }
        }

        void PickUpEquip(GameObject collectableItem, int side)
        {
            var collectable = collectableItem.GetComponent<vCollectableMelee>();
            var weapon = collectable.GetComponentInChildren<vMeleeWeapon>();

            if (meleeManager != null)
            {
                if (side == 1 && CheckPickUpCondictions(1, weapon))
                    meleeManager.SetRightWeaponHandler(collectable);
                else if (side == 1 && meleeManager.currentMeleeWeaponRA != null)
                    meleeManager.DropRightWeapon();
                else if (side == -1 && CheckPickUpCondictions(-1, weapon))
                    meleeManager.SetLeftWeaponHandler(collectable);
                else if (side == -1 && meleeManager.currentMeleeWeaponLA != null)
                    meleeManager.DropLeftWeapon();
            }

            if (hud != null) hud.HideEquipText();
            currentCollectable = null;
        }

        bool CheckPickUpCondictions(int side, vMeleeWeapon weapon)
        {
            if (side == 1)
            {
                if ((weapon.meleeType == vMeleeWeapon.MeleeType.All && (weapon.handEquip == vMeleeWeapon.HandEquip.BothHand || weapon.handEquip == vMeleeWeapon.HandEquip.RightHand)))
                    return true;
                if (weapon.meleeType == vMeleeWeapon.MeleeType.Attack) return true;
            }
            else if (side == -1)
            {
                if (weapon.useTwoHand) return false;
                if ((weapon.meleeType == vMeleeWeapon.MeleeType.All && (weapon.handEquip == vMeleeWeapon.HandEquip.BothHand || weapon.handEquip == vMeleeWeapon.HandEquip.RightHand)))
                    return true;
                if (weapon.meleeType == vMeleeWeapon.MeleeType.Defense) return true;
            }

            return false;
        }

        /// <summary>
        /// DropWeapon - drops the current weapon
        /// </summary>
        void DropWeaponInput(string _input)
        {
            if (inputType == InputType.Mobile)
            {
                if (CrossPlatformInputManager.GetButtonDown("EquipLeftMobile") && !actions)
                {
                    if (isAxisInUse == false)
                    {
                        if (meleeManager != null && meleeManager.CurrentMeleeDefense() != null)
                            meleeManager.DropLeftWeapon();
                        isAxisInUse = true;
                    }
                }
                else if (CrossPlatformInputManager.GetButtonUp("EquipLeftMobile"))
                    isAxisInUse = false;
                if (CrossPlatformInputManager.GetButtonDown("EquipRightMobile") && !actions)
                {
                    if (isAxisInUse == false)
                    {
                        if (meleeManager != null)
                            meleeManager.DropRightWeapon();

                        isAxisInUse = true;
                    }
                }
                else if (CrossPlatformInputManager.GetButtonUp("EquipRightMobile"))
                    isAxisInUse = false;
            }
            else
            {
                if (Input.GetAxisRaw(_input) >= 1f)
                {
                    if (isAxisInUse == false)
                    {
                        if (meleeManager != null)
                            meleeManager.DropRightWeapon();

                        isAxisInUse = true;
                    }
                }
                else if (Input.GetAxisRaw(_input) <= -1f)
                {
                    if (isAxisInUse == false)
                    {
                        if (meleeManager != null && meleeManager.CurrentMeleeDefense() != null)
                            meleeManager.DropLeftWeapon();
                        isAxisInUse = true;
                    }
                }
                if (Input.GetAxisRaw(_input) == 0)
                    isAxisInUse = false;
            }
        }

        /// <summary>
        /// ON TRIGGER STAY
        /// </summary>
        void OnTriggerStay(Collider other)
        {
            try
            {
                // if you enter a trigger area of a CollectableItem, there is a verification to know if it's a weapon or a shield
                if (other.gameObject.CompareTag("Weapon"))
                {
                    var collectable = other.gameObject.GetComponent<vCollectableMelee>();
                    if (collectable != null)
                    {
                        currentCollectable = collectable.gameObject;
                        if (collectable._meleeWeapon.GetType().Equals(typeof(vMeleeWeapon)))
                        {
                            var _meleeWeapon = collectable.gameObject.GetComponent<vMeleeWeapon>();
                            if (_meleeWeapon == null)
                                _meleeWeapon = collectable.gameObject.GetComponentInChildren<vMeleeWeapon>();

                            if (hud != null && _meleeWeapon.meleeType == vMeleeWeapon.MeleeType.Attack)
                                hud.ShowEquipText(collectable.message + " (" + _meleeWeapon.damage.value + " ATK)", _meleeWeapon.meleeType, _meleeWeapon.useTwoHand ? 1 : (int)_meleeWeapon.handEquip);
                            else if (hud != null && _meleeWeapon.meleeType == vMeleeWeapon.MeleeType.Defense)
                                hud.ShowEquipText(collectable.message + " (" + _meleeWeapon.defenseRate + " DEF)", _meleeWeapon.meleeType, (int)_meleeWeapon.handEquip);
                            else if (hud != null && _meleeWeapon.meleeType == vMeleeWeapon.MeleeType.All)
                                hud.ShowEquipText(collectable.message + " (" + _meleeWeapon.damage.value + " ATK)" + " (" + _meleeWeapon.defenseRate + " DEF)", _meleeWeapon.meleeType, _meleeWeapon.useTwoHand ? 1 : (int)_meleeWeapon.handEquip);
                        }
                    }
                }
                // if you are using the ladder and reach the exit from the bottom
                if (other.gameObject.CompareTag("ExitLadderBottom") && usingLadder)
                {
                    if (inputType == InputType.Mobile)
                    {
                        if (CrossPlatformInputManager.GetButtonDown("B") || speed <= -0.05f && !enterLadderBottom)
                            exitLadderBottom = true;
                    }
                    else
                    {
                        if (Input.GetButtonDown("B") || speed <= -0.05f && !enterLadderBottom)
                            exitLadderBottom = true;
                    }
                }
                // if you are using the ladder and reach the exit from the top
                if (other.gameObject.CompareTag("ExitLadderTop") && usingLadder && !enterLadderTop)
                {
                    if (speed >= 0.05f)
                        exitLadderTop = true;
                }
            }
            catch (UnityException e)
            {
                Debug.LogWarning(e.Message);
            }
        }

        /// <summary>
        /// TRIGGER EXIT
        /// </summary>
        /// <param name="other"></param>
        void OnTriggerExit(Collider other)
        {
            // reset the currentCollectable to null if you exit the trigger area 
            if (other.gameObject.CompareTag("Weapon"))
            {
                var collectable = other.gameObject.GetComponent<vCollectableMelee>();
                if (collectable != null)
                {
                    currentCollectable = null;
                }
            }
        }

        /// <summary>
        /// CloseApp 
        /// </summary>
        void CloseApp()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!Cursor.visible)
                    Cursor.visible = true;
                else
                    Application.Quit();
            }
        }
    }
}