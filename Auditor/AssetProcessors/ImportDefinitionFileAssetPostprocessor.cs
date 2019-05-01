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
		// TODO inform the cache, so it does not need its own AssetPostprocessor script

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
			List<AuditProfileData> defs = ProfileCache.Profiles;
			// Any profiles can interact with the Asset, so we need to check all
			for( int i = 0; i < defs.Count; ++i )
			{
				defs[i].m_AuditProfile.ProcessAsset( this.assetImporter );
			}
			
			// // this is pretty optimal to Get profiles up its folder structure from root. Could be better to limit this approach
			// // Need to decide if want IDF's outside the folder structure. And if to keep the Window (for not auto importing IDF)
			// string path = assetImporter.assetPath;
			// string folderPath = path;
			// List<string> folders = new List<string>();
			//
			//  // this doesn't really work, there can be profiles outside of the folder structure that affects the
			//
			//  while( folderPath != "" )
			//  {
			//  	int index = folderPath.LastIndexOf( '/' );
			//  	if( index == -1 )
			//  		break;
			//  	
			//  	folderPath = folderPath.Remove( index );
			//  	folders.Add( folderPath );
			//  }
			//
			//  // Get a list of profiles in each folder
			//  List<AuditProfileData>[] folderProfiles = new List<AuditProfileData>[folders.Count];
			//  for( int i = 0; i < defs.Count; ++i )
			//  {
			//  	for( int f = 0; f < folders.Count; ++f )
			//  	{
			//  		if( String.CompareOrdinal( folders[f], defs[i].m_FolderPath ) == 0 )
			//  		{
			//  			if( folderProfiles[f] == null )
			//  				folderProfiles[f] = new List<AuditProfileData>(1);
			//  			folderProfiles[f].Add( defs[i] );
			//  			break;
			//  		}
			//  	}
			//  }
			//
			//  // going from root folder
			//  for( int i = folderProfiles.Length-1; i >= 0; --i )
			//  {
			//  	if( folderProfiles[i] == null )
			//  		continue;
			//  	
			//  	for( int d = 0; d < folderProfiles[i].Count; ++d )
			//  	{
			//  		folderProfiles[i][d].m_AuditProfile.ProcessAsset( this.assetImporter );
			//  	}
			//  }
			
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