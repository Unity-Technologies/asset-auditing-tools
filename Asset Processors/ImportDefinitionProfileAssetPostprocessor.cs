using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	public class ImportDefinitionProfileAssetPostprocessor : AssetPostprocessor
	{
		// Not really needed
		public override uint GetVersion()
		{
			return base.GetVersion();
		}

		private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
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
			// TODO optimise this
			List<ProfileData> defs = ImportDefinitionProfileCache.Profiles;
			// Any profiles can interact with the Asset, so we need to check all
			for( int i = 0; i < defs.Count; ++i )
			{
				defs[i].m_ImportDefinitionProfile.ProcessAsset( this.assetImporter );
			}
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