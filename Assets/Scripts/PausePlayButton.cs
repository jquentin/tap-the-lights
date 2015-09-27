using UnityEngine;
using System.Collections;

public class PausePlayButton : MonoBehaviour {

	public GameObject play;
	public GameObject pause;

	void Start()
	{
		Play ();
	}

	public void Play () 
	{
		pause.SetActive(true);
		play.SetActive(false);
	}
	
	public void Pause () 
	{
		pause.SetActive(false);
		play.SetActive(true);
	}

}
