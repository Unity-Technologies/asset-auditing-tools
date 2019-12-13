using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public abstract class HierarchyTreeView : TreeView
	{
		public enum SearchType
		{
			Hierarchy = 0,
			HierarchyLeafOnly,
			Flat,
			FlatLeafOnly,
			Standard,
		}
		
		private string m_CustomSearch;
		private SearchType m_SearchType;
		private bool m_CaseSensitive;

		public string CustomSearch
		{
			get { return m_CustomSearch; }
			set { m_CustomSearch = value; }
		}

		public SearchType SearchStyle
		{
			get { return m_SearchType; }
			set { m_SearchType = value;  }
		}
		
		public bool SearchCaseSensitive
		{
			get { return m_CaseSensitive; }
			set { m_CaseSensitive = value;  }
		}

		protected HierarchyTreeView( TreeViewState state ) : base( state )
		{
		}

		protected override bool CanChangeExpandedState( TreeViewItem item )
		{
			if( string.IsNullOrEmpty(m_CustomSearch) || m_SearchType == SearchType.Standard )
				return base.CanChangeExpandedState( item );
			return false;
		}
		
		protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
		{
			if( !string.IsNullOrEmpty(m_CustomSearch) )
			{
				List<TreeViewItem> searchedRows = new List<TreeViewItem>( 100 );
				switch( m_SearchType )
				{
					case SearchType.HierarchyLeafOnly:
						BuildSearchTreeRecursive( root, searchedRows, true );
						break;
					case SearchType.Hierarchy:
						BuildSearchTreeRecursive( root, searchedRows, false );
						break;
					case SearchType.FlatLeafOnly:
						BuildSearchFlat( root, searchedRows, true );
						break;
					case SearchType.Flat:
						BuildSearchFlat( root, searchedRows, false );
						break;
				}
				return searchedRows;
			}
			
			return base.BuildRows(root);
		}
		
		// TODO optimise this recursiveness

		private void BuildSearchFlat( TreeViewItem item, List<TreeViewItem> items, bool leafOnly )
		{
			if( item.hasChildren )
			{
				foreach( TreeViewItem child in item.children )
				{
					if( IsInSearch( m_CustomSearch, child, leafOnly ) )
					{
						if( child.hasChildren == false )
						{
							items.Add( child );
							child.depth = 0;
						}
						BuildSearchFlat( child, items, leafOnly );
					}
				}
			}
		}

		private void BuildSearchTreeRecursive( TreeViewItem item, List<TreeViewItem> items, bool leafOnly )
		{
			if( item.hasChildren )
			{
				foreach( TreeViewItem child in item.children )
				{
					if( IsInSearch( m_CustomSearch, child, leafOnly ) )
					{
						items.Add( child );
						BuildSearchTreeRecursive( child, items, leafOnly );
					}
				}
			}
		}

		private bool IsInSearch( string search, TreeViewItem item, bool leafOnly )
		{
			bool includedInSearch = false;
			if( item.hasChildren )
			{
				if( !leafOnly )
					includedInSearch = item.displayName.IndexOf( search, m_CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase ) >= 0;
				if( !includedInSearch )
				{
					foreach( TreeViewItem t in item.children )
					{
						if( IsInSearch( search, t, leafOnly ) )
						{
							includedInSearch = true;
							break;
						}
					}
				}
			}
			else
				includedInSearch = item.displayName.IndexOf( search, m_CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase ) >= 0;
			
			return includedInSearch;
		}
		
		public void ExpandForSelection( )
		{
			List<int> toExpand = new List<int>(GetExpanded());
			IList<int> selection = GetSelection();
			
			foreach( int selectedId in selection )
			{
				TreeViewItem selectedItem = FindItem( selectedId, rootItem );
				if( selectedItem == null )
					continue;
				
				while( selectedItem.parent != null )
				{
					selectedItem = selectedItem.parent;
					if( rootItem != selectedItem && ! toExpand.Contains( selectedItem.id ))
						toExpand.Add( selectedItem.id );
				}
			}
			
			SetExpanded( toExpand );
		}
	}

}