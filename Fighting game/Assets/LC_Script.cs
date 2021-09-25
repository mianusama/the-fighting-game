using UnityEngine;
using System.Collections;

public class LC_Script : MonoBehaviour {

	public static bool LC;

	// Use this for initialization
	void Start () {
		LC = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	void OnTriggerEnter(Collider col)
	{
		if (col.tag == "Player") {
			LC = true;
		}
	}
}
