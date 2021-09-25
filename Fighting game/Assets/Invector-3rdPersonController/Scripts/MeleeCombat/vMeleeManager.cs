using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Invector;

public class vMeleeManager : MonoBehaviour
{
    public HitProperties hitProperties;
    public bool useDefaultHitbox;

    public vMeleeWeapon leftArmHitbox;
    public vMeleeWeapon rightArmHitbox;
    public vMeleeWeapon leftLegHitbox;
    public vMeleeWeapon rightLegHitbox;

    public vMeleeWeapon currentMeleeWeaponLA;
    public vMeleeWeapon currentMeleeWeaponRA;

    [HideInInspector]
    public vCollectableMelee currentCollectableWeapon;
    [HideInInspector]
    public bool changeWeapon;
    [HideInInspector]
    public bool displayGizmos;
   // [HideInInspector]
    public bool applyDamage;
    [HideInInspector]
    public int damageModifier;

    protected List<Collider> hitTopColliders;
    protected List<Collider> hitCenterColliders;
    protected List<Collider> hitBottomColliders;

    [SerializeField]
    [HideInInspector]
    private Transform leftHandHandler, leftArmHandler, rightHandHandler, rightArmHandler;

    protected HitboxFrom currentHitboxFrom;
    Animator animator;
    [HideInInspector]
    string attackName;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) Destroy(this);

        var rightArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        var leftArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

        rightArmHandler = rightArm.Find(rightArm.name + " Handlers");
        rightHandHandler = rightHand.Find(rightHand.name + " Handlers");
        leftArmHandler = leftArm.Find(leftArm.name + " Handlers");
        leftHandHandler = leftHand.Find(leftHand.name + " Handlers");

        hitTopColliders = new List<Collider>();
        hitCenterColliders = new List<Collider>();
        hitBottomColliders = new List<Collider>();

        InitDefaultMeleeWeapon();
        InitCustomMeleeWeapon();
    }

    public void InitDefaultMeleeWeapon()
    {
        if (leftArmHitbox != null)
        {
            leftArmHitbox.Init();
            leftArmHitbox.hitProperties = this.hitProperties;
        }
        if (leftLegHitbox != null)
        {
            leftLegHitbox.Init();
            leftLegHitbox.hitProperties = this.hitProperties;
        }
        if (rightArmHitbox != null)
        {
            rightArmHitbox.Init();
            rightArmHitbox.hitProperties = this.hitProperties;
        }
        if (rightLegHitbox != null)
        {
            rightLegHitbox.Init();
            rightLegHitbox.hitProperties = this.hitProperties;
        }
        SendMessage("SetCombatID", GetCombatID(), SendMessageOptions.DontRequireReceiver);
    }

    public void InitCustomMeleeWeapon()
    {
        if (currentMeleeWeaponLA == null && leftHandHandler != null)
        {
            var _meleeWeapon = leftHandHandler.GetComponentInChildren<vCollectableMelee>();
            SetLeftWeaponHandler(_meleeWeapon);
        }
        if (currentMeleeWeaponLA == null && leftArmHandler != null)
        {
            var _meleeWeapon = leftArmHandler.GetComponentInChildren<vCollectableMelee>();
            SetLeftWeaponHandler(_meleeWeapon);
        }
        if (currentMeleeWeaponRA == null && rightHandHandler != null)
        {
            var _meleeWeapon = rightHandHandler.GetComponentInChildren<vCollectableMelee>();
            SetRightWeaponHandler(_meleeWeapon);
        }
        if (currentMeleeWeaponRA == null && rightArmHandler != null)
        {
            var _meleeWeapon = rightArmHandler.GetComponentInChildren<vCollectableMelee>();
            SetRightWeaponHandler(_meleeWeapon);
        }
    }

    public void SetTagToDetect(Transform _transform)
    {
        if (_transform!=null&& hitProperties.hitDamageTags != null && !hitProperties.hitDamageTags.Contains(_transform.tag))
        {            
            hitProperties.hitDamageTags.Add(_transform.tag);
            UpdateHitProperties();
        }            
    }

    public void RemoveTag(Transform _transform)
    {
        if (hitProperties.hitDamageTags != null && hitProperties.hitDamageTags.Contains(_transform.tag))
        {
            hitProperties.hitDamageTags.Remove(_transform.tag);
            UpdateHitProperties();
        }             
    }

    void UpdateHitProperties()
    {
        if (currentMeleeWeaponLA != null)
            currentMeleeWeaponLA.hitProperties = hitProperties;
        if (currentMeleeWeaponRA != null)
            currentMeleeWeaponRA.hitProperties = hitProperties;
        if (rightArmHitbox != null)
            rightArmHitbox.hitProperties = hitProperties;
        if (rightLegHitbox != null)
            rightLegHitbox.hitProperties = hitProperties;
        if (leftArmHitbox != null)
            leftArmHitbox.hitProperties = hitProperties;
        if (leftLegHitbox != null)
            leftLegHitbox.hitProperties = hitProperties;
    }

    public CombatID GetCombatID()
    {
        CombatID combatID = new CombatID();

        // RIGHT HAND EQUIPPED AND LEFT HAND EMPTY
        if (currentMeleeWeaponRA != null && currentMeleeWeaponLA == null)
        {
            // SET MOVESET AND ATK ID FROM THE RIGHT WEAPON
            if (checkWeaponType(currentMeleeWeaponRA, vMeleeWeapon.MeleeType.Attack))
            {
                combatID.moveSet = currentMeleeWeaponRA.MoveSet_ID;
                combatID.atk = currentMeleeWeaponRA.ATK_ID;
            }
            // SET DEF ID FROM RIGHT WEAPON
            if (checkWeaponType(currentMeleeWeaponRA, vMeleeWeapon.MeleeType.Defense))
                combatID.def = currentMeleeWeaponRA.DEF_ID;

            return combatID;
        }

        // LEFT HAND EQUIPPED AND RIGHT HAND EMPTY
        if (currentMeleeWeaponLA != null && currentMeleeWeaponRA == null)
        {
            // SET DEF ID FROM LEFT WEAPON
            if (checkWeaponType(currentMeleeWeaponLA, vMeleeWeapon.MeleeType.Defense))
            {
                combatID.def = currentMeleeWeaponLA.DEF_ID;
                combatID.mirror = currentMeleeWeaponLA.mirrorAnimation;
            }

            // SET ID FROM DEFAULT HITBOX
            combatID.moveSet = rightArmHitbox.MoveSet_ID;
            combatID.atk = rightArmHitbox.MoveSet_ID;
            return combatID;
        }

        // BOTH HANDS EQUIPPED
        if (currentMeleeWeaponRA != null && currentMeleeWeaponLA != null)
        {
            // ATK ID FROM RIGHT HAND
            if (checkWeaponType(currentMeleeWeaponRA, vMeleeWeapon.MeleeType.Attack))
                combatID.atk = currentMeleeWeaponRA.ATK_ID;
            // DEF ID FROM LEFT HAND
            if (checkWeaponType(currentMeleeWeaponLA, vMeleeWeapon.MeleeType.Defense))
            {
                combatID.def = currentMeleeWeaponLA.DEF_ID;
                combatID.mirror = currentMeleeWeaponLA.mirrorAnimation;
            }
            // MOVE SET ID FROM RIGHT HAND
            combatID.moveSet = currentMeleeWeaponRA.MoveSet_ID;
            return combatID;
        }

        // BOTH HANDS EMPTY WITH DEFAULT HITBOX ENABLE
        if (currentMeleeWeaponRA == null && currentMeleeWeaponLA == null && useDefaultHitbox)
        {
            combatID.moveSet = rightArmHitbox.MoveSet_ID;
            combatID.def = leftArmHitbox.DEF_ID;
            combatID.atk = rightArmHitbox.MoveSet_ID;
            combatID.mirror = false;
            return combatID;
        }

        return combatID;
    }
    
    public vMeleeWeapon CurrentMeleeDefense()
    {
        if (currentMeleeWeaponLA != null && checkWeaponType(currentMeleeWeaponLA, vMeleeWeapon.MeleeType.Defense))
            return currentMeleeWeaponLA;
        if (currentMeleeWeaponRA != null && checkWeaponType(currentMeleeWeaponRA, vMeleeWeapon.MeleeType.Defense))
            return currentMeleeWeaponRA;
        if (leftArmHitbox != null && checkWeaponType(leftArmHitbox, vMeleeWeapon.MeleeType.Defense))
            return leftArmHitbox;
        if (rightArmHitbox != null && checkWeaponType(rightArmHitbox, vMeleeWeapon.MeleeType.Defense))
            return rightArmHitbox;
  
        return null;
    }

    bool checkWeaponType(vMeleeWeapon m_weapon, vMeleeWeapon.MeleeType type)
    {
        return m_weapon.meleeType == vMeleeWeapon.MeleeType.All || m_weapon.meleeType == type;
    }

    public vMeleeWeapon CurrentMeleeAttack(HitboxFrom hitboxFrom = HitboxFrom.RightArm)
    {
        switch (hitboxFrom)
        {
            case HitboxFrom.LeftArm:
                if (currentMeleeWeaponLA != null && (currentMeleeWeaponLA.meleeType == vMeleeWeapon.MeleeType.Attack || currentMeleeWeaponLA.meleeType == vMeleeWeapon.MeleeType.All))
                    return currentMeleeWeaponLA;
                if (leftArmHitbox != null && useDefaultHitbox)
                    return leftArmHitbox;
                break;
            case HitboxFrom.RightArm:
                if (currentMeleeWeaponRA != null && (currentMeleeWeaponRA.meleeType == vMeleeWeapon.MeleeType.Attack || currentMeleeWeaponRA.meleeType == vMeleeWeapon.MeleeType.All))
                    return currentMeleeWeaponRA;
                if (rightArmHitbox != null && useDefaultHitbox)
                    return rightArmHitbox;
                break;
            case HitboxFrom.BothArms:
                if (currentMeleeWeaponRA != null && (currentMeleeWeaponRA.meleeType == vMeleeWeapon.MeleeType.Attack || currentMeleeWeaponRA.meleeType == vMeleeWeapon.MeleeType.All))
                    return currentMeleeWeaponRA;
                if (rightArmHitbox != null && useDefaultHitbox)
                    return rightArmHitbox;
                break;
            case HitboxFrom.LeftLeg:
                if (leftLegHitbox != null)
                    return leftLegHitbox;
                break;
            case HitboxFrom.RightLeg:
                if (rightLegHitbox != null && useDefaultHitbox)
                    return rightLegHitbox;
                break;
            case HitboxFrom.BothLegs:
                if (rightLegHitbox != null && useDefaultHitbox)
                    return rightLegHitbox;
                break;
        }
        return null;
    }

    public void EnableDamage(AttackObject attackObject)
    {
        if (CurrentMeleeAttack(attackObject.hitboxFrom)==null) return;

        currentHitboxFrom = attackObject.hitboxFrom;

        applyDamage = attackObject.value;
        if (!applyDamage)
        {
            ClearHitColliders();
        }
        else
        {
            currentHitboxFrom = attackObject.hitboxFrom;
            attackName = attackObject.attackName;
        }
        var meleeWeapon = CurrentMeleeAttack(attackObject.hitboxFrom);
        //inAttack = attackObject.value;
        meleeWeapon.SetActiveWeapon(attackObject.value);
        meleeWeapon.damage.recoil_id = attackObject.recoil_ID;
        attackName = attackObject.attackName;
    }

    public void ClearHitColliders()
    {
        hitTopColliders.Clear();
        hitCenterColliders.Clear();
        hitBottomColliders.Clear();
    }

    public void OnDamageHit(vHitBox.HitInfo hitInfo)
    {
        var damage = new Damage(CurrentMeleeAttack(currentHitboxFrom).damage);
        damage.hitPosition = hitInfo.hitPoint;
        switch (hitInfo.hitBarPoint)
        {
            case HitBarPoints.Top:
                damage.value = (int)((CurrentMeleeAttack(currentHitboxFrom).damagePercentage.Top * (CurrentMeleeAttack(currentHitboxFrom).damage.value)) / 100);
                TryApplyDamage(hitInfo.hitCollider, damage, HitBarPoints.Top);
                break;
            case HitBarPoints.Center:
                damage.value = (int)((CurrentMeleeAttack(currentHitboxFrom).damagePercentage.Center * (CurrentMeleeAttack(currentHitboxFrom).damage.value)) / 100);
                TryApplyDamage(hitInfo.hitCollider, damage, HitBarPoints.Center);
                break;
        }
    }
	
	public void PlayHitSound()
	{
		CurrentMeleeAttack(currentHitboxFrom).PlayHitSound();
	}

    private void TryApplyDamage(Collider other, Damage damage, HitBarPoints hitBarPoint)
    {
        damage.sender = transform;
        damage.attackName = attackName;

        switch (hitBarPoint)
        {
            case HitBarPoints.Top:
                if (!hitTopColliders.Contains(other))
                {
                    other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                    hitTopColliders.Add(other);
                }
                break;
            case HitBarPoints.Center:
                if (!hitCenterColliders.Contains(other))
                {
                    other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                    hitCenterColliders.Add(other);
                }
                break;
        }
    }

    public void OnRecoilHit(vHitBox.HitInfo hitInfo)
    {
        if (!hitProperties.useRecoil || !applyDamage) return;

		if (InRecoilRange(hitInfo.hitPoint))
        {            
            // Check your animation state on the substate machine HitRecoil, this method will trigger the Recoil_ID 0 to trigger the animation HitWall
	        SendMessage("TriggerRecoil", 0, SendMessageOptions.DontRequireReceiver);
            // trigger recoil sound
	        CurrentMeleeAttack().PlayRecoilSound();
            // instantiate recoil particle 
            var hitrotation = Quaternion.LookRotation(new Vector3(transform.position.x, hitInfo.hitPoint.y, transform.position.z) - hitInfo.hitPoint);
            CurrentMeleeAttack().InstantiateParticle(hitInfo.hitPoint, hitrotation);

            ClearHitColliders();
        }
    }

	public void ResetAttack()
	{
		var meleeWeapon = CurrentMeleeAttack(currentHitboxFrom);
		//inAttack = attackObject.value;
		if(meleeWeapon!=null && meleeWeapon.isActive)
        {
            applyDamage = false;
            meleeWeapon.SetActiveWeapon(false);
        }
			
	}

	public bool InRecoilRange(Vector3 point)
	{
		var localTarget = transform.InverseTransformPoint(point);
		var angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
		if (angle <= hitProperties.recoilRange && angle >= -hitProperties.recoilRange) return true;
		return false;
	}

    public void SetRightMeleeWeapon(Transform handler)
    {
        if (handler)
        {
            var meleeWeapon = handler.GetComponentInChildren<vMeleeWeapon>();

            if (meleeWeapon)
            {
                if (meleeWeapon.useTwoHand)
                    DropLeftWeapon();
                currentMeleeWeaponRA = meleeWeapon;
                currentMeleeWeaponRA.Init();
                currentMeleeWeaponRA.hitProperties = this.hitProperties;
                SendMessage("SetCombatID", GetCombatID(), SendMessageOptions.DontRequireReceiver);
            }
            changeWeapon = false;
        }
    }

    public void SetRightWeaponHandler(vCollectableMelee weapon)
    {
        if (!changeWeapon)
        {
            changeWeapon = true;
            Transform handler = null;

            if (rightArmHandler != null && weapon != null)
                handler = rightArmHandler.Find(weapon.handler);
            if (handler == null && rightHandHandler != null && weapon != null)
                handler = rightHandHandler.Find(weapon.handler);

            if (weapon == null)
            {
                changeWeapon = false;
                return;
            }
            if (handler)
            {
                DropRightWeapon();
                weapon.transform.position = handler.position;
                weapon.transform.rotation = handler.rotation;
                weapon.transform.parent = handler;
                weapon.EnableMeleeItem();
                SetRightMeleeWeapon(handler);
            }
            else
            {
                changeWeapon = false;
                Debug.LogWarning("Missing " + weapon.name + " handler, please create and assign one at the MeleeWeaponManager");
            }
        }
    }

    public void SetLeftMeleeWeapon(Transform handler)
    {
        if (handler)
        {
            var meleeWeapon = handler.GetComponentInChildren<vMeleeWeapon>();

            if (meleeWeapon)
            {
                if (currentMeleeWeaponRA != null && currentMeleeWeaponRA.useTwoHand)
                    DropRightWeapon();
                currentMeleeWeaponLA = meleeWeapon;
                currentMeleeWeaponLA.Init();
                currentMeleeWeaponLA.hitProperties = this.hitProperties;
                SendMessage("SetCombatID", GetCombatID(), SendMessageOptions.DontRequireReceiver);
            }
            changeWeapon = false;
        }
    }

    public void SetLeftWeaponHandler(vCollectableMelee weapon)
    {
        if (!changeWeapon)
        {
            changeWeapon = true;
            Transform handler = null;

            if (leftArmHandler != null && weapon != null)
                handler = leftArmHandler.Find(weapon.handler);
            if (handler == null && leftHandHandler != null && weapon != null)
                handler = leftHandHandler.Find(weapon.handler);

            if (weapon == null)
            {
                changeWeapon = false;
                return;
            }
            if (handler)
            {
                DropLeftWeapon();
                weapon.transform.position = handler.position;
                weapon.transform.rotation = handler.rotation;
                weapon.transform.parent = handler;
                weapon.EnableMeleeItem();
                SetLeftMeleeWeapon(handler);
            }
            else
            {
                changeWeapon = false;
                Debug.LogWarning("Missing " + weapon.name + " handler, please create and assign one at the MeleeWeaponManager");
            }
        }
    }

    public void DropRightWeapon()
    {
        if (currentMeleeWeaponRA != null)
        {
            var collectable = currentMeleeWeaponRA.GetComponentInParent<vCollectableMelee>();
            if (collectable != null)
            {
                collectable.transform.parent = null;
                collectable.DisableMeleeItem();
                if (collectable.destroyOnDrop)
                    Destroy(collectable.gameObject);
                currentMeleeWeaponRA = null;
                SendMessage("SetCombatID", GetCombatID(), SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void DropLeftWeapon()
    {
        if (currentMeleeWeaponLA != null)
        {
            var collectable = currentMeleeWeaponLA.GetComponentInParent<vCollectableMelee>();
            if (collectable != null)
            {
                collectable.transform.parent = null;
                collectable.DisableMeleeItem();
                if (collectable.destroyOnDrop)
                    Destroy(collectable.gameObject);
                currentMeleeWeaponLA = null;
                SendMessage("SetCombatID", GetCombatID(), SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}

[System.Serializable]
public class HitProperties
{
    [Tooltip("Tag to receiver Damage")]
    public List<string> hitDamageTags = new List<string>() { "Enemy" };
    [Tooltip("Trigger a HitRecoil animation if the character attacks a obstacle")]
    public bool useRecoil = true;    
    public bool drawRecoilGizmos;
	[Range(0,180f)]
	public float recoilRange = 90f;
    [Tooltip("layer to Recoil Damage")]
    public LayerMask hitRecoilLayer = 1 << 0;    
}

[System.Serializable]
public class HitEffect
{
    public string hitName = "";
    public GameObject hitPrefab;
}

public class AttackObject
{
    public HitboxFrom hitboxFrom;
    public int recoil_ID;
    public bool value;
    public string attackName;
    public AttackObject(HitboxFrom hitboxFrom, int recoil_ID, bool value, string attackName = "")
    {
        this.hitboxFrom = hitboxFrom;
        this.recoil_ID = recoil_ID;
        this.value = value;
        this.attackName = attackName;
    }
}

public class CombatID
{
    public int moveSet;
    public int atk;
    public int def;
    public bool mirror;
}

public enum HitboxFrom
{
    LeftArm,
    RightArm,
    BothArms,
    LeftLeg,
    RightLeg,
    BothLegs
}