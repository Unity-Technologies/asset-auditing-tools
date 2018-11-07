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
		public UnityEngine.Object m_ImporterReference = null;
		
		#if UNITY_2018_1_OR_NEWER
		// TODO on 2018 allow for the use of a Preset instead of an Object reference
		public Preset m_Preset;
		#endif
		
		public List<Filter> m_Filters;
		public List<string> m_ConstrainProperties = new List<string>();
		
		[NonSerialized] public List<string> m_ConstrainPropertiesDisplayNames = new List<string>();
		[NonSerialized] public List<SerializedProperty> m_SerialisedProperties = new List<SerializedProperty>();
		[NonSerialized] private int m_HasProperties = 0;

		public int PropertyCount
		{
			get
			{
				if( m_HasProperties == 0 )
					GatherProperties();
				return m_ConstrainProperties == null ? 0 : m_ConstrainProperties.Count;
			}
		}
		
		public AssetImporter GetAssetImporter()
		{
			return AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( m_ImporterReference ) );
		}

		public AssetType GetAssetType()
		{
			if( m_ImporterReference == null )
			{
				Debug.LogError( string.Format("Template not set in Profile \"{0}\"", name) );
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
			
			throw new IndexOutOfRangeException( string.Format( "AssetImporter {0}, not a supported Type", a.GetType().Name ) );
		}

		public void AddProperty( string propName, bool isRealName = true )
		{
			if( m_HasProperties == 0 )
				GatherProperties();
			
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
			if( m_HasProperties == 0 )
				GatherProperties();
			
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
			if( m_HasProperties == 0 )
				GatherProperties();
			
			m_ConstrainProperties.RemoveAt( i );
			m_ConstrainPropertiesDisplayNames.RemoveAt( i );
		}
		
		public string GetDisplayName( int i )
		{
			if( m_HasProperties == 0 )
				GatherProperties();
			
			if( m_ConstrainProperties.Count != m_ConstrainPropertiesDisplayNames.Count )
				Debug.LogError( "DisplayName and PropertyName count's do not match" );
			
			return m_ConstrainPropertiesDisplayNames[i];
		}
		
		public string GetDisplayName( string realName )
		{
			if( m_HasProperties == 0 )
				GatherProperties();
			
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( property.name == realName )
				{
					return property.displayName;
				}
			}

			return string.Empty;
		}
		
		string GetRealName( string displayName )
		{
			if( m_HasProperties == 0 )
				GatherProperties();
			
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( property.displayName == displayName )
					return property.name;
			}

			return string.Empty;
		}

		internal void GatherProperties()
		{
			if( m_ImporterReference == null )
				return;
			
			SerializedObject so = new SerializedObject( GetAssetImporter() );
			SerializedProperty iter = so.GetIterator();
			
			List<string> props = new List<string>();
			
			m_SerialisedProperties.Clear();
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
			
			m_ConstrainPropertiesDisplayNames.Clear();
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( m_ConstrainProperties.Contains( property.name ) )
					m_ConstrainPropertiesDisplayNames.Add( property.displayName );
			}

			m_HasProperties = m_ConstrainProperties.Count > 0 ? 1 : -1;
		}
	}
}