using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	[System.Serializable]
	public class ImporterPropertiesModule
	{
		public UnityEngine.Object ImporterReference
		{
			get { return m_Profile == null ? null : m_Profile.m_ImporterReference; }
		}
		
		public List<string> m_ConstrainProperties = new List<string>();

		[NonSerialized] public List<string> m_ConstrainPropertiesDisplayNames = new List<string>();
		[NonSerialized] public List<SerializedProperty> m_SerialisedProperties = new List<SerializedProperty>();
		private int m_HasProperties = 0;
		
		internal AuditProfile m_Profile = null;
		
		private AssetImporter m_ProfilerAssetImporter;
		public AssetImporter ProfileAssetImporter
		{
			get
			{
				if( m_ProfilerAssetImporter == null )
					m_ProfilerAssetImporter = AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( ImporterReference ) );
				return m_ProfilerAssetImporter;
			}
		}
		
		public int PropertyCount
		{
			get
			{
				if( m_HasProperties == 0 )
					GatherProperties();
				return m_ConstrainProperties == null ? 0 : m_ConstrainProperties.Count;
			}
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
			if( ImporterReference == null )
				return;

			SerializedObject so = new SerializedObject( m_Profile.GetAssetImporter() );
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

			m_ConstrainPropertiesDisplayNames.Clear();
			foreach( SerializedProperty property in m_SerialisedProperties )
			{
				if( m_ConstrainProperties.Contains( property.name ) )
					m_ConstrainPropertiesDisplayNames.Add( property.displayName );
			}

			m_HasProperties = m_ConstrainProperties.Count > 0 ? 1 : -1;
		}
		
		
		internal void TogglePropertySelected( object selectedObject )
		{
			string propertyName = selectedObject as string;
			if( m_ConstrainPropertiesDisplayNames.Contains( propertyName ) )
				RemoveProperty( propertyName, false );
			else
				AddProperty( propertyName, false );
		}
		
		public List<IConformObject> GetConformObjects( string asset )
		{
			AssetImporter assetImporter = AssetImporter.GetAtPath( asset );
			if( ImporterReference == null )
				return new List<IConformObject>(0);
			
			SerializedObject assetImporterSO = new SerializedObject( assetImporter );
			SerializedObject profileImporterSO = new SerializedObject( ProfileAssetImporter );
			
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

		List<IConformObject> CompareSerializedObject( SerializedObject template, SerializedObject asset )
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
		
		internal void CopyProperties( AssetViewItem item )
		{
			if( m_ConstrainProperties.Count > 0 )
			{
				SerializedObject profileSerializedObject = new SerializedObject( ProfileAssetImporter );
				SerializedObject assetImporterSO = new SerializedObject( item.AssetImporter );
				CopyConstrainedProperties( assetImporterSO, profileSerializedObject );
			}
			else
			{
				EditorUtility.CopySerialized( ProfileAssetImporter, item.AssetImporter );
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
		
		internal void FixCallback( AssetDetailList windowDisplay, object context )
		{
			// TODO find out how to multi select
			// TODO if selection is a folder
			
			AssetViewItem selectedNodes = context as AssetViewItem;
			if( selectedNodes != null )
			{
				CopyProperties( selectedNodes );
				foreach( PropertyConformObject data in selectedNodes.conformData )
				{
					data.Conforms = true;
				}
				windowDisplay.m_PropertyList.Reload();
			}
			else
				Debug.LogError( "Could not fix Asset with no Assets selected." );
		}
	}
}