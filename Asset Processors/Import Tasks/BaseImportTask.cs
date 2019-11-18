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
		public enum ProcessingType { Pre, Post }
		
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

		public virtual int Version
		{
			get { return 0; }
		}

		public virtual int MaximumCount
		{
			get { return 1; }
		}

		public virtual string ImportTaskName
		{
			get { return this.GetType().Name; }
		}
		
		public abstract string AssetMenuFixString { get; }

		public abstract ProcessingType TaskProcessType { get; }

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

		public void SetManuallyProcessing( string assetPath, bool value )
		{
			if( value && m_AssetsToForceApply.Contains( assetPath ) == false )
			{
				m_AssetsToForceApply.Add( assetPath );
			}
			else if( !value )
			{
				m_AssetsToForceApply.Remove( assetPath );
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
		public virtual void PreprocessTask( ImportContext context, ImportDefinitionProfile profile )
		{
			UserDataSerialization data = UserDataSerialization.Get( context.AssetPath );
			string profileGuid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( profile ) );
			data.UpdateProcessing( new UserDataSerialization.ImportTaskData( profileGuid, ImportTaskName, Version ) );
		}

		public virtual bool Apply( ImportContext context, ImportDefinitionProfile fromProfile )
		{
			if( CanProcess( context.Importer ) == false )
				return false;
			return true;
		}
	}
}