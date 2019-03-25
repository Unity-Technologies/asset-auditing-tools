using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	public class ImportDefinitionFileAssetPostprocessor : AssetPostprocessor
	{

		private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
			
			
			
			// TODO if any profiles have moved, then we need to move stuff around and reimport stuff

			// for any Profiles (that auto import is set set), imported / deleted / moved

			// get any Assets that may have been affected.

			// get the userData for the Asset
			

			// calculate what it should be

			// if it is different, then the asset needs to be changed to what it should be, and reimported.
			
			
			foreach( string asset in importedAssets )
			{
				//UserDataSerialization data = UserDataSerialization.ParseForAssetPath( asset );
				
				
			}
		}

		private void OnPreprocessAsset()
		{
			// find any profiles it may be affected by.
			
			// check if that is a valid profile
			
			// apply
		}
		
		#if ! UNITY_2018_1_OR_NEWER
		private void OnPreprocessAudio()
		{
			OnPreprocessAsset();
		}

		private void OnPreprocessModel()
		{
			OnPreprocessAsset();
		}

		private void OnPreprocessTexture()
		{
			OnPreprocessAsset();
		}
		#endif
	}

}