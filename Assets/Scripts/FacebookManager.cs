using UnityEngine;
using System.Collections;

public class FacebookManager : MonoBehaviour {

	private static FacebookManager _instance;
	public static FacebookManager instance
	{
		get
		{
			if (_instance == null)
				_instance = FindObjectOfType<FacebookManager>();
			return _instance;
		}
	}

	void Awake()
	{
		SetInit();
	}
	
	public void PostFacebook(int highScore)
	{
		if (!FB.IsLoggedIn)
		{
			FB.Login("email,publish_actions");
		}
		else
		{
			Debug.Log("onShareClicked");
			FB.Feed(
				linkCaption: "Check out Tap the Lights on iOS and Android!",
				picture: "https://www.jeremyquentin.fr/TapTheLights/data/FBShareImage.png",
				linkName: "I reached " + highScore.ToString() + " points! Can you beat it?",
				link: "https://www.jeremyquentin.fr/TapTheLights/FB"
				);
		}
	}
	
	void OnLoggedIn()
	{
		Debug.Log("Logged in. ID: " + FB.UserId);
	}
	
	private void SetInit()
	{
		FB.Init(null);
		Debug.Log("SetInit");
		if (FB.IsLoggedIn)
		{
			Debug.Log("Already logged in");
			OnLoggedIn();
		}
	}
}
