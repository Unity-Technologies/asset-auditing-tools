using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Configuration;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

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
	
	[CreateAssetMenu(fileName = "Asset Tool Profile", menuName = "Asset Tools/New Profile", order = 0)]
	public class AuditProfile : ScriptableObject
	{
		public UnityEngine.Object m_ImporterReference = null;
		
		// TODO if Unity 2018+ Take a preset, else use a ImporterReference
		//public Preset m_TextureImporter;
		public List<Filter> m_Filters;
		
		public List<string> m_ConstrainProperties = new List<string>();
		[NonSerialized] public List<string> m_ConstrainPropertiesDisplayNames = new List<string>();
		
		[NonSerialized] public List<SerializedProperty> m_SerialisedProperties = new List<SerializedProperty>();

		public int PropertyCount
		{
			get { return m_ConstrainProperties == null ? 0 : m_ConstrainProperties.Count;  }
		}
		
		public AssetImporter GetAssetImporter()
		{
			return AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( m_ImporterReference ) );
		}

		public AssetType GetAssetType()
		{
			if( m_ImporterReference == null )
			{
				Debug.LogError( "Template required to be set" );
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
			throw new ArgumentOutOfRangeException( "assetType", a, null );
		}
		
		

		public void AddProperty( string propName, bool isRealName = true )
		{
			string otherName = isRealName ? GetDisplayName( propName ) : GetRealName( propName );
			
			if( m_ConstrainProperties == null )
				m_ConstrainProperties = new List<string>();
			if( m_ConstrainPropertiesDisplayNames == null )
				m_ConstrainPropertiesDisplayNames = new List<string>();
			
			m_ConstrainProperties.Add( isRealName ? propName : otherName );
			m_ConstrainPropertiesDisplayNames.Add( isRealName ? otherName : propName );
		}

		public void RemoveProperty( string p, bool realName = true )
		{
			List<string> list = realName ? m_ConstrainProperties : m_ConstrainPropertiesDisplayNames;
			for( int i = 0; i < list.Count; ++i )
			{
				if( list[i] == p )
				{
					RemoveProperty( i );
					return;
				}
			}
		}

		public void RemoveProperty( int i )
		{
			m_ConstrainProperties.RemoveAt( i );
			m_ConstrainPropertiesDisplayNames.RemoveAt( i );
		}
		
		public string GetDisplayName( int i )
		{
			if( m_ConstrainProperties.Count != m_ConstrainPropertiesDisplayNames.Count )
				Debug.LogError( "wrong count" );
			return m_ConstrainPropertiesDisplayNames[i];
		}

		private void OnValidate()
		{
			m_ConstrainPropertiesDisplayNames = new List<string>();
			GatherProperties();
		}

		// TODO do this less
		public void GatherProperties()
		{
			if( m_ImporterReference == null )
				return;
			
			SerializedObject so = new SerializedObject( GetAssetImporter() );
			SerializedProperty iter = so.GetIterator();
			
			List<string> props = new List<string>();
			
			m_SerialisedProperties = new List<SerializedProperty>();
			iter.NextVisible(true);
            
			do
			{
				props.Add( iter.name );
			}
			while (iter.NextVisible(false)) ;
			
			foreach( string s in props )
			{
				m_SerialisedProperties.Add( so.FindProperty( s ) );
			}
			
			m_ConstrainPropertiesDisplayNames = new List<string>();
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( m_ConstrainProperties.Contains( property.name ) )
					m_ConstrainPropertiesDisplayNames.Add( property.displayName );
			}
		}

		public string GetDisplayName( string realName )
		{
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( property.name == realName )
				{
					return property.displayName;
				}
			}

			return "";
		}
		
		string GetRealName( string displayName )
		{
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( property.displayName == displayName )
					return property.name;
			}

			return "";
		}
	}
}