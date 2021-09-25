using UnityEngine;
using System.Collections;

public class GirlDamage : MonoBehaviour {

	public bool isPlayer;
	public bool Intersecting;
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider other)
	{
		if (isPlayer) {
			if (other.tag == "Enemy") {
				Intersecting = true;
			}
		} else {
			if (other.tag == "Player") {
				Intersecting = true;
			}
		}
	}
	void OnTriggerExit(Collider other)
	{
		if (isPlayer) {
			if (other.tag == "Enemy") {
				Intersecting = false;
			}
		} else {
			if (other.tag == "Player") {
				Intersecting = false;
			}
		}
	}
}
