using UnityEngine;
using System.Collections;

public class BackgroundColor : MonoBehaviour {

	public Camera camera;
	private UITexture texture;

	// Use this for initialization
	void Start () {
		texture = GetComponent<UITexture>();
		camera = NGUITools.FindInParents<Camera>(gameObject);
	}
	
	// Update is called once per frame
	void Update () {
		texture.color = camera.backgroundColor;
	}
}
