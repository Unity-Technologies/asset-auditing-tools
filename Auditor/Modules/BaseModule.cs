using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	[System.Serializable]
	public abstract class BaseModule : IImportProcessModule
	{
		protected List<string> m_AssetsToForceApply = new List<string>();
		protected string m_SearchFilter;

		public virtual bool CanProcess( AssetImporter item )
		{
			return true;
		}
		
		public abstract string AssetMenuFixString { get; }

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

		public abstract List<IConformObject> GetConformObjects( string asset, AuditProfile profile );

		public virtual bool GetSearchFilter( out string typeFilter, List<string> ignoreAssetPaths )
		{
			typeFilter = m_SearchFilter;
			return true;
		}

		public abstract Type GetConformObjectType();
		
		public virtual bool Apply( AssetImporter importer, AuditProfile fromProfile )
		{
			if( CanProcess( importer ) == false )
				return false;
			m_AssetsToForceApply.Remove( importer.assetPath );
			return true;
		}
	}
}