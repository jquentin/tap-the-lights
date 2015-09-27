using System.IO; // for FileStream
using System; // for BitConverter and Byte Type
using UnityEngine;
using System.Collections;

public class AudioRecorder : MonoBehaviour
{

	#region Singleton
	private static AudioRecorder _instance;
	public static AudioRecorder instance
	{
		get
		{
			if (_instance == null)
				_instance = FindObjectOfType<AudioRecorder>();
			return _instance;
		}
	}
	#endregion

	private int bufferSize;
	private int numBuffers;
	private int outputRate = 44100;
	public string fileName = "recTest";
	private int headerSize = 44; //default for uncompressed wav

	private const string INDEX_FILE_KEY = "IndexFile";
	private int indexFile
	{
		get
		{
			int res = PlayerPrefs.GetInt(INDEX_FILE_KEY, 0);
			return res;
		}
	}
	
	private int GetIndexFileAndIncrement()
	{
		int res = indexFile;
		PlayerPrefs.SetInt(INDEX_FILE_KEY, res + 1);
		return res;
	}

	private bool recOutput;

	private FileStream fileStream;

	void Awake()
	{
		AudioSettings.outputSampleRate = outputRate;
	}

	void Start()
	{
		AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
//		FileStream fs = File.OpenRead(fileName + "_" + (indexFile - 1) + ".wav");
//		print (fs);
		StartCoroutine(loadFile(fileName + "_" + (indexFile - 1) + ".wav"));
	}

	public void StartRecording()
	{
		if(!recOutput)
		{
			Debug.Log("Start recording");
			StartWriting(fileName + "_" + GetIndexFileAndIncrement() + ".wav");
			recOutput = true;
		}
		else
		{
			Debug.LogWarning("Already recording");
		}
	}

	public void StopRecording()
	{
		if(recOutput)
		{
			Debug.Log("Stop recording");
			recOutput = false;
			WriteHeader();     
		}
		else
		{
			Debug.LogWarning("Is not recording");
		}
	}

	IEnumerator loadFile(string path) {
		print("d1");
		WWW www = new WWW("file://"+path);
		
		print("d2");
		AudioClip myAudioClip = www.audioClip;
		print("d3");
		while (!myAudioClip.isReadyToPlay)
		{
			print("d4");
			yield return www;
		}
		print("d5");
		
		AudioClip clip = www.GetAudioClip(false);
		print("d6");
		string[] parts = path.Split('\\');
		print("d7");
		clip.name = parts[parts.Length - 1];
		print("d8");
		GetComponent<AudioSource>().PlayOneShot(clip);
	}

	void Update()
	{
		if(Input.GetKeyDown("r"))
		{
			Debug.Log("rec");
			if(recOutput == false)
			{
				StartRecording();
			}
			else
			{
				StopRecording();
			}
		}  
	}

	void StartWriting(string name)
	{
		fileStream = new FileStream(name, FileMode.Create);
		byte emptyByte = new byte();
		
		for(int i = 0; i<headerSize; i++) //preparing the header
		{
			fileStream.WriteByte(emptyByte);
		}
	}

	void OnAudioFilterRead(float[] data, int channels)
	{
		if(recOutput)
		{
			ConvertAndWrite(data); //audio data is interlaced
		}
	}

	void ConvertAndWrite(float[] dataSource)
	{
		
		Int16[] intData = new Int16[dataSource.Length];
		//converting in 2 steps : float[] to Int16[], //then Int16[] to Byte[]
		
		Byte[] bytesData = new Byte[dataSource.Length*2];
		//bytesData array is twice the size of
		//dataSource array because a float converted in Int16 is 2 bytes.
		
		int rescaleFactor = 32767; //to convert float to Int16
		
		for (int i = 0; i<dataSource.Length;i++)
		{
			intData[i] = (short)(dataSource[i]*rescaleFactor);
			Byte[] byteArr = new Byte[2];
			byteArr = BitConverter.GetBytes(intData[i]);
			byteArr.CopyTo(bytesData,i*2);
		}
		
		fileStream.Write(bytesData,0,bytesData.Length);
	}

	void WriteHeader()
	{
		
		fileStream.Seek(0,SeekOrigin.Begin);
		
		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		fileStream.Write(riff,0,4);
		
		Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length-8);
		fileStream.Write(chunkSize,0,4);
		
		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		fileStream.Write(wave,0,4);
		
		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		fileStream.Write(fmt,0,4);
		
		Byte[] subChunk1 = BitConverter.GetBytes(16);
		fileStream.Write(subChunk1,0,4);
		
		UInt16 two = 2;
		UInt16 one = 1;
		
		Byte[] audioFormat = BitConverter.GetBytes(one);
		fileStream.Write(audioFormat,0,2);
		
		Byte[] numChannels = BitConverter.GetBytes(two);
		fileStream.Write(numChannels,0,2);
		
		Byte[] sampleRate = BitConverter.GetBytes(outputRate);
		fileStream.Write(sampleRate,0,4);
		
		Byte[] byteRate = BitConverter.GetBytes(outputRate*4);
		// sampleRate * bytesPerSample*number of channels, here 44100*2*2
		
		fileStream.Write(byteRate,0,4);
		
		UInt16 four = 4;
		Byte[] blockAlign = BitConverter.GetBytes(four);
		fileStream.Write(blockAlign,0,2);
		
		UInt16 sixteen = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(sixteen);
		fileStream.Write(bitsPerSample,0,2);
		
		Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
		fileStream.Write(dataString,0,4);
		
		Byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length-headerSize);
		fileStream.Write(subChunk2,0,4);
		
		fileStream.Close();
	}

}
