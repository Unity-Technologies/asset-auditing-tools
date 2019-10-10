using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{

	public interface IPreprocessor
	{
		bool OnPreprocessAsset( ImportContext context, string data );
		int GetVersion();
	}

}