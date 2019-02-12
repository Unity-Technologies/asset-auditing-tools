using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	internal class AssetDetailList : TreeView
	{
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
			var root = new TreeViewItem( -1, -1 );
			root.children = new List<TreeViewItem>();

			if( m_Profile != null )
				GenerateTreeElements( m_Profile, root );
			else
				Debug.LogError( "Must set a Profile before Building the AssetDetailList" );
			return root;
		}

		void GenerateTreeElements( AuditProfile profile, TreeViewItem root )
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

			Dictionary<int, AssetViewItem> items = new Dictionary<int, AssetViewItem>();
			items.Add( activePath.GetHashCode(), assetsFolder );
			
			foreach( var assetPath in associatedAssets )
			{
				// split the path to generate folder structure
				string path = assetPath.Substring( 7 );
				var strings = path.Split( new[] {'/'}, StringSplitOptions.None );
				activePath = "Assets";

				AssetViewItem active = assetsFolder;
				
				// for each module
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
				
				AssetImporter assetimporter = AssetImporter.GetAtPath( assetPath );
				SerializedObject assetImporterSO = new SerializedObject( assetimporter );

				// the first entries have lower depth
				for( int i = 0; i < strings.Length; i++ )
				{
					activePath += "/" + strings[i];
					int id = activePath.GetHashCode();

					if( i == strings.Length - 1 )
					{
						AssetViewItem item = new AssetViewItem( id, i + 1, strings[i], result );
						item.icon = AssetDatabase.GetCachedIcon( assetPath ) as Texture2D;
						item.path = activePath;
						item.conformData = conformData;
						item.assetObject = assetImporterSO;
						item.isAsset = true;
						active.AddChild( item );
						active = item;
						if( items.ContainsKey( id ))
							Debug.LogError( "id already in for " + activePath );
						items.Add( id, item );
					}
					else
					{
						AssetViewItem item = null;
						if( !items.TryGetValue( id, out item ) )
						{
							item = new AssetViewItem( id, i + 1, strings[i], result );
							item.path = activePath;
							item.icon = AssetDatabase.GetCachedIcon( activePath ) as Texture2D;
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
		
		List<string> GetFilteredAssets( AuditProfile profile )
		{
			List<string> associatedAssets = new List<string>();
			string ignorePath = "";
			string findFilter = "";
			
			if( profile.m_ImporterReference != null )
			{
				switch( profile.GetAssetType() )
				{
					case AssetType.Texture:
						findFilter = "t:Texture";
						break;
					case AssetType.Model:
						findFilter = "t:GameObject";
						break;
					case AssetType.Audio:
						findFilter = "t:AudioClip";
						break;
					case AssetType.Folder:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				ignorePath = AssetDatabase.GetAssetPath( profile.m_ImporterReference );
			}

			string[] guids = AssetDatabase.FindAssets( findFilter );
			foreach( var assetGUID in guids )
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( assetGUID );
				
				// some items may appear twice, due to sub assets, e.g. Sprite
				if( assetPath == ignorePath || associatedAssets.Contains( assetPath ) )
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
				float num = this.GetContentIndent( item ) + this.extraSpaceBeforeIconAndLabel;

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
				AssetViewItem item = this.FindItem( selectedIds[i], rootItem ) as AssetViewItem;
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
			TreeViewItem item = this.FindItem( id, rootItem );
			if( item != null )
			{
				AssetViewItem pathItem = item as AssetViewItem;
				if( pathItem != null )
				{
					UnityEngine.Object o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( pathItem.path );
					if( o != null )
					{
						Selection.activeObject = o;
						EditorGUIUtility.PingObject( o );
					}
				}
			}
		}

		protected override void ContextClickedItem( int id )
		{
			// TODO allow on a folder
			AssetViewItem item = FindItem( id, rootItem ) as AssetViewItem;
			if( item == null || item.conforms )
				return;
			
			GenericMenu menu = new GenericMenu();
			
			// TODO get list of possible selections from modules
			
			menu.AddItem( new GUIContent( "Conform to Template Properties" ), false, FixCallbackImporterProperties, item );
			menu.ShowAsContext();
		}

		void FixCallbackAll( object context )
		{
			FixCallbackImporterProperties( context );
		}

		void FixCallbackImporterProperties( object context )
		{
			if( m_Profile.m_ImporterModule != null )
				m_Profile.m_ImporterModule.FixCallback( this, context );
		}
	}

}