using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AssetTools.GUIUtility;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	[System.Serializable]
	public abstract class BaseImportTask : ScriptableObject, IImportTask
	{
		protected List<string> m_AssetsToForceApply = new List<string>();
		protected string m_SearchFilter = "";
		
		private SerializedObject m_SelfSerializedObject = null;

		protected SerializedObject SelfSerializedObject
		{
			get
			{
				if( m_SelfSerializedObject == null )
					m_SelfSerializedObject = new SerializedObject( this );
				return m_SelfSerializedObject;
			}
		}
		
		public abstract string AssetMenuFixString { get; }

		public abstract bool CanProcess( AssetImporter item );

		public bool IsManuallyProcessing( AssetImporter item )
		{
			return m_AssetsToForceApply.Contains( item.assetPath );
		}

		public void SetManuallyProcessing( List<string> assetPaths, bool value )
		{
			for( int i = 0; i < assetPaths.Count; ++i )
			{
				if( value && m_AssetsToForceApply.Contains( assetPaths[i] ) == false )
				{
					m_AssetsToForceApply.Add( assetPaths[i] );
				}
				else if( !value )
				{
					m_AssetsToForceApply.Remove( assetPaths[i] );
				}
			}
		}
		
		public virtual void DrawGUI( ControlRect layout )
		{
			var type = GetType();
			var so = new SerializedObject(this);
			var p = so.GetIterator();
			p.Next(true);
			while (p.Next(false))
			{
				var prop = type.GetField(p.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				if(prop != null)
					EditorGUI.PropertyField(layout.Get(), p, false);
			}
			so.ApplyModifiedProperties();
		}

		public abstract List<IConformObject> GetConformObjects( string asset, ImportDefinitionProfile profile );
		
		public abstract Type GetConformObjectType();

		public virtual bool GetSearchFilter( out string searchFilter, List<string> ignoreAssetPaths )
		{
			searchFilter = m_SearchFilter;
			return true;
		}

		public virtual bool Apply( AssetImporter importer, ImportDefinitionProfile fromProfile )
		{
			if( CanProcess( importer ) == false )
				return false;
			m_AssetsToForceApply.Remove( importer.assetPath );
			return true;
		}
	}
}