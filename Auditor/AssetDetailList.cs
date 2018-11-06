using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	internal class AssetViewItem : TreeViewItem
	{
		internal bool conforms { get; set; }

		public string path = "";
		public bool isAsset;

		public List<PropertyConformData> conformData;
		public SerializedObject assetObject;
		public AuditProfile profile;

		private AssetImporter assetImporter;

		internal AssetViewItem( int id, int depth, string displayName, bool assetConforms ) : base( id, depth, displayName )
		{
			conforms = assetConforms;
		}

		public void Apply()
		{
			if( !isAsset )
				return;
			
			if( assetImporter == null )
				assetImporter = AssetImporter.GetAtPath( path );
			
			assetImporter.SaveAndReimport();
			icon = AssetDatabase.GetCachedIcon( path ) as Texture2D;
			
			if( !conforms )
			{
				conforms = true;
				for( int i = 0; i < conformData.Count; ++i )
				{
					if( conformData[i].Conforms == false )
					{
						conforms = false;
						break;
					}
				}
			}
		}

		public void CopyProperties()
		{
			AssetImporter profileImporter = profile.GetAssetImporter();
			if( assetImporter == null )
				assetImporter = AssetImporter.GetAtPath( path );
			
			if( profile.m_ConstrainProperties.Count > 0 )
			{
				SerializedObject profileSerializedObject = new SerializedObject( profileImporter );
				SerializedObject assetImporterSO = new SerializedObject( assetImporter );
				CopyConstrainedProperties( assetImporterSO, profileSerializedObject, profile );
			}
			else
			{
				EditorUtility.CopySerialized( profileImporter, assetImporter );
			}

			conforms = true;
			Apply();
		}
		
		private static void CopyConstrainedProperties( SerializedObject affectedAssetImporterSO, SerializedObject templateImporterSO, AuditProfile profile )
		{
			foreach( string property in profile.m_ConstrainProperties )
			{
				SerializedProperty assetRuleSP = templateImporterSO.FindProperty( property );
				affectedAssetImporterSO.CopyFromSerializedProperty( assetRuleSP );
			}
			
			if( ! affectedAssetImporterSO.ApplyModifiedProperties() )
				Debug.LogError( "copy failed" );
		}
	}

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
				Debug.LogError( "must set a profile to search against before Building the root" );
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
			AssetViewItem assetsFolder = new AssetViewItem( activePath.GetHashCode(), 1, activePath, true );
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
				List<PropertyConformData> conformData = CheckAgainstProfileTemplate( assetPath, profile );
				
				AssetImporter assetimporter = AssetImporter.GetAtPath( assetPath );
				SerializedObject assetImporterSO = new SerializedObject( assetimporter );
				
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

				// the first entries have lower depth
				for( int i = 0; i < strings.Length; i++ )
				{
					activePath += "/" + strings[i];
					int id = activePath.GetHashCode();

					if( i == strings.Length - 1 )
					{
						AssetViewItem item = new AssetViewItem( id, i + 2, strings[i], result );
						item.icon = AssetDatabase.GetCachedIcon( assetPath ) as Texture2D;
						item.path = activePath;
						item.conformData = conformData;
						item.assetObject = assetImporterSO;
						item.profile = profile;
						item.isAsset = true;
						active.AddChild( item );
						active = item;
						items.Add( id, item );
					}
					else
					{
						AssetViewItem item = null;
						if( !items.TryGetValue( id, out item ) )
						{
							item = new AssetViewItem( id, i + 2, strings[i], result );
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
			if( profile.m_ImporterReference != null )
			{
				string type = "";
				switch( profile.GetAssetType() )
				{
					case AssetType.Texture:
						type = "Texture";
						break;
					case AssetType.Model:
						type = "GameObject";
						break;
					case AssetType.Audio:
						type = "AudioClip";
						break;
					case AssetType.Folder:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				string templatePath = AssetDatabase.GetAssetPath( profile.m_ImporterReference );
				foreach( var assetGUID in AssetDatabase.FindAssets( "t:" + type ) )
				{
					string assetPath = AssetDatabase.GUIDToAssetPath( assetGUID );
					if( assetPath == templatePath )
						continue;

					if( Filter.Conforms( assetPath, profile.m_Filters ) )
						associatedAssets.Add( assetPath );
				}
			}

			return associatedAssets;
		}
		
		List<PropertyConformData> CheckAgainstProfileTemplate( string asset, AuditProfile profile )
		{
			AssetImporter assetimporter = AssetImporter.GetAtPath( asset );
			AssetImporter profileImporter = profile.GetAssetImporter();

			SerializedObject assetImporterSO = new SerializedObject( assetimporter );
			SerializedObject profileImporterSO = new SerializedObject( profileImporter );
			
			if( profile.m_ConstrainProperties.Count == 0 )
			{
				return CompareSerializedObject( profileImporterSO, assetImporterSO );
			}

			List<PropertyConformData> infos = new List<PropertyConformData>();
			
			for( int i = 0; i < profile.m_ConstrainProperties.Count; ++i )
			{
				string property = profile.m_ConstrainProperties[i];

				SerializedProperty foundAssetSP = assetImporterSO.FindProperty( property );
				SerializedProperty assetRuleSP = profileImporterSO.FindProperty( property );

				PropertyConformData conformData = new PropertyConformData( property );
				conformData.SetSerializedProperties( assetRuleSP, foundAssetSP );
				infos.Add( conformData );
			}

			return infos;
		}

		

		List<PropertyConformData> CompareSerializedObject( SerializedObject template, SerializedObject asset )
		{
			SerializedProperty ruleIter = template.GetIterator();
			SerializedProperty assetIter = asset.GetIterator();
			assetIter.NextVisible( true );
			ruleIter.NextVisible( true );
			
			List<PropertyConformData> infos = new List<PropertyConformData>();

			do
			{
				PropertyConformData data = new PropertyConformData( ruleIter.name );
				// TODO better way to do this?
				data.SetSerializedProperties( template.FindProperty( ruleIter.name ), asset.FindProperty( assetIter.name ) );
				infos.Add( data );
				ruleIter.NextVisible( false );
			} while( assetIter.NextVisible( false ) );

			return infos;
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
			AssetViewItem item = FindItem( id, rootItem ) as AssetViewItem;
			if( item == null || item.conforms )
				return;
			GenericMenu menu = new GenericMenu();
			menu.AddItem( new GUIContent( "Conform to Template Properties" ), false, FixCallback, item );
			menu.ShowAsContext();
		}

		void FixCallback( object context )
		{
			AssetViewItem selectedNodes = context as AssetViewItem;
			if( selectedNodes != null )
			{
				selectedNodes.CopyProperties();
				foreach( PropertyConformData data in selectedNodes.conformData )
				{
					data.Conforms = true;
				}
				m_PropertyList.Reload();
			}
			else
				Debug.LogError( "Could not fix Asset with no Assets selected." );
		}
	}

}