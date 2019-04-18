using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public class AssetDetailList : TreeView
	{
		private class AssetViewItemMenuContext
		{
			public List<IImportProcessModule> m_Modules;
			public List<AssetViewItem> m_Items;

			public AssetViewItemMenuContext( List<IImportProcessModule> modules, List<AssetViewItem> items )
			{
				m_Modules = modules;
				m_Items = items;
			}
		}
		
		private static readonly Color k_ConformFailColor = new Color( 1f, 0.5f, 0.5f );
		
		public AuditProfile m_Profile;
		private readonly List<AssetViewItem> m_SelectedItems = new List<AssetViewItem>();
		internal PropertyDetailList m_PropertyList;

		public AssetDetailList( TreeViewState state ) : base( state )
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
				Debug.LogError( "Must set a Profile before Building the AssetDetailList" );
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
			AssetViewItem assetsFolder = new AssetViewItem( activePath.GetHashCode(), 0, activePath, true );
			if( assetsFolder.children == null )
				assetsFolder.children = new List<TreeViewItem>();
			root.AddChild( assetsFolder );

			Dictionary<int, AssetViewItem> items = new Dictionary<int, AssetViewItem>
			{
				{activePath.GetHashCode(), assetsFolder}
			};

			foreach( var assetPath in associatedAssets )
			{
				// split the path to generate folder structure
				string path = assetPath.Substring( 7 );
				var strings = path.Split( new[] {'/'}, StringSplitOptions.None );
				activePath = "Assets";

				AssetViewItem active = assetsFolder;
				
				List<IConformObject> conformData = profile.GetConformData( assetPath );
				bool result = true;
				for( int i = 0; i < conformData.Count; ++i )
				{
					if( conformData[i].Conforms == false )
					{
						result = false;
						break;
					}
				}
				
				if( !result && assetsFolder.conforms )
					assetsFolder.conforms = false;
				
				AssetImporter assetImporter = AssetImporter.GetAtPath( assetPath );
				SerializedObject assetImporterSO = new SerializedObject( assetImporter );

				// the first entries have lower depth
				for( int i = 0; i < strings.Length; i++ )
				{
					activePath += "/" + strings[i];
					int id = activePath.GetHashCode();

					if( i == strings.Length - 1 )
					{
						AssetViewItem item = new AssetViewItem( id, i + 1, strings[i], result )
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
						AssetViewItem item;
						if( !items.TryGetValue( id, out item ) )
						{
							item = new AssetViewItem( id, i + 1, strings[i], result )
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
			string typeFilter;

			// TODO see if there is a way to merge multiple modules? or have to do multiple passes?
			m_Profile.m_ImporterModule.GetSearchFilter( out typeFilter, ignorePaths );

			string[] GUIDs = AssetDatabase.FindAssets( typeFilter );
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
			AssetViewItem item = args.item as AssetViewItem;
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
				AssetViewItem item = FindItem( selectedIds[i], rootItem ) as AssetViewItem;
				if( item != null )
				{
					m_SelectedItems.Add( item );
				}
			}
			
			m_PropertyList.SetSelection( m_SelectedItems );
		}
		
		protected override void DoubleClickedItem( int id )
		{
			base.DoubleClickedItem( id );
			AssetViewItem pathItem = FindItem( id, rootItem ) as AssetViewItem;
			if( pathItem == null )
				return;
			
			Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( pathItem.path );
			EditorGUIUtility.PingObject( Selection.activeObject );
		}

		protected override void ContextClickedItem( int id )
		{
			AssetViewItem contextItem = FindItem( id, rootItem ) as AssetViewItem;
			if( contextItem == null )
			{
				Debug.LogError( "ContextMenu on unknown ID" );
				return;
			}

			List<AssetViewItem> selectedItems = new List<AssetViewItem>{ contextItem };
			for( int i = 0; i < m_SelectedItems.Count; ++i )
			{
				if( id == m_SelectedItems[i].id )
				{
					selectedItems = new List<AssetViewItem>( m_SelectedItems );
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
			
			// TODO get list of possible selections from modules
			// TODO only list modules that do not conform
			GenericMenu menu = new GenericMenu();
			menu.AddItem( new GUIContent( m_Profile.m_ImporterModule.AssetMenuFixString ), false, ContextMenuSelectionCallback, 
				new AssetViewItemMenuContext( new List<IImportProcessModule>{ m_Profile.m_ImporterModule }, selectedItems ) );
			menu.AddItem( new GUIContent( m_Profile.m_PreprocessorModule.AssetMenuFixString ), false, ContextMenuSelectionCallback, 
				new AssetViewItemMenuContext( new List<IImportProcessModule>{ m_Profile.m_PreprocessorModule }, selectedItems ) );
			menu.AddItem( new GUIContent( "All" ), false, ContextMenuSelectionCallback, 
				new AssetViewItemMenuContext( new List<IImportProcessModule>{ m_Profile.m_ImporterModule, m_Profile.m_PreprocessorModule }, selectedItems ) );
			menu.ShowAsContext();
		}

		private void ContextMenuSelectionCallback( object ctx )
		{
			AssetViewItemMenuContext menuContext = ctx as AssetViewItemMenuContext;
			if( menuContext == null )
			{
				Debug.LogError( "Incorrect context received from AssetViewItem context menu" );
				return;
			}
			
			List<AssetViewItem> assets = new List<AssetViewItem>();
			List<AssetViewItem> folders = new List<AssetViewItem>();
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
					foreach( IConformObject data in assets[i].conformData )
					{
						if( data.GetType() == conformObjectType )
							SetConformObjectRecursive( data, true, conformObjectType );
					}
				}
			}
			
			List<AssetViewItem> checkFoldersForConform = new List<AssetViewItem>();

			for( int i = 0; i < assets.Count; ++i )
			{
				assets[i].Refresh();
				AssetViewItem parent = assets[i].parent as AssetViewItem;
				if( parent != null & checkFoldersForConform.Contains( parent ) == false )
					checkFoldersForConform.Add( parent );
			}
			
			for( int i = 0; i < folders.Count; ++i )
			{
				if( folders[i].conformData == null )
				{
					folders[i].conforms = true;
					folders[i].Refresh();
					AssetViewItem parent = folders[i].parent as AssetViewItem;
					if( parent != null && parent.conforms == false && checkFoldersForConform.Contains( parent ) == false )
						checkFoldersForConform.Add( parent );
				}
			}

			while( checkFoldersForConform.Count > 0 )
			{
				bool conforms = true;
				for( int i = 0; i < checkFoldersForConform[0].children.Count; ++i )
				{
					AssetViewItem item = checkFoldersForConform[0].children[i] as AssetViewItem;
					if( item != null && item.conforms == false )
						conforms = false;
				}

				if( conforms )
				{
					checkFoldersForConform[0].conforms = true;
					AssetViewItem parent = checkFoldersForConform[0].parent as AssetViewItem;
					if( parent != null & checkFoldersForConform.Contains( parent ) == false )
						checkFoldersForConform.Add( parent );
				}
				
				checkFoldersForConform.RemoveAt( 0 );
			}
				
			m_PropertyList.Reload();
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
	}

}