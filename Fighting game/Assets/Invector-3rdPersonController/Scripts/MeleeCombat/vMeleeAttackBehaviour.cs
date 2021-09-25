using UnityEngine;
using System.Collections;
using Invector;
public class vMeleeAttackBehaviour : StateMachineBehaviour
{       
    [Tooltip("normalizedTime of Active Damage")]   
    public float startDamage = 0.05f;
    [Tooltip("normalizedTime of Disable Damage")]
    public float endDamage = 0.9f;
    [Tooltip("Set the reaction/recoil animation for the target if the defense is not check with BreakAttack")]
    public int recoil_ID;
    [Tooltip("Set what limb the attack will come from")]
    public HitboxFrom hitboxFrom;
    [Tooltip("Check this bool on every LAST attack state animation to prevent attacking again right after the last attack")]
    public bool resetTrigger;    
    public string attackName;
	[HideInInspector]
	public bool isActive;

    //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {		
        animator.gameObject.SendMessage("OnAttackEnter", hitboxFrom, SendMessageOptions.DontRequireReceiver);
    }        

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.gameObject.SendMessage("InAttacking", SendMessageOptions.DontRequireReceiver);
		if (stateInfo.normalizedTime >= startDamage && stateInfo.normalizedTime <= endDamage && !isActive)
        {     
			isActive = true;
			animator.gameObject.SendMessage("EnableDamage", new AttackObject(hitboxFrom, recoil_ID, true, attackName), SendMessageOptions.DontRequireReceiver);
        }
		else if(stateInfo.normalizedTime > endDamage && isActive )			
		{
            isActive = false;
            animator.gameObject.SendMessage("FinishAttack", SendMessageOptions.DontRequireReceiver);
            animator.gameObject.SendMessage("EnableDamage", new AttackObject(hitboxFrom, recoil_ID, false,attackName), SendMessageOptions.DontRequireReceiver);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.gameObject.SendMessage("OnAttackExit", SendMessageOptions.DontRequireReceiver);
        if(resetTrigger)
            animator.gameObject.SendMessage("ResetTrigger", SendMessageOptions.DontRequireReceiver);
        if(isActive)
        {
            isActive = false;
            animator.gameObject.SendMessage("FinishAttack", SendMessageOptions.DontRequireReceiver);
            animator.gameObject.SendMessage("EnableDamage", new AttackObject(hitboxFrom, recoil_ID, false, attackName), SendMessageOptions.DontRequireReceiver);
        }
           
    }
}