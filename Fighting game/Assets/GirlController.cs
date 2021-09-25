using UnityEngine;
using System.Collections;

public class GirlController : MonoBehaviour {

	Animator Anim;
	public float Health = 100;



	// Use this for initialization
	void Start () {
		Anim = GetComponent<Animator> ();
	}
	
	// Update is called once per frame
	void Update () {
	
		if (Health < 0){
			Anim.SetBool ("Dead",true);
		}
	}

	public void GotDamage(float dmg)
	{
		Health -= dmg;
		Anim.SetBool ("Hit",true);
		StartCoroutine (Wait());
	}

	IEnumerator Wait()
	{
		yield return new WaitForSeconds (2);
		Anim.SetBool ("Hit",false);
	}

	public void Restart()
	{
		Application.LoadLevel (Application.loadedLevel);
	}

	public void Run()
	{
		Anim.SetBool ("Run",true);
	}

	public void Run1Down()
	{
		Anim.SetBool ("Run1",true);
	}

	public void Run1Up()
	{
		Anim.SetBool ("Run1",false);
	}

	public void PunchDown()
	{
		Anim.SetBool ("Punch",true);
	}

	public void PunchUp()
	{
		Anim.SetBool ("Punch",false);
	}


	public void KickDown()
	{
		Anim.SetBool ("Kick",true);
	}

	public void KickUp()
	{
		Anim.SetBool ("Kick",false);
	}

	public void WalkDown()
	{
		Anim.SetBool ("Walk",true);
	}

	public void WalkUp()
	{
		Anim.SetBool ("Walk",false);
	}


}
