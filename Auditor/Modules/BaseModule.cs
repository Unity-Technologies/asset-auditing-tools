using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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

		public bool IsManuallyProcessing( AssetImporter item )
		{
			return m_AssetsToForceApply.Contains( item.assetPath );
		}
		
		public virtual List<IConformObject> GetConformObjects( string asset, AuditProfile profile )
		{
			return new List<IConformObject>(0);
		}

		public virtual bool GetSearchFilter( out string typeFilter, List<string> ignoreAssetPaths )
		{
			typeFilter = m_SearchFilter;
			return true;
		}

		// TODO split up
		public virtual void FixCallback( AssetDetailList calledFromTreeView, object context )
		{
			List<AssetViewItem> selected = context as List<AssetViewItem>;
			if( selected == null || selected.Count == 0 )
			{
				AssetViewItem selectedItem = context as AssetViewItem;
				if( selectedItem != null )
				{
					selected = new List<AssetViewItem> { selectedItem };
				}
			}

			if( selected == null )
			{
				Debug.LogError( "Something went wrong with the selected items" );
				return;
			}

			List<AssetViewItem> assets = new List<AssetViewItem>();
			List<AssetViewItem> folders = new List<AssetViewItem>();
			for( int i = 0; i < selected.Count; ++i )
			{
				if( selected[i].isAsset == false )
				{
					GetAssetItems( selected[i], assets, folders );
					if( folders.Contains( selected[i] ) == false )
						folders.Add( selected[i] );
				}
				else if( selected[i].conforms == false && assets.Contains( selected[i] ) == false )
					assets.Add( selected[i] );
			}

			if( assets.Count > 0 )
			{
				AssetDatabase.StartAssetEditing();
				for( int i=0; i<assets.Count; ++i )
				{
					m_AssetsToForceApply.Add( assets[i].path );
					assets[i].ReimportAsset();
				}
				AssetDatabase.StopAssetEditing();

				for( int i = 0; i < assets.Count; ++i )
				{
					// TODO confirm that it now conforms, currently just set everything as Conforms
					foreach( IConformObject data in assets[i].conformData )
					{
						Type dt = data.GetType();
						Type cot = GetConformObjectType();
						if( data.GetType() == GetConformObjectType() )
							SetAllConformObjects( data, true );
					}
					
					assets[i].Refresh();
				}
				
				for( int i = 0; i < folders.Count; ++i )
				{
					if( folders[i].conformData == null )
					{
						folders[i].conforms = true;
					}
					else
					{
						foreach( IConformObject data in folders[i].conformData )
						{
							if( data.GetType() == GetConformObjectType() )
								SetAllConformObjects( data, true );
						}
					}
					
					assets[i].Refresh();
				}
				
				calledFromTreeView.m_PropertyList.Reload();
			}
			else
				Debug.LogError( "Could not fix Asset with no Assets selected." );
		}

		static void GetAssetItems( AssetViewItem item , List<AssetViewItem> assets, List<AssetViewItem> folders )
		{
			for( int i = 0; i < item.children.Count; ++i )
			{
				AssetViewItem avi = item.children[i] as AssetViewItem;
				if( avi.isAsset == false && folders.Contains( avi ) == false )
				{
					folders.Add( avi );
					GetAssetItems( avi, assets, folders );
				}
				else if( avi.conforms == false && assets.Contains( avi ) == false )
					assets.Add( avi );
			}
		}

		private void SetAllConformObjects( IConformObject obj, bool value )
		{
			obj.Conforms = value;
			foreach( IConformObject data in obj.SubObjects )
			{
				if( data.GetType() == GetConformObjectType() )
					SetAllConformObjects( data, value );
			}
		}

		protected abstract Type GetConformObjectType();
		
		public virtual bool Apply( AssetImporter importer, AuditProfile fromProfile )
		{
			if( CanProcess( importer ) == false )
				return false;
			m_AssetsToForceApply.Remove( importer.assetPath );
			return true;
		}
	}
}