using UnityEngine;
using System.Collections;

public class ArrowPointer : MonoBehaviour {

	public Transform finishPoint;
	private UnityEngine.AI.NavMeshPath path;
	public UnityEngine.AI.NavMeshAgent Agent;
	private Vector3 temp;

	// Use this for initialization
	IEnumerator Start () {
		yield return new WaitForSeconds (1);
		GetComponent<UnityEngine.AI.NavMeshAgent> ().enabled = true;
	}
	
	// Update is called once per frame
	void Update () {

		if(GetComponent<UnityEngine.AI.NavMeshAgent> ().enabled == true)
		{
			path = new UnityEngine.AI.NavMeshPath ();
			Agent.CalculatePath (finishPoint.transform.position, path);
			temp = new Vector3 (path.corners [1].x, transform.position.y, path.corners [1].z);
			transform.LookAt (temp);
		}
	}


	void LateUpdate()
	{
		if (!finishPoint)
			finishPoint = GameObject.FindGameObjectWithTag ("Finish").GetComponent<Transform>();
	}
}
