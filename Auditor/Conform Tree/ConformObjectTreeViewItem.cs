using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace AssetTools
{
	public class ConformObjectTreeViewItem : TreeViewItem
	{
		private bool m_Conforms = false;
		internal bool conforms
		{
			get
			{
				return conformObject != null ? conformObject.Conforms : m_Conforms;
			}
			set { m_Conforms = value; }
		}
		internal IConformObject conformObject { get; set; }
		internal AssetsTreeViewItem AssetsTreeViewItem { get; set; }
		
		internal ConformObjectTreeViewItem( int id, int depth, string displayName, bool propertyConforms ) : base( id, depth, displayName )
		{
			m_Conforms = propertyConforms;
		}
		
		internal ConformObjectTreeViewItem( int id, int depth, IConformObject conformObject )
		{
			base.id = id;
			base.depth = depth;
			this.conformObject = conformObject;
			base.displayName = conformObject.Name;
		}

		public void ApplyConform()
		{
			if( conformObject.Apply( AssetsTreeViewItem.assetObject ) )
			{
				conformObject.Conforms = true;
				m_Conforms = true;
				displayName = conformObject.Name;
				AssetsTreeViewItem.ReimportAsset();
			}
		}
	}
}