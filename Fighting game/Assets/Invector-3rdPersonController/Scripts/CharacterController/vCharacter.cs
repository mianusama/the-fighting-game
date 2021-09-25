using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

namespace Invector
{
    [System.Serializable]
    public abstract class vCharacter : MonoBehaviour
    {
        #region Character Variables      

        [Header("---! Layers !---")]
        [Tooltip("Layers that the character can walk on")]
        public LayerMask groundLayer = 1 << 0;
        [Tooltip("Distance to became not grounded")]
        [SerializeField] protected float groundCheckDistance = 0.5f;

        [Tooltip("What objects can make the character auto crouch")]
        public LayerMask autoCrouchLayer = 1 << 0;
        [Tooltip("[SPHERECAST] ADJUST IN PLAY MODE - White Spherecast put just above the head, this will make the character Auto-Crouch if something hit the sphere.")]
        public float headDetect = 0.95f;       

        [Tooltip("Gameobjects that has the ActionTrigger component to trigger a action")]
	    public LayerMask actionLayer;
        [Tooltip("[RAYCAST] Height of the ActionTriggers Raycast, normally at the knee height of your character works fine")]
        public float actionRayHeight = 0.5f;
        [Tooltip("[RAYCAST] Distance of the ActionTriggers Raycast, make sure that this raycast reachs the Trigger Collider")]
        public float actionRayDistance = 0.25f;
	    
	    [Tooltip("Select the layers the your character will stop moving when close to")]
	    public LayerMask stopMoveLayer;
	    [Tooltip("[RAYCAST] Stopmove Raycast Height")]
	    public float stopMoveHeight = 0.65f;
	    [Tooltip("[RAYCAST] Stopmove Raycast Distance")]
	    public float stopMoveDistance = 0.5f;

	    [Header("--- Health & Stamina ---")]
	    public float maxHealth = 100f;
	    public float currentHealth;
	    public float healthRecovery = 0f;
	    public float healthRecoveryDelay = 0f;
        public float maxStamina = 100f;
        public float staminaRecovery = 1.2f;

        protected float recoveryDelay;
        protected bool recoveringStamina;
        protected bool canRecovery;
        protected float currentStamina;
        protected float currentHealthRecoveryDelay;

        protected bool isDead;
        public enum DeathBy
        {
            Animation,
            AnimationWithRagdoll,
            Ragdoll
        }

        public DeathBy deathBy = DeathBy.Animation;

        // get the animator component of character
        [HideInInspector] public Animator animator;
        // know if the character is ragdolled or not
        [HideInInspector] public bool ragdolled { get; set; }

        #endregion

		public Transform GetTransform
		{
			get{ return transform; }
		}
 
        public virtual void ResetRagdoll()
        {

        }

        public virtual void RagdollGettingUp()
        {

        }

        public virtual void EnableRagdoll()
        {

        }

        public virtual void TakeDamage(Damage damage)
        {

        }
    }

}