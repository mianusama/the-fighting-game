using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour {


	public AudioClip[] BG;
	int ct =0;
	bool soundPlaying = false;
	float CurrentLength;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(ct<BG.Length)
		{
			GetComponent<AudioSource>().clip = BG[ct];

			if (!soundPlaying) {
				GetComponent<AudioSource> ().Play ();
				soundPlaying = true;
				CurrentLength = GetComponent<AudioSource> ().clip.length;
			}

			if(GetComponent<AudioSource>().time+0.5f >= CurrentLength)
			{
			ct++;
			soundPlaying = false;
			}
		}
		if (ct == BG.Length) {
			ct = 0;

		}

	}
}
