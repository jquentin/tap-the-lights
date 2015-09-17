using UnityEngine;
using System.Collections;

public class Pad : MonoBehaviour {

	public GameObject fireObject;
	public Texture fireTexture;
	public AudioClip unfireSound;
	public ColorPalette colorPalette;
	public int index { private get; set; }
	private AudioSource _source;
	private AudioSource source
	{
		get
		{
			if (_source == null)
				_source = GetComponent<AudioSource>();
			if (_source == null)
				_source = gameObject.AddComponent<AudioSource>();
			return _source;
		}
	}

	private UITexture _texture;
	private UITexture texture
	{
		get
		{
			if (_texture == null)
				_texture = GetComponent<UITexture>();
			return _texture;
		}
	}

	private bool fired = false;

	public delegate void OnUnfireHandler(int index);
	public OnUnfireHandler OnUnfire; 

	
	public delegate void OnDieHandler();
	public OnDieHandler OnDie; 

	void Start()
	{
	}

	public void Fire () 
	{
		fireObject.SetActive(true);
		fired = true;
		Invoke ("TooLate", PadController.instance.delay / PadController.instance.speedMultiplier);
	}

	void TooLate()
	{
		if (PadController.instance.score == 0)
			PadController.instance.PauseGame();
		else
			Die ();
	}

	void Shake()
	{
		iTween.ShakePosition(gameObject, iTween.Hash(
			"amount", Vector3.one * 0.07f,
			"time", 0.5f));
	}
	
	public void UnFire()
	{
		if (fired)
		{
			CancelInvoke("TooLate");
			fireObject.SetActive(false);
			source.PlayOneShot(unfireSound);
			if (OnUnfire != null)
				OnUnfire(index);
			fired = false;
		}
		else
		{
			Die ();
		}
	}

	void Die()
	{
		Shake();
		PadController.instance.deadColor = texture.color;
		if (OnDie != null)
			OnDie();
	}

	public void Init()
	{
		CancelInvoke();
		iTween.Stop(gameObject);
		fireObject.SetActive(false);
		fired = false;
		texture.color = colorPalette.GetRandomColor();
		PlanNextColorChange();
		SetBelowOverlay();
	}

	void PlanNextColorChange()
	{
		Invoke("ChangeColor", UnityEngine.Random.Range(2f, 6f));
	}

	void ChangeColor()
	{
		iTween.ValueTo(gameObject, iTween.Hash(
			"from", texture.color,
			"to", colorPalette.GetRandomColor(),
			"time", UnityEngine.Random.Range(2f, 6f),
			"onupdate", "UpdateColor",
			"oncomplete", "PlanNextColorChange"));
	}

	void UpdateColor(Color color)
	{
		texture.color = color;
	}

	void OnPress(bool isPressed)
	{
		if (isPressed)
			UnFire();
	}

	public void SetAboveOverlay()
	{
		texture.depth = 3;
		fireObject.GetComponent<UITexture>().depth = 4;
	}
	
	public void SetBelowOverlay()
	{
		texture.depth = 0;
		fireObject.GetComponent<UITexture>().depth = 1;
	}

	[ContextMenu("Initialize")]
	void Initialize()
	{
		for (int i = transform.childCount - 1 ; i >= 0 ; i--)
			DestroyImmediate(transform.GetChild(i).gameObject);
		UITexture texture = NGUITools.AddChild<UITexture>(gameObject);
		texture.mainTexture = fireTexture;
		texture.SetAnchor(transform);
		texture.bottomAnchor.relative = 0f;
		texture.topAnchor.relative = 1f;
		texture.leftAnchor.relative = 0f;
		texture.rightAnchor.relative = 1f;
		texture.depth = GetComponent<UIWidget>().depth + 1;
		fireObject = texture.gameObject;
		gameObject.AddComponent<BoxCollider>();
		GetComponent<UIWidget>().autoResizeBoxCollider = true;
	}

}
