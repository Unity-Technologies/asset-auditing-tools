using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{
	public enum AssetType
	{
		Texture,
		Model,
		Audio,
		Folder,
		NA
	}
	
	[CreateAssetMenu(fileName = "NewAssetAuditorProfile", menuName = "Asset Tools/New Auditor Profile", order = 0)]
	public class AuditProfile : ScriptableObject
	{
		public bool m_RunOnImport = true;
		public List<Filter> m_Filters;
		
		public UnityEngine.Object m_ImporterReference = null;
		
#if UNITY_2018_1_OR_NEWER
		// TODO on 2018 allow for the use of a Preset instead of an Object reference
		public Preset m_Preset;
#endif
		
		public ImporterPropertiesModule m_ImporterModule;

		private void OnEnable()
		{
			if( m_ImporterModule != null )
				m_ImporterModule.m_Profile = this;
		}

		public AssetImporter GetAssetImporter()
		{
			return AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( m_ImporterReference ) );
		}

		public AssetType GetAssetType()
		{
			if( m_ImporterReference == null )
			{
				//Debug.LogError( string.Format("Template not set in Profile \"{0}\"", name) );
				return AssetType.NA;
			}

			string path = AssetDatabase.GetAssetPath( m_ImporterReference );
			AssetImporter a = AssetImporter.GetAtPath( path );
			if( AssetDatabase.IsValidFolder( path ) )
				return AssetType.Folder;
			if( a is TextureImporter )
				return AssetType.Texture;
			if( a is ModelImporter )
				return AssetType.Model;
			if( a is AudioImporter )
				return AssetType.Audio;
			
			throw new IndexOutOfRangeException( string.Format( "ProfileAssetImporter {0}, not a supported Type", a.GetType().Name ) );
		}
		
		
		public List<IConformObject> GetConformData( string asset )
		{
			// TODO each module
			return m_ImporterModule.GetConformObjects( asset );
		}
	}
}