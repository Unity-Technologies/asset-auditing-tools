using System;
using System.Collections;
using System.Collections.Generic;
using AssetTools.GUIUtility;
using UnityEditor;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Presets;
#endif
using UnityEngine;

namespace AssetTools
{
	public enum AssetType
	{
		Texture,
		Model,
		Audio,
		Folder,
		Native,
		NA
	}
	
	[System.Serializable]
	public class ImporterPropertiesImportTask : BaseImportTask
	{
		public UnityEngine.Object m_ImporterReference = null;

		public List<string> m_ConstrainProperties = new List<string>();

		[NonSerialized] public List<string> m_ConstrainPropertiesDisplayNames = new List<string>();
		[NonSerialized] public List<SerializedProperty> m_SerialisedProperties = new List<SerializedProperty>();
		private int m_HasProperties = 0;
		
		internal AssetImporter m_AssetImporter;
		
		private ImporterPropertiesImportTaskInspector m_Inspector = null;
		
		public override Type GetConformObjectType()
		{
			return typeof(PropertyConformObject);
		}
		
		public override ProcessingType TaskProcessType
		{
			get { return ProcessingType.Pre; }
		}

		public override void PreprocessTask( ImportContext context, ImportDefinitionProfile profile )
		{
			// this task doesn't need to set the metaData
		}

		public override string AssetMenuFixString
		{
			get { return "Conform to Importer Template Properties"; }
		}
		
		private AssetType GetAssetType()
		{
			if( m_ImporterReference == null )
				return AssetType.NA;

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
			
			return AssetType.Native;
		}
		
		private AssetImporter GetAssetImporter()
		{
			return AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( m_ImporterReference ) );
		}
		
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
		
		public override bool CanProcess( AssetImporter item )
		{
			if( m_ImporterReference == null || ReferenceAssetImporter == item )
				return false;
			
			return item.GetType() == ReferenceAssetImporter.GetType();
		}

		public override int Version
		{
			get { return 0; }
		}

		public override bool GetSearchFilter( out string searchFilter, List<string> ignoreAssetPaths )
		{
			searchFilter = null;
			if( m_ImporterReference != null )
			{
				switch( GetAssetType() )
				{
					case AssetType.Texture:
						searchFilter = "t:Texture";
						break;
					case AssetType.Model:
						searchFilter = "t:GameObject";
						break;
					case AssetType.Audio:
						searchFilter = "t:AudioClip";
						break;
					case AssetType.Folder:
						break;
					case AssetType.Native:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				ignoreAssetPaths.Add( AssetDatabase.GetAssetPath( m_ImporterReference ) );
				return true;
			}

			return false;
		}
		
		public override List<IConformObject> GetConformObjects( string asset, ImportDefinitionProfile profile )
		{
			AssetImporter assetImporter = AssetImporter.GetAtPath( asset );
			if( m_ImporterReference == null )
				return new List<IConformObject>(0);
			
			SerializedObject assetImporterSO = new SerializedObject( assetImporter );
			SerializedObject profileImporterSO = new SerializedObject( ReferenceAssetImporter );
			
			// TODO Performance: if there are any. check to make sure these are valid constraints, if not, don't include them in the count 
			if( m_ConstrainProperties.Count == 0 )
			{
				return CompareSerializedObject( profileImporterSO, assetImporterSO );
			}
			
			List<IConformObject> infos = new List<IConformObject>();
			
			for( int i = 0; i < m_ConstrainProperties.Count; ++i )
			{
				SerializedProperty assetRuleSP = profileImporterSO.FindProperty( m_ConstrainProperties[i] );
				if( assetRuleSP == null )
					continue; // could be properties from another Object
				SerializedProperty foundAssetSP = assetImporterSO.FindProperty( m_ConstrainProperties[i] );

				PropertyConformObject conformObject = new PropertyConformObject( m_ConstrainProperties[i] );
				conformObject.SetSerializedProperties( assetRuleSP, foundAssetSP );
				infos.Add( conformObject );
			}

			return infos;
		}

		// TODO no longer used, would this be a good public API?
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

		private void RemoveProperty( int i )
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
			
			// TODO check if properties exist in active m_Importer
			

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

		private static List<IConformObject> CompareSerializedObject( SerializedObject template, SerializedObject asset )
		{
			SerializedProperty templateIter = template.GetIterator();
			SerializedProperty assetIter = asset.GetIterator();
			assetIter.NextVisible( true );
			templateIter.NextVisible( true );
			
			List<IConformObject> infos = new List<IConformObject>();

			do
			{
				if( assetIter.name == "m_UserData" )
				{
					templateIter.NextVisible( false );
					continue;
				}
				
				// TODO Performance: Is there a better way to find the properties?
				// possibly could use utility method to get all properties in one loop?? (this may not work, NextVisible will not work this way)
				PropertyConformObject conformObject = new PropertyConformObject( templateIter.name );
				conformObject.SetSerializedProperties( template.FindProperty( templateIter.name ), asset.FindProperty( assetIter.name ) );
				infos.Add( conformObject );
				
				templateIter.NextVisible( false );
			} while( assetIter.NextVisible( false ) );

			return infos;
		}
		
		public override bool Apply( ImportContext context, ImportDefinitionProfile fromProfile )
		{
			if( CanProcess( context.Importer ) == false )
				return false;
			
			if( m_ConstrainProperties.Count > 0 )
			{
				SerializedObject profileSerializedObject = new SerializedObject( ReferenceAssetImporter );
				SerializedObject assetImporterSO = new SerializedObject( context.Importer );
				CopyConstrainedProperties( assetImporterSO, profileSerializedObject );
			}
			else
			{
				EditorUtility.CopySerialized( ReferenceAssetImporter, context.Importer );
			}
			
			return true;
		}
		
		private void CopyConstrainedProperties( SerializedObject affectedAssetImporterSO, SerializedObject templateImporterSO )
		{
			foreach( string property in m_ConstrainProperties )
			{
				SerializedProperty templateSerialisedProperty = templateImporterSO.FindProperty( property );
				affectedAssetImporterSO.CopyFromSerializedProperty( templateSerialisedProperty );
			}
			
			if( ! affectedAssetImporterSO.ApplyModifiedProperties() )
				Debug.LogError( "copy failed" );
		}
		
		public override void DrawGUI( ControlRect layout )
		{
			if( m_Inspector == null )
				m_Inspector = new ImporterPropertiesImportTaskInspector();
			
			m_Inspector.Draw( SelfSerializedObject, layout );
			SelfSerializedObject.ApplyModifiedProperties();
		}
		
		public override int GetHashCode()
		{
			return (m_ConstrainProperties.GetHashCode() * 31) +
			       ((m_ImporterReference != null ? m_ImporterReference.GetHashCode() : 0) * 31) +
			       (ImportTaskName.GetHashCode() * 31);
		}
	}
}