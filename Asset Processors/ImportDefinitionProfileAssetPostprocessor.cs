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
		private static Dictionary<string, ImportContext> m_AssetProcessingContext = new Dictionary<string, ImportContext>();

		

		private static ImportContext GetContext( string path )
		{
			ImportContext ctx;
			if( !m_AssetProcessingContext.TryGetValue( path, out ctx ) )
			{
				ctx = new ImportContext();
				ctx.Importer = AssetImporter.GetAtPath( path );
				m_AssetProcessingContext.Add( path, ctx );
			}

			return ctx;
		}

		private ImportContext GetContext( )
		{
			ImportContext ctx;
			if( !m_AssetProcessingContext.TryGetValue( assetPath, out ctx ) )
			{
				ctx = new ImportContext();
				ctx.Importer = assetImporter;
				m_AssetProcessingContext.Add( assetPath, ctx );
			}

			return ctx;
		}

		private static ImportContext SetContext( string path, Texture2D texture )
		{
			ImportContext ctx = GetContext( path );
			ctx.PostprocessingTexture = texture;
			return ctx;
		}
		
		private ImportContext SetContext( Texture2D texture )
		{
			ImportContext ctx = GetContext();
			ctx.PostprocessingTexture = texture;
			return ctx;
		}
		
		
		private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
		//	Debug.Log( "OnPostprocessAllAssets" );
			
			
			// TODO process any changes needed by Profiles being chnaged
			// for any Profiles (that auto import is set set), imported / deleted / moved

			// get any Assets that may have been affected.

			// get the userData for the Asset
			

			// calculate what it should be

			// if it is different, then the asset needs to be changed to what it should be, and reimported.
			
			
			
			
			// List<ProfileData> defs = ImportDefinitionProfileCache.Profiles;
			// foreach( string asset in importedAssets )
			// {
			// 	// TODO anything that does not have a specialised Processing method process them
			// }
			
			
			
			
			
			m_AssetProcessingContext.Clear();
		}

		private void OnPreprocessAsset()
		{
			ImportContext context = GetContext( );
			// TODO optimise this
			List<ProfileData> defs = ImportDefinitionProfileCache.Profiles;
			// Any profiles can interact with the Asset, so we need to check all
			for( int i = 0; i < defs.Count; ++i )
			{
				defs[i].m_ImportDefinitionProfile.PreprocessAsset( context );
			}
		}
		
		/// <summary>
		/// To properly post process Assets, we need to use the methods individually
		/// </summary>

		private void OnPostprocessTexture( Texture2D texture )
		{
			ImportContext context = SetContext( texture );
			
			// TODO optimise this
			List<ProfileData> defs = ImportDefinitionProfileCache.Profiles;
			// Any profiles can interact with the Asset, so we need to check all
			for( int i = 0; i < defs.Count; ++i )
			{
				defs[i].m_ImportDefinitionProfile.PostprocessAsset( context );
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