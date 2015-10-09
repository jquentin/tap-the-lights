using UnityEngine;
using System.Collections;

public class SocialStateDependant : MonoBehaviour {

	public GameObject target;

	void Update () 
	{
//		Debug.Log (Social.localUser + " ---- " + Social.localUser.authenticated);
		if (target != null)
			target.SetActive(Social.localUser.authenticated);
	}
}
