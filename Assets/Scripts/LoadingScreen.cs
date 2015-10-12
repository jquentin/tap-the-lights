using UnityEngine;
using System.Collections;

public class LoadingScreen : MonoBehaviour {
	
	private static LoadingScreen _instance;
	public static LoadingScreen instance
	{
		get
		{
			if (_instance == null)
				_instance = FindObjectOfType<LoadingScreen>();
			return _instance;
		}
	}

	public void Show()
	{
		gameObject.SetActive(true);
	}
	
	public void Hide()
	{
		gameObject.SetActive(false);
	}
}
