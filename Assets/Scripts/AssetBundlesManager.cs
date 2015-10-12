using System;
using UnityEngine;
using System.Collections;

public class AssetBundlesManager : MonoBehaviour {
	
//	private const string ExtraInstrumentsBundleURL = "https://www.jeremyquentin.fr/tapthelights/assetBundles/extrainstruments";
	private const string ExtraInstrumentsBundleURL = "https://dl.dropbox.com/s/0vxa1oi5u159pbu/xylo2test";
	public int version
	{
		get
		{
			return 1;
//			return PlayerPrefs.GetInt("Extrainstruments-version", 0);
		}
	}
	
	void Start() {
		StartCoroutine (DownloadAndCache());
	}
	
	IEnumerator DownloadAndCache (){
		// Wait for the Caching system to be ready
		while (!Caching.ready)
			yield return null;
		
		// Load the AssetBundle file from Cache if it exists with the same version or download and store it in the cache
		using(WWW www = WWW.LoadFromCacheOrDownload (ExtraInstrumentsBundleURL, version)){
			yield return www;
			if (www.error != null)
				throw new Exception("WWW download had an error:" + www.error);
			else
				Debug.Log("WWW download of instruments succeed");
			AssetBundle bundle = www.assetBundle;
			PadController.instance.LoadExtraInstruments(bundle);
			AudioClip[] clips = bundle.LoadAllAssets<AudioClip>();
			foreach(AudioClip ac in clips)
				print(ac.name);
//			if (AssetName == "")
//				Instantiate(bundle.mainAsset);
//			else
//				Instantiate(bundle.LoadAsset(AssetName));
//			// Unload the AssetBundles compressed contents to conserve memory
//			bundle.Unload(false);
			
		} // memory is freed from the web stream (www.Dispose() gets called implicitly)
	}
}