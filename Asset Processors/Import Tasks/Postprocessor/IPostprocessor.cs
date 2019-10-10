using UnityEditor;

namespace AssetTools
{

	public interface IPostprocessor
	{
		bool OnPostprocessAsset( ImportContext context, string data );
		int GetVersion();
	}

}