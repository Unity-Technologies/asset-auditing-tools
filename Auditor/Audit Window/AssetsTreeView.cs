using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public class AssetsTreeView : TreeView
	{
		private class AssetViewItemMenuContext
		{
			public List<IImportProcessModule> m_Modules;
			public List<AssetTreeViewItem> m_Items;

			public AssetViewItemMenuContext( List<IImportProcessModule> modules, List<AssetTreeViewItem> items )
			{
				m_Modules = modules;
				m_Items = items;
			}
		}
		
		private static readonly Color k_ConformFailColor = new Color( 1f, 0.5f, 0.5f );
		
		public AuditProfile m_Profile;
		private readonly List<AssetTreeViewItem> m_SelectedItems = new List<AssetTreeViewItem>();
		internal ModularDetailTreeView m_ModularTreeView;

		public AssetsTreeView( TreeViewState state ) : base( state )
		{
			showBorder = true;
		}

		protected override TreeViewItem BuildRoot()
		{
			TreeViewItem root = new TreeViewItem( -1, -1 )
			{
				children = new List<TreeViewItem>()
			};

			if( m_Profile != null )
				GenerateTreeElements( m_Profile, root );
			else
				Debug.LogError( "Must set a Profile before Building the AssetsTreeView" );
			return root;
		}

		private void GenerateTreeElements( AuditProfile profile, TreeViewItem root )
		{
			List<string> associatedAssets = GetFilteredAssets( profile );

			// early out if there are no affected assets
			if( associatedAssets.Count == 0 )
			{
				Debug.Log( "No matching assets found for " + profile.name );
				return;
			}

			string activePath = "Assets";
			AssetTreeViewItem assetsTreeFolder = new AssetTreeViewItem( activePath.GetHashCode(), 0, activePath, true );
			if( assetsTreeFolder.children == null )
				assetsTreeFolder.children = new List<TreeViewItem>();
			root.AddChild( assetsTreeFolder );

			Dictionary<int, AssetTreeViewItem> items = new Dictionary<int, AssetTreeViewItem>
			{
				{activePath.GetHashCode(), assetsTreeFolder}
			};

			foreach( var assetPath in associatedAssets )
			{
				// split the path to generate folder structure
				string path = assetPath.Substring( 7 );
				var strings = path.Split( new[] {'/'}, StringSplitOptions.None );
				activePath = "Assets";

				AssetTreeViewItem active = assetsTreeFolder;
				
				List<ModuleConformData> conformData = profile.GetConformData( assetPath );
				bool result = true;
				for( int i = 0; i < conformData.Count; ++i )
				{
					if( conformData[i].Conforms == false )
					{
						result = false;
						break;
					}
				}
				
				if( !result && assetsTreeFolder.conforms )
					assetsTreeFolder.conforms = false;
				
				AssetImporter assetImporter = AssetImporter.GetAtPath( assetPath );
				SerializedObject assetImporterSO = new SerializedObject( assetImporter );

				// the first entries have lower depth
				for( int i = 0; i < strings.Length; i++ )
				{
					activePath += "/" + strings[i];
					int id = activePath.GetHashCode();

					if( i == strings.Length - 1 )
					{
						AssetTreeViewItem item = new AssetTreeViewItem( id, i + 1, strings[i], result )
						{
							icon = AssetDatabase.GetCachedIcon( assetPath ) as Texture2D,
							path = activePath,
							conformData = conformData,
							assetObject = assetImporterSO,
							isAsset = true
						};
						active.AddChild( item );
						active = item;
						if( items.ContainsKey( id ))
							Debug.LogError( "id already in for " + activePath );
						items.Add( id, item );
					}
					else
					{
						AssetTreeViewItem item;
						if( !items.TryGetValue( id, out item ) )
						{
							item = new AssetTreeViewItem( id, i + 1, strings[i], result )
							{
								path = activePath,
								icon = AssetDatabase.GetCachedIcon( activePath ) as Texture2D
							};
							active.AddChild( item );
							items.Add( id, item );
						}
						else if( result == false )
						{
							if( item.conforms )
								item.conforms = false;
						}

						active = item;
					}
				}
			}
		}

		private List<string> GetFilteredAssets( AuditProfile profile )
		{
			List<string> associatedAssets = new List<string>();
			List<string> ignorePaths = new List<string>();
			string searchFilter;

			List<string> GUIDs;
			
			// TODO this is probably inefficient, profile and see if there is a more efficient design needed
			if( m_Profile.m_Modules.Count > 0 )
			{
				m_Profile.m_Modules[0].GetSearchFilter( out searchFilter, ignorePaths );
				GUIDs = new List<string>( AssetDatabase.FindAssets( searchFilter ) );
				
				for( int i = 1; i < m_Profile.m_Modules.Count; ++i )
				{
					m_Profile.m_Modules[i].GetSearchFilter( out searchFilter, ignorePaths );
					string[] moduleGUIDs = AssetDatabase.FindAssets( searchFilter );
					for( int m = GUIDs.Count - 1; m >= 0; --m )
					{
						bool guidInModule = false;
						foreach( string moduleGuiD in moduleGUIDs )
						{
							if( GUIDs[m] == moduleGuiD )
							{
								guidInModule = true;
								break;
							}
						}
						if( guidInModule == false )
							GUIDs.RemoveAt( m );
					}
				}
			}
			else
			{
				GUIDs = new List<string>( AssetDatabase.FindAssets( "" ) );
			}

			//string[] GUIDs = AssetDatabase.FindAssets( typeFilter );
			foreach( var assetGUID in GUIDs )
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( assetGUID );
				
				// some items may appear twice, due to sub assets, e.g. Sprite
				if( ignorePaths.Contains( assetPath ) || associatedAssets.Contains( assetPath ) )
					continue;

				if( Filter.Conforms( assetPath, profile.m_Filters ) )
					associatedAssets.Add( assetPath );
			}

			return associatedAssets;
		}

		protected override void RowGUI( RowGUIArgs args )
		{
			AssetTreeViewItem item = args.item as AssetTreeViewItem;
			if( item != null )
			{
				float num = GetContentIndent( item ) + extraSpaceBeforeIconAndLabel;

				Rect r = args.rowRect;
				if( args.item.icon != null )
				{
					r.xMin += num;
					r.width = r.height;
					GUI.DrawTexture( r, args.item.icon );
				}

				Color old = GUI.color;
				if( item.conforms == false )
					GUI.color = k_ConformFailColor;

				r = args.rowRect;
				r.xMin += num;
				if( args.item.icon != null )
				{
					r.x += r.height + 2f;
					r.width -= r.height + 2f;
				}

				EditorGUI.LabelField( r, args.label );
				GUI.color = old;
			}
			else
			{
				base.RowGUI( args );
			}
		}

		protected override void SelectionChanged( IList<int> selectedIds )
		{
			base.SelectionChanged( selectedIds );
			SetupSelection( selectedIds );
		}

		internal void SetupSelection( IList<int> selectedIds )
		{
			m_SelectedItems.Clear();

			for( int i = 0; i < selectedIds.Count; ++i )
			{
				AssetTreeViewItem item = FindItem( selectedIds[i], rootItem ) as AssetTreeViewItem;
				if( item != null )
				{
					m_SelectedItems.Add( item );
				}
			}
			
			m_ModularTreeView.SetSelectedAssetItems( m_SelectedItems );
		}
		
		protected override void DoubleClickedItem( int id )
		{
			base.DoubleClickedItem( id );
			AssetTreeViewItem pathItem = FindItem( id, rootItem ) as AssetTreeViewItem;
			if( pathItem == null )
				return;
			
			Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( pathItem.path );
			EditorGUIUtility.PingObject( Selection.activeObject );
		}

		protected override void ContextClickedItem( int id )
		{
			AssetTreeViewItem contextItem = FindItem( id, rootItem ) as AssetTreeViewItem;
			if( contextItem == null )
			{
				Debug.LogError( "ContextMenu on unknown ID" );
				return;
			}

			List<AssetTreeViewItem> selectedItems = new List<AssetTreeViewItem>{ contextItem };
			for( int i = 0; i < m_SelectedItems.Count; ++i )
			{
				if( id == m_SelectedItems[i].id )
				{
					selectedItems = new List<AssetTreeViewItem>( m_SelectedItems );
					break;
				}
			}

			for( int i = selectedItems.Count - 1; i >= 0; --i )
			{
				if( selectedItems[i].conforms )
					selectedItems.RemoveAt( i );
			}

			if( selectedItems.Count == 0 )
				return;
			
			GenericMenu menu = new GenericMenu();
			List<IImportProcessModule> all = new List<IImportProcessModule>(m_Profile.m_Modules.Count);
			for( int i = 0; i < m_Profile.m_Modules.Count; ++i )
			{
				menu.AddItem( new GUIContent( m_Profile.m_Modules[i].AssetMenuFixString ), false, ContextMenuSelectionCallback, 
					new AssetViewItemMenuContext( new List<IImportProcessModule>{ m_Profile.m_Modules[i] }, selectedItems ) );
				all.Add( m_Profile.m_Modules[i] );
			}
			menu.AddItem( new GUIContent( "All" ), false, ContextMenuSelectionCallback,
				new AssetViewItemMenuContext( all, selectedItems ) );
			menu.ShowAsContext();
		}

		private void ContextMenuSelectionCallback( object ctx )
		{
			AssetViewItemMenuContext menuContext = ctx as AssetViewItemMenuContext;
			if( menuContext == null )
			{
				Debug.LogError( "Incorrect context received from AssetTreeViewItem context menu" );
				return;
			}
			
			List<AssetTreeViewItem> assets = new List<AssetTreeViewItem>();
			List<AssetTreeViewItem> folders = new List<AssetTreeViewItem>();
			for( int i = 0; i < menuContext.m_Items.Count; ++i )
			{
				if( menuContext.m_Items[i].isAsset == false )
				{
					GetAssetItems( menuContext.m_Items[i], assets, folders );
					if( folders.Contains( menuContext.m_Items[i] ) == false )
						folders.Add( menuContext.m_Items[i] );
				}
				else if( menuContext.m_Items[i].conforms == false && assets.Contains( menuContext.m_Items[i] ) == false )
					assets.Add( menuContext.m_Items[i] );
			}
			
			List<string> assetPaths = new List<string>(assets.Count);
			for( int i = 0; i < assets.Count; ++i )
				assetPaths.Add( assets[i].path );
			
			for( int i = 0; i < menuContext.m_Modules.Count; ++i )
			{
				if( menuContext.m_Modules[i] == null )
				{
					Debug.LogError( "Module became null during window use, report issue" );
					continue;
				}
				
				menuContext.m_Modules[i].SetManuallyProcessing( assetPaths, true );
			}
			
			// reimport all the assets -> triggering the modules to process them (including any that profiles always do)
			AssetDatabase.StartAssetEditing();
			for( int i=0; i<assets.Count; ++i )
			{
				assets[i].ReimportAsset();
			}
			AssetDatabase.StopAssetEditing();

			foreach( IImportProcessModule module in menuContext.m_Modules )
			{
				Type conformObjectType = module.GetConformObjectType();
				for( int i = 0; i < assets.Count; ++i )
				{
					// TODO confirm that it now conforms, currently just set everything as Conforms
					foreach( ModuleConformData data in assets[i].conformData )
					{
						foreach( IConformObject conformObject in data.m_ConformObjects )
						{
							if( conformObject.GetType() == conformObjectType )
								SetConformObjectRecursive( conformObject, true, conformObjectType );
						}
					}
				}
			}
			
			List<AssetTreeViewItem> checkFoldersForConform = new List<AssetTreeViewItem>();

			for( int i = 0; i < assets.Count; ++i )
			{
				assets[i].Refresh();
				AssetTreeViewItem parent = assets[i].parent as AssetTreeViewItem;
				if( parent != null & checkFoldersForConform.Contains( parent ) == false )
					checkFoldersForConform.Add( parent );
			}
			
			for( int i = 0; i < folders.Count; ++i )
			{
				if( folders[i].conformData == null )
				{
					folders[i].conforms = true;
					folders[i].Refresh();
					AssetTreeViewItem parent = folders[i].parent as AssetTreeViewItem;
					if( parent != null && parent.conforms == false && checkFoldersForConform.Contains( parent ) == false )
						checkFoldersForConform.Add( parent );
				}
			}

			while( checkFoldersForConform.Count > 0 )
			{
				bool conforms = true;
				for( int i = 0; i < checkFoldersForConform[0].children.Count; ++i )
				{
					AssetTreeViewItem item = checkFoldersForConform[0].children[i] as AssetTreeViewItem;
					if( item != null && item.conforms == false )
						conforms = false;
				}

				if( conforms )
				{
					checkFoldersForConform[0].conforms = true;
					AssetTreeViewItem parent = checkFoldersForConform[0].parent as AssetTreeViewItem;
					if( parent != null & checkFoldersForConform.Contains( parent ) == false )
						checkFoldersForConform.Add( parent );
				}
				
				checkFoldersForConform.RemoveAt( 0 );
			}
				
			m_ModularTreeView.Reload();
		}
		
		private void SetConformObjectRecursive( IConformObject obj, bool value, Type restrictToType )
		{
			obj.Conforms = value;
			foreach( IConformObject data in obj.SubObjects )
			{
				if( data.GetType() == restrictToType )
					SetConformObjectRecursive( data, value, restrictToType );
			}
		}
		
		static void GetAssetItems( AssetTreeViewItem item , List<AssetTreeViewItem> assets, List<AssetTreeViewItem> folders )
		{
			for( int i = 0; i < item.children.Count; ++i )
			{
				AssetTreeViewItem avi = item.children[i] as AssetTreeViewItem;
				if( avi.isAsset == false && folders.Contains( avi ) == false )
				{
					folders.Add( avi );
					GetAssetItems( avi, assets, folders );
				}
				else if( avi.conforms == false && assets.Contains( avi ) == false )
					assets.Add( avi );
			}
		}
	}

}