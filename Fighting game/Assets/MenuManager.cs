using UnityEngine;
using System.Collections;

public class MenuManager : MonoBehaviour {

	public GameObject Menu, Exit, Title, Girl, Levels, PlayerCanvas, GirlPlayer;

	public GameObject[] Lock;

	// Use this for initialization
	void Start () {

		if (!PlayerPrefs.HasKey ("LevelCompleted")) {
			PlayerPrefs.SetInt("LevelCompleted",0);
		}

//		PlayerPrefs.SetInt ("LevelCompleted",0);
		Time.timeScale = 1;
		for(int i=0; i<=PlayerPrefs.GetInt("LevelCompleted");i++)
		{
			Lock[i].SetActive(false);
			Debug.Log(PlayerPrefs.GetInt("LevelCompleted"));
		}
	}
	
	// Update is called once per frame
	void Update () {
//		PlayerPrefs.DeleteAll ();
	}


	public void Btn_Play()
	{
		Menu.SetActive (false);
		Levels.SetActive (true);
		PlayerCanvas.SetActive (false);
		GirlPlayer.SetActive (false);

	}
	public void Btn_Options()
	{

	} 

	public void Btn_Exit()
	{
		Exit.SetActive (true);
		Menu.SetActive (false);
		Girl.SetActive (false);
		Title.SetActive (false);
	}

	public void Btn_Quit()
	{
		Application.Quit ();
	}

	public void Cancel()
	{
		Exit.SetActive (false);
		Menu.SetActive (true);
		Girl.SetActive (true);
		Title.SetActive (true);
	}


	public void BtnLvl(int LevelNum)
	{
		if (LevelNum <= (PlayerPrefs.GetInt ("LevelCompleted")+1)) {
			PlayerPrefs.SetInt ("Level", LevelNum);
			Application.LoadLevel (1);
		}
	}
}
