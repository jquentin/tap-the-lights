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
				_camera = GetComponentInChildren<Camera>();
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

#if UNITY_IOS
	private const string ExtraInstrumentsBundleURL = "https://www.jeremyquentin.fr/TapTheLights/data/AssetBundles/iOS/extrainstruments";
#elif UNITY_ANDROID
	private const string ExtraInstrumentsBundleURL = "https://www.jeremyquentin.fr/TapTheLights/data/AssetBundles/Android/extrainstruments";
#else
	private const string ExtraInstrumentsBundleURL = "https://www.jeremyquentin.fr/TapTheLights/data/AssetBundles/Other/extrainstruments";
#endif
	private const string ExtraInstrumentsBundleVersionURL = "https://www.jeremyquentin.fr/TapTheLights/data/AssetBundles/extrainstruments.version";

	[DllImport("__Internal")]
	private static extern void _ReportAchievement( string achievementID, float progress );

	private class ScoreAchievement
	{
		public int score;
		public string id;
		public ScoreAchievement (int score, string id)
		{
			this.score = score;
			this.id = id;
		}
	}

	private List<ScoreAchievement> scoreAchievements = new List<ScoreAchievement>();

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
			foreach(ScoreAchievement sa in scoreAchievements)
			{
				if (_score == sa.score)
					scoreAchievement = sa.id;
			}
//			if (_score == 42)
//				scoreAchievement = "42";
//			else if (score == 99)
//				scoreAchievement = "reach99";
//			else if (score == 420)
//				scoreAchievement = "420";

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

	public void LoadExtraInstruments(AssetBundle bundle)
	{
		AudioClip[] extraInstrumentsClips = bundle.LoadAllAssets<AudioClip>();
		LoadExtraInstruments(extraInstrumentsClips);
	}

	public void LoadExtraInstruments()
	{
		AudioClip[] extraInstrumentsClips = Resources.LoadAll<AudioClip>("ExtraInstruments");
		LoadExtraInstruments(extraInstrumentsClips);
	}
	
	public void LoadExtraInstruments(AudioClip[] clips)
	{
		foreach(AudioClip ac in clips)
		{
			LoadNewAudioClip(ac);
		}
		CleanInstruments();
	}

	void CleanInstruments()
	{
		instruments.RemoveAll(delegate(Instrument instrument) {
			foreach (AudioClip ac in instrument.notes)
			{
				if (ac == null)
				{
					Debug.Log("Remove incomplete instrument: " + instrument.name);
					return true;
				}
			}
			return false;
		});
	}

	void LoadNewAudioClip(AudioClip clip)
	{
		Debug.Log("Load extra audio clip: " + clip.name);
		string[] fileNameSplit = clip.name.Split(new char[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
		if (fileNameSplit.Length < 2)
			Debug.LogWarning("Non valid audio clip: " + clip.name + ". File has to be named \"<InstrumentName>-<NoteIndex>-Anything\".");
		else
		{
			string instrumentName = fileNameSplit[0];
			int noteIndex = -1;
			if (int.TryParse(fileNameSplit[1], out noteIndex))
			{
				Instrument instr = instruments.Find(instrument => instrument.name == instrumentName);
				if (instr == null)
				{
					instr = new Instrument();
					instr.name = instrumentName;
					instr.notes = new List<AudioClip>();
					for ( int i = 0 ; i < 9 ; i++)
						instr.notes.Add(null);
					instruments.Add(instr);
				}
				
				if (instr.notes.Count >= noteIndex)
					instr.notes[noteIndex - 1] = clip;
				else
					instr.notes.Insert(noteIndex - 1, clip);
			}
			else
				Debug.LogWarning("Non valid audio clip: " + clip.name + ". File has to be named \"<InstrumentName>-<NoteIndex>-Anything\".");
		}
	}
	
	float time;

	void InitTimer()
	{
		time = Time.realtimeSinceStartup;
	}

	void LogTime(string label)
	{
		Debug.Log("LogTime: " + label + " : " + (Time.realtimeSinceStartup - time));
		GoogleAnalyticsV3.getInstance().LogTiming("Statistics", (long)((Time.realtimeSinceStartup - time) * 1000f), "TimerLog", label);
		time = Time.realtimeSinceStartup;
	}

	int _oldVersion = int.MinValue;
	int oldVersion
	{
		get
		{
			if (_oldVersion == int.MinValue)
				_oldVersion = PlayerPrefs.GetInt("InstrumentBundleVersion", 0);
			return _oldVersion;
		}
		set
		{
			PlayerPrefs.SetInt("InstrumentBundleVersion", value);
		}
	}

	IEnumerator Start ()
	{
		ScoreScreenController.instance.ShowBlankMode();
		InitTimer();
		while (!Caching.ready)
			yield return null;
		LogTime("Caching Ready");
		// Load the AssetBundle file from Cache if it exists with the same version or download and store it in the cache
		using(WWW www = WWW.LoadFromCacheOrDownload (ExtraInstrumentsBundleURL, oldVersion)){
			yield return www;
			if (www.error != null)
				throw new Exception("WWW download of instruments had an error:" + www.error);
			else
				Debug.Log("WWW download of instruments succeed");
			LogTime("Instruments");
			AssetBundle bundle = www.assetBundle;
			PadController.instance.LoadExtraInstruments(bundle);
			LoadExtraInstruments(bundle);
			bundle.Unload(false);
			LogTime("Instruments processing");
		}


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
					{
						string scoreString = a.id.ToLower().Replace("reach", "");
						int score = -1;
						if (int.TryParse(scoreString, out score))
						{
							ScoreAchievement sa = new ScoreAchievement(score, a.id);
							scoreAchievements.Add(sa);
							Debug.Log ("Reach " + sa.score + " to unlock achievement: " + sa.id);
						}
					}
					LogTime("Social");
				});
			}
			else
				Debug.Log ("Authentication failed");
		});
		Init();
//		LoadingScreen.instance.Hide();
		ScoreScreenController.instance.BackToGameWithTime(0.8f, true);
		int latestVersion = 0;
		using(WWW www = new WWW(ExtraInstrumentsBundleVersionURL)){
			yield return www;
			LogTime("InstrumentsVersion");
			if (www.error != null)
				throw new Exception("WWW download of instruments version file had an error:" + www.error);
			else
				Debug.Log("WWW download of instruments version file succeed");
			latestVersion = int.Parse(www.text);
		}
		Debug.Log("Instrument Version = " + latestVersion);

		if (latestVersion > oldVersion)
		{
			// Load the AssetBundle file from Cache if it exists with the same version or download and store it in the cache
			using(WWW www = WWW.LoadFromCacheOrDownload (ExtraInstrumentsBundleURL, latestVersion)){
				yield return www;
				if (www.error != null)
					throw new Exception("WWW download of instruments had an error:" + www.error);
				else
					Debug.Log("WWW download of instruments succeed");
				LogTime("NonCachedInstruments");
				AssetBundle bundle = www.assetBundle;
				PadController.instance.LoadExtraInstruments(bundle);
				LoadExtraInstruments(bundle);
				bundle.Unload(false);
				LogTime("Instruments processing");
			}
			oldVersion = latestVersion;
		}
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
