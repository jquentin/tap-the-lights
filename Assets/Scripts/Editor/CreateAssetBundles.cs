using UnityEditor;
using System.IO;

public class CreateAssetBundles
{
	[MenuItem ("Assets/Build AssetBundles")]
	static void BuildAllAssetBundles ()
	{
		Directory.CreateDirectory("AssetBundles/iOS");
		BuildPipeline.BuildAssetBundles ("AssetBundles/iOS", BuildAssetBundleOptions.None, BuildTarget.iOS);
		Directory.CreateDirectory("AssetBundles/Android");
		BuildPipeline.BuildAssetBundles ("AssetBundles/Android", BuildAssetBundleOptions.None, BuildTarget.Android);
	}
}
