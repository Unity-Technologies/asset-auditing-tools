using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace AssetTools
{
	internal class PropertyViewItem : TreeViewItem
	{
		internal bool conforms { get; set; }
		internal PropertyConformObject propertyConformObject { get; set; }
		internal AssetViewItem assetViewItem { get; set; }
		
		internal PropertyViewItem( int id, int depth, string displayName, bool propertyConforms ) : base( id, depth, displayName )
		{
			conforms = propertyConforms;
		}
		
		internal PropertyViewItem( string activePath, int depth, IConformObject conformObject )
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
			assetViewItem.assetObject.CopyFromSerializedProperty( propertyConformObject.TemplateSerializedProperty );
			if( !assetViewItem.assetObject.ApplyModifiedProperties() )
			{
				Debug.LogError( "copy failed" );
			}
			else
			{
				propertyConformObject.Conforms = true;
				conforms = true;
				displayName = propertyConformObject.Name;
				assetViewItem.ReimportAsset();
			}
		}
	}
}