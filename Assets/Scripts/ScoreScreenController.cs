using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScoreScreenController : MonoBehaviour {

	private static ScoreScreenController _instance;
	public static ScoreScreenController instance
	{
		get
		{
			if (_instance == null)
				_instance = FindObjectOfType<ScoreScreenController>();
			return _instance;
		}
	}


	public UIWidget scoreScreenWidget;
	public UILabel scoreTitleLabel;
	public UILabel scoreLabel;
	public UILabel bestScoreTitleLabel;
	public UILabel bestScoreLabel;
	public float speedMultiplier = 4f;
	public float transitionTime = 0.3f;

	public GameObject skipButton;
	public GameObject restartButton;

	private int currentScore = 0;
	private int finalScore;
	private List<SavedNote> savedNotes;
	
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

	private Camera _camera;
	private Camera camera
	{
		get
		{
			if (_camera == null)
				_camera = NGUITools.FindCameraForLayer(gameObject.layer);
			return _camera;
		}
	}

	void Start()
	{
		UpdateViewport(0f);
//		camera.rect = new Rect(0f, -1f, 0f, 1f);
	}

	void UpdateViewport(float height)
	{
		scoreScreenWidget.topAnchor.relative = height;
		scoreScreenWidget.bottomAnchor.relative = -1f + height;
//		camera.rect = new Rect(0f, -1f + height, 1f, 1f);
	}

	public void Play (int score, List<SavedNote> savedNotes, Color losingColor, Color bgColor) 
	{
		
		scoreTitleLabel.color = losingColor;
		scoreLabel.color = losingColor;
		bestScoreTitleLabel.color = losingColor;
		bestScoreLabel.color = losingColor;

		camera.backgroundColor = bgColor;
		
		this.finalScore = score;
		this.savedNotes = savedNotes;
		
		iTween.ValueTo(gameObject, iTween.Hash(
			"from", 0f,
			"to", 1f,
			"time", transitionTime,
			"onupdate", "UpdateViewport"));

		Launch(0.5f);
	}

	void Launch(float delay = 0f)
	{
//		float[] data = new float[1024];
//		AudioListener.GetOutputData(data, 4);
		currentScore = 0;
		UpdateScoreLabel();
		foreach(SavedNote note in savedNotes)
			Invoke("PlayOne", note.time / speedMultiplier + delay);
	}

	void PlayOne()
	{
		SavedNote note = savedNotes[currentScore];
		source.PlayOneShot(note.clip);
		currentScore++;
		UpdateScoreLabel();
	}

	void UpdateScoreLabel()
	{
		scoreLabel.text = currentScore.ToString();
		skipButton.SetActive(currentScore < finalScore);
		restartButton.SetActive(currentScore == finalScore);
	}

	public void BackToGame()
	{
		iTween.ValueTo(gameObject, iTween.Hash(
			"from", 1f,
			"to", 0f,
			"time", transitionTime,
			"onupdate", "UpdateViewport"));
		PadController.instance.Init ();
	}

	public void Skip()
	{
		CancelInvoke();
		currentScore = finalScore;
		UpdateScoreLabel();
	}

	public void Replay()
	{
		Launch();
	}

	public void ShareOnFacebook()
	{
		FacebookManager.instance.PostFacebook(finalScore);
	}
	
	public void ShowGameCenter()
	{
		Social.ShowLeaderboardUI();
	}

}
