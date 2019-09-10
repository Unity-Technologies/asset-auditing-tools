using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace AssetTools
{
	internal class ModularDetailTreeView : TreeView
	{
		private static readonly Color k_ConformFailColor = new Color( 1f, 0.5f, 0.5f );

		private List<AssetsTreeViewItem> m_SelectedItems;
		
		public ModularDetailTreeView( TreeViewState state ) : base( state )
		{
			showBorder = true;
		}

		public void SetSelectedAssetItems( List<AssetsTreeViewItem> selection )
		{
			m_SelectedItems = selection;
			Reload();
			if( selection.Count > 0 )
			{
				// TODO expand so many, but not all if multi selected
				SetExpanded( new int[] {(selection[0].displayName + ":").GetHashCode()} );
			}
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem( -1, -1 )
			{
				children = new List<TreeViewItem>()
			};

			// TODO need to display multiple selection (include folders in that)
			if( m_SelectedItems != null && m_SelectedItems.Count > 0 && m_SelectedItems[0].isAsset )
				GenerateTreeElements( m_SelectedItems[0], root );
			return root;
		}

		private static void GenerateTreeElements( AssetsTreeViewItem assetsTreeItem, TreeViewItem root )
		{
			string activePath = assetsTreeItem.displayName + ":";
			
			// create root object for the Asset
			ConformObjectTreeViewItem conformObjectAssetRoot = new ConformObjectTreeViewItem( activePath.GetHashCode(), 0, activePath, true )
			{
				icon = assetsTreeItem.icon
			};
			if( conformObjectAssetRoot.children == null )
				conformObjectAssetRoot.children = new List<TreeViewItem>();
			root.AddChild( conformObjectAssetRoot );

			List<ConformData> data = assetsTreeItem.conformData;
			
			for( int i = 0; i < data.Count; ++i )
			{
				if( data[i].ConformObjects.Count == 0 )
					continue;
				
				int moduleHash = data[i].ImportTask.GetHashCode() + i;
				ConformObjectTreeViewItem conformObjectModuleRoot = new ConformObjectTreeViewItem( moduleHash, 1, data[i].ImportTask.GetType().Name, true );
				if( conformObjectModuleRoot.children == null )
					conformObjectModuleRoot.children = new List<TreeViewItem>();
				conformObjectAssetRoot.AddChild( conformObjectModuleRoot );

				if( data[i].Conforms == false )
				{
					conformObjectModuleRoot.conforms = false;
					conformObjectAssetRoot.conforms = false;
				}

				foreach( var conformObject in data[i].ConformObjects )
				{
					conformObject.AddTreeViewItems( moduleHash, conformObjectModuleRoot, assetsTreeItem, 2 );
				}
			}
		}

		protected override void RowGUI( RowGUIArgs args )
		{
			ConformObjectTreeViewItem item = args.item as ConformObjectTreeViewItem;
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
				
				// This displays what the current value is
				if( item.conformObject != null && r.width > 400 )
				{
					Rect or = new Rect(r);
					or.x += r.width - 100;
					or.width = 100;
					EditorGUI.LabelField( or, item.conformObject.ActualValue );
				}

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

		protected override void ContextClickedItem( int id )
		{
			ConformObjectTreeViewItem item = FindItem( id, rootItem ) as ConformObjectTreeViewItem;
			Assert.IsNotNull( item );
			if( item.conforms )
				return;
			
			// TODO ask the ConformObject to do this
			PropertyConformObject propertyConformObject = item.conformObject as PropertyConformObject;
			if( propertyConformObject != null )
			{
				GenericMenu menu = new GenericMenu();
				if( propertyConformObject.TemplateType == SerializedPropertyType.Generic )
				{
					menu.AddItem( new GUIContent( "Set children to Template Values" ), false, FixCallback, item );
				}
				else if( propertyConformObject.TemplateType == SerializedPropertyType.ArraySize )
				{
					menu.AddDisabledItem( new GUIContent( "Cannot set array size" ) );
				}
				else
				{
					menu.AddItem( new GUIContent( "Set to " + propertyConformObject.TemplateValue ), false, FixCallback, item );
				}

				menu.ShowAsContext();
			}
		}

		private static void FixCallback( object context )
		{
			if( context == null )
				return;
			
			// TODO multi-select
			ConformObjectTreeViewItem selectedNodes = context as ConformObjectTreeViewItem;
			Assert.IsNotNull( selectedNodes, "Context must be a ConformObjectTreeViewItem" );
			selectedNodes.ApplyConform();
		}
	}

}