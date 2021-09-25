using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {

	public float TargetDistance;
	public float EnemyLookDistance;
	public float AttackDistance;
	public float EnemyMovementSpeed;
	public float damping;
	public float Cooldown;
	public float Timer;
	public Transform Target;
	Rigidbody theRigidBody;
	UnityEngine.AI.NavMeshAgent agent;

	public GirlDamage RFist;
	public GirlDamage LFist;
	public GirlDamage RFoot;
	public GirlDamage LFoot;

	public float Health = 100f;
	Animator anim;
	bool isAlive=true;
	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();
		theRigidBody = GetComponent<Rigidbody> ();
		agent = GetComponent<UnityEngine.AI.NavMeshAgent> ();
	}


	void FixedUpdate()
	{
		if (isAlive) {
			TargetDistance = Vector3.Distance (Target.position, transform.position);
			if (TargetDistance < EnemyLookDistance && TargetDistance > AttackDistance) {
				anim.SetBool ("Walk", true);
				Vector3 Direction2Player = (Target.position - transform.position).normalized;
				agent.SetDestination (Target.position);
				Debug.Log ("Looking at the player");
			}

			if (TargetDistance < AttackDistance) {
				anim.SetBool ("Walk", false);
				Attack ();
			}
		}
	}


	void Attack(){
		if (Target.GetComponent<GirlController> ().Health > 0) {
			if (Time.time >= Timer) {
				int AttackNum = Random.Range (1, 4);
				if (AttackNum == 1) {
					anim.SetTrigger ("Punch");
				} else if (AttackNum == 2) {
					anim.SetTrigger ("Punch_Combo");
				} else if (AttackNum == 3) {
					anim.SetTrigger ("Kick");
				}
				Timer = Time.time + Cooldown;
			}
		}
	}

	// THIS GETS CALLED BY ANIMATION EVENTS, 
	public void Damage(string HitCollider){
		switch(HitCollider){
		case "RFist": // IF WE'RE PUNCHING WITH R FIST
			if (RFist.Intersecting) Target.GetComponent<GirlController> ().GotDamage (10); // CHECK IF WE HIT PLAYER AND DAMAGE HIM
			break;

		case "LFist":
			if (LFist.Intersecting) Target.GetComponent<GirlController> ().GotDamage (10);
			break;

		case "RFoot":
			if (RFoot.Intersecting) Target.GetComponent<GirlController> ().GotDamage (10);
			break;

		case "LFoot":
			if (LFoot.Intersecting) Target.GetComponent<GirlController> ().GotDamage (10);
			break;
		}		
	}
	void lookAtPlayer()
	{
		Quaternion rot = Quaternion.LookRotation (Target.position - transform.position);
		transform.rotation = Quaternion.Slerp (transform.rotation, rot, Time.deltaTime*damping);
	}

	// Update is called once per frame
	void Update () {
	
		if (Health < 0) {
			isAlive = false;
			anim.SetBool ("Die", true);
			Destroy (this.gameObject, 7);
		}
	}

	void GotDamage(float dmg)
	{
		Health -= dmg;
		anim.SetBool ("Hit",true);
		StartCoroutine (Wait());
	}

//
//	public void KickDown()
//	{
//		anim.SetBool ("Kick",true);
//	}
//
//	public void KickUp()
//	{
//		anim.SetBool ("Kick",false);
//	}
//
//	public void PunchDown()
//	{
//		anim.SetBool ("Punch",true);
//	} 
//	 
//	public void PunchUp()
//	{
//		anim.SetBool ("Punch",false);
//	} 
//
//	public void Punch_ComboDown()
//	{
//		anim.SetBool ("Punch_Combo",true);
//	}
//
//	public void Punch_ComboUp()
//	{
//		anim.SetBool ("Punch_Combo",false);
//	}
//
//	IEnumerator Wait()
//	{
//		yield return new WaitForSeconds (3);
//		anim.SetBool ("Die",false);
//	}


	IEnumerator Wait()
	{
		yield return new WaitForSeconds (2);
		anim.SetBool ("Hit",false);
	}
}
