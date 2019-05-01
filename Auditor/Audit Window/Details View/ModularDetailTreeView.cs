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

		private List<AssetTreeViewItem> selectedItems;
		
		public ModularDetailTreeView( TreeViewState state ) : base( state )
		{
			showBorder = true;
		}

		public void SetSelection( List<AssetTreeViewItem> selection )
		{
			selectedItems = selection;
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
			if( selectedItems != null && selectedItems.Count > 0 && selectedItems[0].isAsset )
				GenerateTreeElements( selectedItems[0], root );
			return root;
		}

		private static void GenerateTreeElements( AssetTreeViewItem assetTreeItem, TreeViewItem root )
		{
			string activePath = assetTreeItem.displayName + ":";
			ConformObjectTreeViewItem conformObjectTreeRoot = new ConformObjectTreeViewItem( activePath.GetHashCode(), 0, activePath, true )
			{
				icon = assetTreeItem.icon
			};
			if( conformObjectTreeRoot.children == null )
				conformObjectTreeRoot.children = new List<TreeViewItem>();
			root.AddChild( conformObjectTreeRoot );

			List<IConformObject> data = assetTreeItem.conformData;
			for( int i = 0; i < data.Count; ++i )
			{
				// Add all ConformObject's that are properties
				if( data[i] is PropertyConformObject )
					AddChildProperty( activePath, conformObjectTreeRoot, (PropertyConformObject)data[i], assetTreeItem, 1 );
			}
		}

		private static void AddChildProperty( string parentPath, ConformObjectTreeViewItem parent, PropertyConformObject propertyConformObject, AssetTreeViewItem assetTreeItem, int depth, int arrayIndex = -1 )
		{
			string extra = arrayIndex >= 0 ? arrayIndex.ToString() : "";
			string activePath = parentPath + propertyConformObject.Name + extra;
			ConformObjectTreeViewItem conformObjectTree = new ConformObjectTreeViewItem( activePath, depth, propertyConformObject )
			{
				assetTreeViewItem = assetTreeItem
			};
			parent.AddChild( conformObjectTree );

			for( int i=0; i<propertyConformObject.SubObjects.Count; ++i )
			{
				// TODO will this be slow? , need to see if there is a better way to cache object type
				if( propertyConformObject.SubObjects[i] is PropertyConformObject )
					AddChildProperty( activePath, conformObjectTree, (PropertyConformObject)propertyConformObject.SubObjects[i], assetTreeItem, depth+1, propertyConformObject.AssetSerializedProperty.isArray ? i : -1 );
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
				{
					GUI.color = k_ConformFailColor;
				}
				
				if( item.propertyConformObject != null && item.propertyConformObject.AssetSerializedProperty.propertyType != SerializedPropertyType.Generic && r.width > 400 )
				{
					Rect or = new Rect(r);
					or.x += r.width - 100;
					or.width = 100;
					EditorGUI.LabelField( or, item.propertyConformObject.AssetValue );
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
			
			GenericMenu menu = new GenericMenu();
			if( item.propertyConformObject.TemplateType == SerializedPropertyType.Generic )
			{
				menu.AddItem( new GUIContent( "Set children to Template Values" ), false, FixCallback, item );
			}
			else if( item.propertyConformObject.TemplateType == SerializedPropertyType.ArraySize )
			{
				menu.AddDisabledItem( new GUIContent( "Cannot set array size" ) );
			}
			else
			{
				menu.AddItem( new GUIContent( "Set to " + item.propertyConformObject.TemplateValue ), false, FixCallback, item );
			}
			menu.ShowAsContext();
		}

		private static void FixCallback( object context )
		{
			if( context == null )
				return;
			
			// TODO multi-select
			
			ConformObjectTreeViewItem selectedNodes = context as ConformObjectTreeViewItem;
			Assert.IsNotNull( selectedNodes, "Context must be a ConformObjectTreeViewItem" );
			selectedNodes.CopyProperty();
		}
	}

}