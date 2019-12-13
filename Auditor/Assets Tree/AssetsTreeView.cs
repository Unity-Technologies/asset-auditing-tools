using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public class AssetsTreeView : HierarchyTreeView
	{
		private class AssetViewItemMenuContext
		{
			public List<IImportTask> m_Modules;
			public List<AssetsTreeViewItem> m_Items;

			public AssetViewItemMenuContext( List<IImportTask> modules, List<AssetsTreeViewItem> items )
			{
				m_Modules = modules;
				m_Items = items;
			}
		}
		
		private static readonly Color k_ConformFailColor = new Color( 1f, 0.5f, 0.5f );
		
		public ImportDefinitionProfile m_Profile;
		private readonly List<AssetsTreeViewItem> m_SelectedItems = new List<AssetsTreeViewItem>();
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

		private void GenerateTreeElements( ImportDefinitionProfile profile, TreeViewItem root )
		{
			List<string> associatedAssets = GetFilteredAssets( profile );

			// early out if there are no affected assets
			if( associatedAssets.Count == 0 )
			{
				Debug.Log( "No matching assets found for " + profile.name );
				return;
			}

			string activePath = "Assets";
			AssetsTreeViewItem assetsesTreeFolder = new AssetsTreeViewItem( activePath.GetHashCode(), 0, activePath, true );
			if( assetsesTreeFolder.children == null )
				assetsesTreeFolder.children = new List<TreeViewItem>();
			root.AddChild( assetsesTreeFolder );

			Dictionary<int, AssetsTreeViewItem> items = new Dictionary<int, AssetsTreeViewItem>
			{
				{activePath.GetHashCode(), assetsesTreeFolder}
			};

			foreach( var assetPath in associatedAssets )
			{
				// split the path to generate folder structure
				string path = assetPath.Substring( 7 );
				var strings = path.Split( new[] {'/'}, StringSplitOptions.None );
				activePath = "Assets";

				AssetsTreeViewItem active = assetsesTreeFolder;
				
				List<ConformData> conformData = profile.GetConformData( assetPath );
				bool result = true;
				for( int i = 0; i < conformData.Count; ++i )
				{
					if( conformData[i].Conforms == false )
					{
						result = false;
						break;
					}
				}
				
				if( !result && assetsesTreeFolder.conforms )
					assetsesTreeFolder.conforms = false;
				
				AssetImporter assetImporter = AssetImporter.GetAtPath( assetPath );
				SerializedObject assetImporterSO = new SerializedObject( assetImporter );

				// the first entries have lower depth
				for( int i = 0; i < strings.Length; i++ )
				{
					activePath += "/" + strings[i];
					int id = activePath.GetHashCode();

					if( i == strings.Length - 1 )
					{
						AssetsTreeViewItem item = new AssetsTreeViewItem( id, i + 1, strings[i], result )
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
						AssetsTreeViewItem item;
						if( !items.TryGetValue( id, out item ) )
						{
							item = new AssetsTreeViewItem( id, i + 1, strings[i], result )
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

		private List<string> GetFilteredAssets( ImportDefinitionProfile profile )
		{
			List<string> associatedAssets = new List<string>();
			List<string> ignorePaths = new List<string>();
			string searchFilter;

			List<string> GUIDs;
			
			// TODO this is probably inefficient, profile and see if there is a more efficient design needed
			if( m_Profile.m_ImportTasks.Count > 0 )
			{
				m_Profile.m_ImportTasks[0].GetSearchFilter( out searchFilter, ignorePaths );
				GUIDs = new List<string>( AssetDatabase.FindAssets( searchFilter ) );
				
				for( int i = 1; i < m_Profile.m_ImportTasks.Count; ++i )
				{
					m_Profile.m_ImportTasks[i].GetSearchFilter( out searchFilter, ignorePaths );
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

				if( Filter.Conforms( assetPath, profile.GetFilters() ) )
					associatedAssets.Add( assetPath );
			}

			return associatedAssets;
		}

		protected override void RowGUI( RowGUIArgs args )
		{
			AssetsTreeViewItem item = args.item as AssetsTreeViewItem;
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
				AssetsTreeViewItem item = FindItem( selectedIds[i], rootItem ) as AssetsTreeViewItem;
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
			AssetsTreeViewItem pathItem = FindItem( id, rootItem ) as AssetsTreeViewItem;
			if( pathItem == null )
				return;
			
			Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( pathItem.path );
			EditorGUIUtility.PingObject( Selection.activeObject );
		}

		protected override void ContextClickedItem( int id )
		{
			AssetsTreeViewItem contextItem = FindItem( id, rootItem ) as AssetsTreeViewItem;
			if( contextItem == null )
			{
				Debug.LogError( "ContextMenu on unknown ID" );
				return;
			}

			List<AssetsTreeViewItem> selectedItems = new List<AssetsTreeViewItem>{ contextItem };
			for( int i = 0; i < m_SelectedItems.Count; ++i )
			{
				if( id == m_SelectedItems[i].id )
				{
					selectedItems = new List<AssetsTreeViewItem>( m_SelectedItems );
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
			List<IImportTask> all = new List<IImportTask>(m_Profile.m_ImportTasks.Count);
			for( int i = 0; i < m_Profile.m_ImportTasks.Count; ++i )
			{
				menu.AddItem( new GUIContent( m_Profile.m_ImportTasks[i].AssetMenuFixString ), false, ContextMenuSelectionCallback, 
					new AssetViewItemMenuContext( new List<IImportTask>{ m_Profile.m_ImportTasks[i] }, selectedItems ) );
				all.Add( m_Profile.m_ImportTasks[i] );
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
			
			List<AssetsTreeViewItem> assets = new List<AssetsTreeViewItem>();
			List<AssetsTreeViewItem> folders = new List<AssetsTreeViewItem>();
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

			foreach( IImportTask module in menuContext.m_Modules )
			{
				Type conformObjectType = module.GetConformObjectType();
				for( int i = 0; i < assets.Count; ++i )
				{
					// TODO confirm that it now conforms, currently just set everything as Conforms
					foreach( ConformData data in assets[i].conformData )
					{
						foreach( IConformObject conformObject in data.ConformObjects )
						{
							if( conformObject.GetType() == conformObjectType )
								SetConformObjectRecursive( conformObject, true, conformObjectType );
						}
					}
				}
			}
			
			List<AssetsTreeViewItem> checkFoldersForConform = new List<AssetsTreeViewItem>();

			for( int i = 0; i < assets.Count; ++i )
			{
				assets[i].Refresh();
				AssetsTreeViewItem parent = assets[i].parent as AssetsTreeViewItem;
				if( parent != null & checkFoldersForConform.Contains( parent ) == false )
					checkFoldersForConform.Add( parent );
			}
			
			for( int i = 0; i < folders.Count; ++i )
			{
				if( folders[i].conformData == null )
				{
					folders[i].conforms = true;
					folders[i].Refresh();
					AssetsTreeViewItem parent = folders[i].parent as AssetsTreeViewItem;
					if( parent != null && parent.conforms == false && checkFoldersForConform.Contains( parent ) == false )
						checkFoldersForConform.Add( parent );
				}
			}

			while( checkFoldersForConform.Count > 0 )
			{
				bool conforms = true;
				for( int i = 0; i < checkFoldersForConform[0].children.Count; ++i )
				{
					AssetsTreeViewItem item = checkFoldersForConform[0].children[i] as AssetsTreeViewItem;
					if( item != null && item.conforms == false )
						conforms = false;
				}

				if( conforms )
				{
					checkFoldersForConform[0].conforms = true;
					AssetsTreeViewItem parent = checkFoldersForConform[0].parent as AssetsTreeViewItem;
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
		
		static void GetAssetItems( AssetsTreeViewItem item , List<AssetsTreeViewItem> assets, List<AssetsTreeViewItem> folders )
		{
			for( int i = 0; i < item.children.Count; ++i )
			{
				AssetsTreeViewItem avi = item.children[i] as AssetsTreeViewItem;
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