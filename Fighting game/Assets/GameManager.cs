using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
	public GameObject PauseDialog, GameOverDialog, LevelComplete, GamePlay;
	public static bool GameOver= false, LC = false;
	public GameObject[] Enemies, Levels;
	public Transform[] SpawnPoint;

	public GameObject Player;
	// Use this for initialization
	void Start () {
		if (!PlayerPrefs.HasKey ("Level"))
			PlayerPrefs.SetInt ("Level",1);


		Time.timeScale = 1;
		GameOver = false;

		Player.transform.position = SpawnPoint [PlayerPrefs.GetInt ("Level")-1].position;
		for(int i=0; i<=2; i++)
		{


			if(i==PlayerPrefs.GetInt ("Level")-1)
			{
				Levels [i].SetActive (true);
				Debug.Log(i);
			}
			else
				Levels[i].SetActive(false);


		}
	}
	
	// Update is called once per frame
	void Update () {
	
		Enemies = GameObject.FindGameObjectsWithTag ("Enemy");

		if (GameOver) {
			Time.timeScale=0;
			GameOverDialog.SetActive(true);
			GamePlay.SetActive (false);
		}

		if (Enemies.Length == 0 && LC_Script.LC) {
			StartCoroutine(LComplete());




		}
			

	}

	public void BtnPause()
	{
		Time.timeScale = 0;
		PauseDialog.SetActive (true);
	}

	public void BtnRestart()
	{
		Application.LoadLevel (Application.loadedLevel);
	}

	public void BtnMenu()
	{
		Application.LoadLevel (0);
	}

	public void BtnResume()
	{
		Time.timeScale = 1;
		PauseDialog.SetActive (false);
	}

	public void Next()
	{
		PlayerPrefs.SetInt ("Level", PlayerPrefs.GetInt ("Level") + 1);
		Application.LoadLevel (Application.loadedLevel);
	}


	IEnumerator LComplete()
	{
		if (!LC) {
			PlayerPrefs.SetInt ("LevelCompleted", PlayerPrefs.GetInt ("Level"));
			Debug.Log ("LC = " + PlayerPrefs.GetInt ("LevelCompleted"));
		}
		LC = true;
		yield return new WaitForSeconds(1);
		GamePlay.SetActive (false);
		Time.timeScale = 0;
		LevelComplete.SetActive(true);
	}


}
