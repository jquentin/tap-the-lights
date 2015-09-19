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
				linkCaption: "I reached " + highScore.ToString() + " points! Can you beat it?",
//				picture: "http://a5.mzstatic.com/us/r30/Purple5/v4/a0/ae/53/a0ae53ae-248b-b38c-04f1-60be74ebd360/screen568x568.jpeg",
				linkName: "Check out Tap the Lights on iOS and Android!",
				link: "https://itunes.apple.com/app/apple-store/id972420360?pt=94349800&ct=InAppFacebookPosts&mt=8"
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
