using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace AssetTools
{
	// TODO make this generic for IConformObject
	internal class ConformObjectTreeViewItem : TreeViewItem
	{
		internal bool conforms { get; set; }
		internal PropertyConformObject propertyConformObject { get; set; }
		internal AssetTreeViewItem assetTreeViewItem { get; set; }
		
		internal ConformObjectTreeViewItem( int id, int depth, string displayName, bool propertyConforms ) : base( id, depth, displayName )
		{
			conforms = propertyConforms;
		}
		
		internal ConformObjectTreeViewItem( string activePath, int depth, IConformObject conformObject )
		{
			Assert.IsTrue( conformObject is PropertyConformObject );
			base.id = activePath.GetHashCode();
			base.depth = depth;
			conforms = conformObject.Conforms;
			propertyConformObject = (PropertyConformObject)conformObject;
			
			base.displayName = conformObject.Name;
		}

		public void CopyProperty()
		{
			assetTreeViewItem.assetObject.CopyFromSerializedProperty( propertyConformObject.TemplateSerializedProperty );
			if( !assetTreeViewItem.assetObject.ApplyModifiedProperties() )
			{
				Debug.LogError( "copy failed" );
			}
			else
			{
				propertyConformObject.Conforms = true;
				conforms = true;
				displayName = propertyConformObject.Name;
				assetTreeViewItem.ReimportAsset();
			}
		}
	}
}