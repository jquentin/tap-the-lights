using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[Serializable]
public class Instrument
{
	public string name;
	public float volume = 1f;
	public List<AudioClip> notes;
}


[Serializable]
public class ColorPalette
{
	public string hex;
	public Color bgColor;
	public List<Color> padColors;
	public List<string> padHexs;
	public Color GetRandomColor()
	{
		return padColors[UnityEngine.Random.Range(0, padColors.Count)];
	}
	public void InitFromHex()
	{
		padColors = new List<Color>();
#if UNITY_5_2
		ColorUtility.TryParseHtmlString(hex, out bgColor);
#else
		Color.TryParseHexString(hex, out bgColor);
#endif
		for (int i= 0 ; i < padHexs.Count ; i++)
		{
			padColors.Add(new Color());
			Color padColor = padColors[i];
#if UNITY_5_2
			ColorUtility.TryParseHtmlString(padHexs[i], out padColor);
#else
			Color.TryParseHexString(padHexs[i], out padColor);
#endif
			padColors[i] = padColor;
		}
	}
}

[Serializable]
public class DifficultyLevel
{
	public int start;
	public int end;
	public float probDouble;
	public float probTriple;
	public float speedMultiplier;
}

public class SavedNote
{
	public float time;
	public AudioClip clip;
	public SavedNote(float time, AudioClip clip)
	{
		this.time = time;
		this.clip = clip;
	}
}

public class PadController : MonoBehaviour {

	
	private static PadController _instance;
	public static PadController instance
	{
		get
		{
			if (_instance == null)
				_instance = FindObjectOfType<PadController>();
			return _instance;
		}
	}
	
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
	
	private AudioListener _listener;
	public AudioListener listener
	{
		get
		{
			if (_listener == null)
				_listener = camera.GetComponent<AudioListener>();
			return _listener;
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

	public List<Pad> pads;
	public List<Instrument> instruments;
	public List<ColorPalette> palettes;
	public List<DifficultyLevel> levels;
	public UILabel scoreLabel;
	public UITexture overlay;

	public GameObject pausebutton;
	public GameObject playbutton;

	private List<SavedNote> savedNotes = new List<SavedNote>();

	public float delay = 2f;
	private float lastFire;

	private float probDouble = 0f;
	private float probTriple = 0f;
	public float speedMultiplier
	{
		get;
		private set;
	}

	private float initTime;

	[DllImport("__Internal")]
	private static extern void _ReportAchievement( string achievementID, float progress );

	private int _score = 0;
	public int score
	{
		get
		{
			return _score;
		}
		set
		{
			_score = value;
			string scoreAchievement = string.Empty;
			if (_score == 42)
				scoreAchievement = "42";
			else if (score == 99)
				scoreAchievement = "reach99";
			else if (score == 420)
				scoreAchievement = "420";

			if (!string.IsNullOrEmpty(scoreAchievement))
			{
				_ReportAchievement(scoreAchievement, 100);
			}
		}
	}

	private int instrumentIndex = -1;
	private int paletteIndex = -1;
	
	private int lastPadFired;

	private bool halfTime = false;

	private bool inTuto = false;

	void Start () 
	{

//		AudioSettings.SetDSPBufferSize(128, 2);
		print(Social.Active);
		Social.localUser.Authenticate(delegate(bool success) {
			if (success) {
				Debug.Log ("Authentication successful");
				string userInfo = "Username: " + Social.localUser.userName + 
					"\nUser ID: " + Social.localUser.id + 
						"\nIsUnderage: " + Social.localUser.underage + 
						"\nIsAuthenticated: " + Social.localUser.authenticated;
				Debug.Log (userInfo);
				Social.LoadAchievementDescriptions( delegate(UnityEngine.SocialPlatforms.IAchievementDescription[] obj) {
					if (obj == null)
						return;
					foreach(UnityEngine.SocialPlatforms.IAchievementDescription a in obj)
						Debug.Log(a.id);
				});
			}
			else
				Debug.Log ("Authentication failed");
		});
		Init();
	}

	public void Init()
	{
		this.enabled = true;
		inTuto = false;
		score = 0;
		lastPadFired = -1;
		halfTime = false;
		UpdateDifficulty();
		savedNotes.Clear();
		lastFire = Time.time;
		initTime = Time.time;
		instrumentIndex = RandomUtils.RangeExcluding(0, instruments.Count, instrumentIndex);
		paletteIndex = RandomUtils.RangeExcluding(0, palettes.Count, paletteIndex);
		for (int i = 0 ; i < 9 ; i++)
		{
			pads[i].index = i;
			pads[i].unfireSound = instruments[instrumentIndex].notes[i];
			pads[i].unfireSoundVolume = instruments[instrumentIndex].volume;
			pads[i].colorPalette = palettes[paletteIndex];
			pads[i].Init();
			pads[i].OnUnfire += OnTabUnfired;
			pads[i].OnDie += OnDie;
		}
		camera.backgroundColor = palettes[paletteIndex].bgColor;
		playbutton.SetActive(false);
		pausebutton.SetActive(false);
	}
	

	void Update () 
	{
		scoreLabel.text = score.ToString();
		if (lastFire + delay * (halfTime ? 0.5f : 1f) / speedMultiplier < Time.time)
		{
			Fire();
		}
	}

	void OnTabUnfired(int index)
	{
		pausebutton.SetActive(true);
		UntriggerTuto();
		score++;
		UpdateDifficulty();
		savedNotes.Add(new SavedNote(Time.time - initTime, instruments[instrumentIndex].notes[index]));
	}

	public Color deadColor;

	void OnDie()
	{
		for (int i = 0 ; i < 9 ; i++)
		{
			pads[i].OnUnfire -= OnTabUnfired;
			pads[i].OnDie -= OnDie;
		}
		Social.ReportScore(score, "Score", delegate(bool success) {
			Debug.Log("Reported score " + score + " to leaderboard: Score, success: " + success);
		});
//		if (Social.localUser.authenticated)
//		{
//
//		}
		StartCoroutine(DieCoroutine());
		playbutton.SetActive(false);
		pausebutton.SetActive(false);
		this.enabled = false;
	}

	IEnumerator DieCoroutine()
	{
		for ( int i = instruments[instrumentIndex].notes.Count - 1 ; i >= 0 ; i--)
		{
			source.PlayOneShot(instruments[instrumentIndex].notes[i]);
			yield return new WaitForSeconds(0.05f);
		}
		ShowScoreScreen();
	}

	void ShowScoreScreen()
	{
		ScoreScreenController.instance.Play(score, savedNotes, palettes[paletteIndex].padColors[0], palettes[paletteIndex].bgColor);
	}

	void UpdateDifficulty()
	{
		foreach (DifficultyLevel level in levels)
		{
			if (score >= level.start && score < level.end)
			{
				probDouble = level.probDouble;
				probTriple = level.probTriple;
				speedMultiplier = level.speedMultiplier;
				break;
			}
		}
	}

	void Fire()
	{
		float rand = UnityEngine.Random.Range(0f, 100f);
		bool fireTwo = false;
		bool fireThree = false;
		if (!halfTime)
		{
			if (rand < probTriple + probDouble)
				fireTwo = true;
			if (rand < probTriple)
				fireThree = true;
		}
		int padToFire = -1;
		if (lastPadFired > -1)
		{
			// Choose the second pad in all pads except the first one the two around
			padToFire = UnityEngine.Random.Range(0, pads.Count - 1);
			if (padToFire >= lastPadFired)
				padToFire++;
		}
		else
			padToFire = UnityEngine.Random.Range(0, pads.Count);
		pads[padToFire].Fire();
		int padToFire2 = 0;
		if (fireTwo)
		{
			// Choose the second pad in all pads except the first one  and the two around
			padToFire2 = UnityEngine.Random.Range(0, pads.Count - 3);
			if (padToFire2 >= padToFire - 1)
				padToFire2 += 3;
			pads[padToFire2].Fire();
			if (fireThree)
			{
				// Choose the third pad in all pads except the first one and the two around and the second and the two around
				int padToFire3 = UnityEngine.Random.Range(0, pads.Count - 6);
				if (padToFire3 >= Mathf.Min(padToFire, padToFire2) - 1)
					padToFire3 += 3;
				if (padToFire3 >= Mathf.Max(padToFire, padToFire2) - 1)
					padToFire3 += 3;
				pads[padToFire3].Fire();
			}
		}
		lastFire = Time.time;
		lastPadFired = padToFire;
		halfTime = (UnityEngine.Random.Range(0f, 1f) < 0.5f);
		if (score == 0)
		{
			TriggerTuto();
			pads[padToFire].SetAboveOverlay();
			if (fireTwo)
				pads[padToFire2].SetAboveOverlay();
		}
	}

	void TriggerTuto()
	{
		if (!inTuto)
		{
			inTuto = true;
			Time.timeScale = 0f;
			overlay.GetOrAddComponent<AlphaTweenable>().StopTween();
			overlay.GetOrAddComponent<AlphaTweenable>().UpdateAlpha(0f);
			overlay.GetOrAddComponent<AlphaTweenable>().TweenAlpha(0.6f, 8f, 0f, iTween.EaseType.linear);
		}
	}

	void UntriggerTuto()
	{
		if (inTuto)
		{
			overlay.GetOrAddComponent<AlphaTweenable>().StopTween();
			overlay.GetOrAddComponent<AlphaTweenable>().UpdateAlpha(0f);
			Time.timeScale = 1f;
			for (int i = 0 ; i < 9 ; i++)
			{
				pads[i].SetBelowOverlay();
			}
			inTuto = false;
		}
	}

	public void PauseGame()
	{
		CancelInvoke("SetBackTimeScale");
		Time.timeScale = 0f;
		overlay.GetOrAddComponent<AlphaTweenable>().TweenAlpha(0.6f, 0.5f);
		playbutton.SetActive(true);
		pausebutton.SetActive(false);
	}
	public void UnpauseGame()
	{
		overlay.GetOrAddComponent<AlphaTweenable>().StopTween();
		overlay.GetOrAddComponent<AlphaTweenable>().UpdateAlpha(0f);
		playbutton.SetActive(false);
		pausebutton.SetActive(true);
		Time.timeScale = 1f;
	}

	[ContextMenu("Initialize colors")]
	public void InitColors()
	{
		foreach(ColorPalette palette in palettes)
			palette.InitFromHex();
	}
}
