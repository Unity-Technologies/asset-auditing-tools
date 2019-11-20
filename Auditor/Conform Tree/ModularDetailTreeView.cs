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
		internal class ItemTree
		{
			private ConformObjectTreeViewItem item;
			private List<ItemTree> children;

			public int Depth
			{
				get { return item == null ? -1 : item.depth; }
			}

			public ItemTree( ConformObjectTreeViewItem i )
			{
				item = i;
				children = new List<ItemTree>();
			}

			public void AddChild( ItemTree item )
			{
				children.Add( item );
			}

			public void Sort( int[] columnSortOrder, bool[] isColumnAscending )
			{
				children.Sort( delegate( ItemTree a, ItemTree b )
				{
					int rtn = 0;
					for( int i = 0; i < columnSortOrder.Length; i++ )
					{
						if( columnSortOrder[i] == 0 )
						{
							rtn = isColumnAscending[i] ? a.item.conforms.CompareTo( b.item.conforms )
								: b.item.conforms.CompareTo( a.item.conforms );
							if( rtn == 0 )
								continue;
							return rtn;
						}
						else if( columnSortOrder[i] == 1 )
						{
							rtn = isColumnAscending[i] ? string.Compare( a.item.displayName, b.item.displayName, StringComparison.Ordinal )
								: string.Compare( b.item.displayName, a.item.displayName, StringComparison.Ordinal );
							if( rtn == 0 )
								continue;
							return rtn;
						}
					}

					return rtn;
				});
				
				foreach( ItemTree child in children )
					child.Sort( columnSortOrder, isColumnAscending );
			}

			public void ToList(List<TreeViewItem> list)
			{
				// TODO be good to optimise this, rarely used, so not required
				if( item != null )
					list.Add( item );
				foreach( ItemTree child in children )
					child.ToList( list );
			}
		}
		
		private static readonly Color k_ConformFailColor = new Color( 1f, 0.5f, 0.5f );

		private List<AssetsTreeViewItem> m_SelectedItems;
		private Texture2D m_UnconformedTexture;
		
		public ModularDetailTreeView( TreeViewState state, MultiColumnHeaderState headerState ) : base( state, new MultiColumnHeader( headerState ) )
		{
			showBorder = true;
			showAlternatingRowBackgrounds = true;
			columnIndexForTreeFoldouts = 1;
			m_UnconformedTexture = EditorGUIUtility.FindTexture( "LookDevClose@2x" );
			multiColumnHeader.sortingChanged += OnSortingChanged;
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
		
		protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
		{
			var rows = base.BuildRows (root);
			SortIfNeeded(root, rows);
			return rows;
		}

		void OnSortingChanged (MultiColumnHeader multiColumnHeader)
		{
			if( multiColumnHeader.sortedColumnIndex == -1 )
				return;
			SortIfNeeded(rootItem, GetRows());
		}
		void SortIfNeeded( TreeViewItem root, IList<TreeViewItem> rows )
		{
			if(multiColumnHeader.sortedColumnIndex == -1)
				return;
			SortByMultipleColumns(rows);
			Repaint();
		}

		void SortByMultipleColumns( IList<TreeViewItem> rows )
		{
			int[] sortedColumns = multiColumnHeader.state.sortedColumns;
			if (sortedColumns.Length == 0)
				return;
			
			bool[] columnAscending = new bool[sortedColumns.Length];
			for( int i = 0; i < sortedColumns.Length; i++ )
				columnAscending[i] = multiColumnHeader.IsSortedAscending( sortedColumns[i] );

			ItemTree root = new ItemTree(null);
			Stack<ItemTree> stack = new Stack<ItemTree>();
			stack.Push( root );
			foreach( TreeViewItem row in rows )
			{
				ConformObjectTreeViewItem r = row as ConformObjectTreeViewItem;
				if( r == null )
					continue;
				int activeParentDepth = stack.Peek().Depth;
				
				while( row.depth <= activeParentDepth )
				{
					stack.Pop();
					activeParentDepth = stack.Peek().Depth;
				}
				
				if( row.depth > activeParentDepth )
				{
					ItemTree t = new ItemTree(r);
					stack.Peek().AddChild(t);
					stack.Push(t);
				}
			}
			
			root.Sort( sortedColumns, columnAscending );

			// convert back to rows
			List<TreeViewItem> newRows = new List<TreeViewItem>(rows.Count);
			root.ToList( newRows );
			rows.Clear();
			foreach( TreeViewItem treeViewItem in newRows )
				rows.Add( treeViewItem );
		}

		protected override void RowGUI( RowGUIArgs args )
		{
			ConformObjectTreeViewItem item = args.item as ConformObjectTreeViewItem;
			if( item == null )
			{
				Debug.LogWarning( "Unknown TreeViewItem for conform tree" );
				return;
			}
			
			for (int i = 0; i < args.GetNumVisibleColumns (); ++i)
			{
				if( i == 0 )
					ConformsGUI(args.GetCellRect(i), item);
				else if( i == 1 )
					PropertyNameGUI(args.GetCellRect(i), item, ref args);
				else if( i == 2 && item.conformObject != null )
					ExpectedValueGUI(args.GetCellRect(i), item);
				else if( i == 3 && item.conformObject != null )
					ActualValueGUI(args.GetCellRect(i), item);
				
			}
		}
		
		void ConformsGUI( Rect cellRect, ConformObjectTreeViewItem item )
		{
			if( !item.conforms )
			{
				Color old = GUI.color;
				GUI.color = k_ConformFailColor * 0.8f;
				GUI.DrawTexture( cellRect, m_UnconformedTexture );
				GUI.color = old;
			}
		}

		void ActualValueGUI( Rect cellRect, ConformObjectTreeViewItem item )
		{
			Rect labelRect = cellRect;
			if( item.conforms == false )
			{
				Color old = GUI.color;
				GUI.color = k_ConformFailColor;
				EditorGUI.LabelField( labelRect, item.conformObject.ActualValue );
				GUI.color = old;
			}
			else
				EditorGUI.LabelField( labelRect, item.conformObject.ActualValue );
		}
		
		void ExpectedValueGUI( Rect cellRect, ConformObjectTreeViewItem item )
		{
			Rect labelRect = cellRect;
			if( item.conforms == false )
			{
				Color old = GUI.color;
				GUI.color = k_ConformFailColor;
				EditorGUI.LabelField( labelRect, item.conformObject.ExpectedValue );
				GUI.color = old;
			}
			else
				EditorGUI.LabelField( labelRect, item.conformObject.ExpectedValue );
		}
		
		void PropertyNameGUI( Rect cellRect, ConformObjectTreeViewItem item, ref RowGUIArgs args )
		{
			float indent = GetContentIndent( item ) + extraSpaceBeforeIconAndLabel;

			Rect labelRect = cellRect;
			labelRect.xMin += indent;
			Rect iconRect = cellRect;
			if( args.item.icon != null )
			{
				iconRect.xMin += indent;
				iconRect.width = iconRect.height;
				GUI.DrawTexture( iconRect, args.item.icon );
				labelRect.x += 18;
				labelRect.width -= 18;
			}
			
			if( item.conforms == false )
			{
				Color old = GUI.color;
				GUI.color = k_ConformFailColor;
				EditorGUI.LabelField( labelRect, args.label );
				GUI.color = old;
			}
			else
				EditorGUI.LabelField( labelRect, args.label );
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