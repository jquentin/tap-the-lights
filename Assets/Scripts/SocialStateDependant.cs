using UnityEngine;
using System.Collections;

public class SocialStateDependant : MonoBehaviour {

	void Update () 
	{
		gameObject.SetActive(Social.localUser.authenticated);
	}
}
