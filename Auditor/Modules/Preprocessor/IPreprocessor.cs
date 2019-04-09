using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{

	public interface IPreprocessor
	{
		bool OnPreprocessAsset( AssetImporter importer, string data );
		int GetVersion();
	}

}