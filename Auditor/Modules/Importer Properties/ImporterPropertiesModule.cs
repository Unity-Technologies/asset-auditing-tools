using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	/// <summary>
	/// TODO test this
	/// will this work for just any SerialisedObject, like ScriptableObject??
	/// SerialisedPropertiesModule
	/// </summary>
	
	[System.Serializable]
	public class ImporterPropertiesModule : IImportProcessModule
	{
		public UnityEngine.Object m_ImporterReference = null;
		
#if UNITY_2018_1_OR_NEWER
		// TODO on 2018 allow for the use of a Preset instead of an Object reference
		public Preset m_Preset;
#endif
		
		public List<string> m_ConstrainProperties = new List<string>();
		

		//[NonSerialized] 
		public List<string> m_ConstrainPropertiesDisplayNames = new List<string>();
		[NonSerialized] public List<SerializedProperty> m_SerialisedProperties = new List<SerializedProperty>();
		private int m_HasProperties = 0;
		

		
		
		private AssetType GetAssetType()
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
			
			throw new IndexOutOfRangeException( string.Format( "ReferenceAssetImporter {0}, not a supported Type", a.GetType().Name ) );
		}

		
		
		private AssetImporter GetAssetImporter()
		{
			return AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( m_ImporterReference ) );
		}

		private AssetImporter m_AssetImporter;
		public AssetImporter ReferenceAssetImporter
		{
			get
			{
				if( m_AssetImporter == null && m_ImporterReference != null )
					m_AssetImporter = AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( m_ImporterReference ) );
				return m_AssetImporter;
			}
		}
		
		internal int PropertyCount
		{
			get
			{
				GatherPropertiesIfNeeded();
				return m_ConstrainProperties == null ? 0 : m_ConstrainProperties.Count;
			}
		}
		
		public bool GetSearchFilter( out string typeFilter, List<string> ignoreAssetPaths )
		{
			typeFilter = null;
			if( m_ImporterReference != null )
			{
				switch( GetAssetType() )
				{
					case AssetType.Texture:
						typeFilter = "t:Texture";
						break;
					case AssetType.Model:
						typeFilter = "t:GameObject";
						break;
					case AssetType.Audio:
						typeFilter = "t:AudioClip";
						break;
					case AssetType.Folder:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				ignoreAssetPaths.Add( AssetDatabase.GetAssetPath( m_ImporterReference ) );
				return true;
			}

			return false;
		}

		internal void AddProperty( string propName, bool isRealName = true )
		{
			GatherPropertiesIfNeeded();

			string otherName = isRealName ? GetPropertyDisplayName( propName ) : GetPropertyRealName( propName );

			if( m_ConstrainProperties == null )
				m_ConstrainProperties = new List<string>();
			if( m_ConstrainPropertiesDisplayNames == null )
				m_ConstrainPropertiesDisplayNames = new List<string>();

			m_ConstrainProperties.Add( isRealName ? propName : otherName );
			m_ConstrainPropertiesDisplayNames.Add( isRealName ? otherName : propName );
		}

		private void RemoveProperty( string p, bool realName = true )
		{
			GatherPropertiesIfNeeded();

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

		internal void RemoveProperty( int i )
		{
			GatherPropertiesIfNeeded();

			m_ConstrainProperties.RemoveAt( i );
			m_ConstrainPropertiesDisplayNames.RemoveAt( i );
		}

		internal string GetPropertyDisplayName( int i )
		{
			GatherPropertiesIfNeeded();

			return m_ConstrainPropertiesDisplayNames[i];
		}

		internal string GetPropertyDisplayName( string realName )
		{
			GatherPropertiesIfNeeded();

			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( property.name == realName )
				{
					return property.displayName;
				}
			}

			return string.Empty;
		}

		internal string GetPropertyRealName( string displayName )
		{
			GatherPropertiesIfNeeded();

			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( property.displayName == displayName )
					return property.name;
			}

			return string.Empty;
		}

		internal void GatherPropertiesIfNeeded()
		{
			if( m_HasProperties == 0 || m_ConstrainProperties.Count != m_ConstrainPropertiesDisplayNames.Count )
				GatherProperties();
		}
		internal void GatherProperties()
		{
			if( m_ImporterReference == null )
				return;

			SerializedObject so = new SerializedObject( GetAssetImporter() );
			SerializedProperty iter = so.GetIterator();

			List<string> props = new List<string>();

			m_SerialisedProperties.Clear();
			iter.NextVisible( true );

			do
			{
				props.Add( iter.name );
			} while( iter.NextVisible( false ) );

			foreach( string s in props )
			{
				m_SerialisedProperties.Add( so.FindProperty( s ) );
			}

			GatherDisplayNames();

			m_HasProperties = m_ConstrainProperties.Count > 0 ? 1 : -1;
		}

		internal void GatherDisplayNames()
		{
			m_ConstrainPropertiesDisplayNames.Clear();
			for( int i=0; i<m_ConstrainProperties.Count; ++i )
				m_ConstrainPropertiesDisplayNames.Add( null );
			
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				int i = m_ConstrainProperties.IndexOf( property.name );
				if( i >= 0 )
					m_ConstrainPropertiesDisplayNames[i] = property.displayName;
			}
		}
		
		public List<IConformObject> GetConformObjects( string asset )
		{
			AssetImporter assetImporter = AssetImporter.GetAtPath( asset );
			if( m_ImporterReference == null )
				return new List<IConformObject>(0);
			
			SerializedObject assetImporterSO = new SerializedObject( assetImporter );
			SerializedObject profileImporterSO = new SerializedObject( ReferenceAssetImporter );
			
			if( m_ConstrainProperties.Count == 0 )
			{
				return CompareSerializedObject( profileImporterSO, assetImporterSO );
			}
			
			List<IConformObject> infos = new List<IConformObject>();
			
			for( int i = 0; i < m_ConstrainProperties.Count; ++i )
			{
				string propertyName = m_ConstrainProperties[i];

				SerializedProperty foundAssetSP = assetImporterSO.FindProperty( propertyName );
				SerializedProperty assetRuleSP = profileImporterSO.FindProperty( propertyName );

				PropertyConformObject conformObject = new PropertyConformObject( propertyName );
				conformObject.SetSerializedProperties( assetRuleSP, foundAssetSP );
				infos.Add( conformObject );
			}

			return infos;
		}

		private List<IConformObject> CompareSerializedObject( SerializedObject template, SerializedObject asset )
		{
			SerializedProperty ruleIter = template.GetIterator();
			SerializedProperty assetIter = asset.GetIterator();
			assetIter.NextVisible( true );
			ruleIter.NextVisible( true );
			
			List<IConformObject> infos = new List<IConformObject>();

			do
			{
				PropertyConformObject @object = new PropertyConformObject( ruleIter.name );
				// TODO better way to do this?
				@object.SetSerializedProperties( template.FindProperty( ruleIter.name ), asset.FindProperty( assetIter.name ) );
				infos.Add( @object );
				ruleIter.NextVisible( false );
			} while( assetIter.NextVisible( false ) );

			return infos;
		}
		
		public void FixCallback( AssetDetailList calledFromTreeView, object context )
		{
			// TODO find out how to multi select
			// TODO if selection is a folder
			
			AssetViewItem selectedNodes = context as AssetViewItem;
			if( selectedNodes != null )
			{
				CopyProperties( selectedNodes );
				foreach( IConformObject data in selectedNodes.conformData )
				{
					data.Conforms = true;
				}
				calledFromTreeView.m_PropertyList.Reload();
			}
			else
				Debug.LogError( "Could not fix Asset with no Assets selected." );
		}
		
		private void CopyProperties( AssetViewItem item )
		{
			if( m_ConstrainProperties.Count > 0 )
			{
				SerializedObject profileSerializedObject = new SerializedObject( ReferenceAssetImporter );
				SerializedObject assetImporterSO = new SerializedObject( item.AssetImporter );
				CopyConstrainedProperties( assetImporterSO, profileSerializedObject );
			}
			else
			{
				EditorUtility.CopySerialized( ReferenceAssetImporter, item.AssetImporter );
			}

			item.conforms = true;
			item.ReimportAsset();
		}
		
		private void CopyConstrainedProperties( SerializedObject affectedAssetImporterSO, SerializedObject templateImporterSO )
		{
			foreach( string property in m_ConstrainProperties )
			{
				SerializedProperty assetRuleSP = templateImporterSO.FindProperty( property );
				affectedAssetImporterSO.CopyFromSerializedProperty( assetRuleSP );
			}
			
			if( ! affectedAssetImporterSO.ApplyModifiedProperties() )
				Debug.LogError( "copy failed" );
		}
	}
}