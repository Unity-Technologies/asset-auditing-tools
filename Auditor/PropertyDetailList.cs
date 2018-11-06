using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace AssetTools
{

	internal class PropertyViewItem : TreeViewItem
	{
		internal bool conforms { get; set; }
		internal PropertyConformData propertyData { get; set; }
		internal AssetViewItem assetViewItem { get; set; }
		
		internal PropertyViewItem( int id, int depth, string displayName, bool propertyConforms ) : base( id, depth, displayName )
		{
			conforms = propertyConforms;
		}
		
		internal PropertyViewItem( string activePath, int depth, PropertyConformData data )
		{
			base.id = activePath.GetHashCode();
			base.depth = depth;
			conforms = data.Conforms;
			propertyData = data;
			
			if( conforms || data.TemplateType == SerializedPropertyType.Generic )
				base.displayName = data.propertyName;
			else
				base.displayName = data.propertyName + ",  <<<  " + data.TemplateValue;
		}

		public void CopyProperty()
		{
			assetViewItem.assetObject.CopyFromSerializedProperty( propertyData.templateSerializedProperty );
			if( !assetViewItem.assetObject.ApplyModifiedProperties() )
			{
				Debug.LogError( "copy failed" );
			}
			else
			{
				propertyData.Conforms = true;
				conforms = true;
				displayName = propertyData.propertyName;
				assetViewItem.Apply();
			}
		}
	}

	internal class PropertyDetailList : TreeView
	{
		private static Color k_ConformFailColor = new Color( 1f, 0.5f, 0.5f );

		private List<AssetViewItem> selectedItems;
		
		public PropertyDetailList( TreeViewState state ) : base( state )
		{
			showBorder = true;
		}

		public void SetSelection( List<AssetViewItem> selection )
		{
			selectedItems = selection;
			Reload();
			SetExpanded( new int[]{ "Properties:".GetHashCode() } );
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem( -1, -1 );
			root.children = new List<TreeViewItem>();

			// TODO need to display multiple selection (include folders in that)
			if( selectedItems != null && selectedItems.Count > 0 && selectedItems[0].isAsset )
				GenerateTreeElements( selectedItems[0], root );
			return root;
		}

		internal static void GenerateTreeElements( AssetViewItem assetItem, TreeViewItem root )
		{
			string activePath = "Properties:";
			PropertyViewItem propertyRoot = new PropertyViewItem( activePath.GetHashCode(), 1, activePath, true );
			if( propertyRoot.children == null )
				propertyRoot.children = new List<TreeViewItem>();
			root.AddChild( propertyRoot );

			List<PropertyConformData> data = assetItem.conformData;
			for( int i = 0; i < data.Count; ++i )
			{
				AddChildProperty( activePath, propertyRoot, data[i], assetItem, 2 );
			}
		}

		internal static void AddChildProperty( string parentPath, PropertyViewItem parent, PropertyConformData data, AssetViewItem assetItem, int depth, int arrayIndex = -1 )
		{
			string extra = arrayIndex >= 0 ? arrayIndex.ToString() : "";
			string activePath = parentPath + data.propertyName + extra;
			PropertyViewItem property = new PropertyViewItem( activePath, depth, data );
			property.assetViewItem = assetItem;
			parent.AddChild( property );

			for( int i=0; i<data.subData.Count; ++i )
			{
				AddChildProperty( activePath, property, data.subData[i], assetItem, depth+1, data.assetSerializedProperty.isArray ? i : -1 );
			}
		}

		protected override void RowGUI( RowGUIArgs args )
		{
			PropertyViewItem item = args.item as PropertyViewItem;
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
				
				if( item.propertyData != null && item.propertyData.assetSerializedProperty.propertyType != SerializedPropertyType.Generic && r.width > 400 )
				{
					Rect or = new Rect(r);
					or.x += r.width - 100;
					or.width = 100;
					EditorGUI.LabelField( or, item.propertyData.AssetValue );
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
			PropertyViewItem item = FindItem( id, rootItem ) as PropertyViewItem;
			if( item.conforms )
				return;
			
			GenericMenu menu = new GenericMenu();
			if( item.propertyData.TemplateType == SerializedPropertyType.Generic )
			{
				menu.AddItem( new GUIContent( "Set children to Template Values" ), false, FixCallback, item );
			}
			else if( item.propertyData.TemplateType == SerializedPropertyType.ArraySize )
			{
				menu.AddDisabledItem( new GUIContent( "Cannot set array size" ) );
			}
			else
			{
				menu.AddItem( new GUIContent( "Set to " + item.propertyData.TemplateValue ), false, FixCallback, item );
			}
			menu.ShowAsContext();
		}

		void FixCallback( object context )
		{
			if( context == null )
				return;
			
			PropertyViewItem selectedNodes = context as PropertyViewItem;
			Assert.IsNotNull( selectedNodes, "Context must be a PropertyViewItem" );
			selectedNodes.CopyProperty();
		}
	}

}