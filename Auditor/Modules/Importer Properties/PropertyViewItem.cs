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
		internal PropertyConformObject propertyConformObject { get; set; }
		internal AssetViewItem assetViewItem { get; set; }
		
		internal PropertyViewItem( int id, int depth, string displayName, bool propertyConforms ) : base( id, depth, displayName )
		{
			conforms = propertyConforms;
		}
		
		internal PropertyViewItem( string activePath, int depth, PropertyConformObject conformObject )
		{
			base.id = activePath.GetHashCode();
			base.depth = depth;
			conforms = conformObject.Conforms;
			propertyConformObject = conformObject;
			
			if( conforms || conformObject.TemplateType == SerializedPropertyType.Generic )
				base.displayName = conformObject.Name;
			else
				base.displayName = conformObject.Name + ",  <<<  " + conformObject.TemplateValue;
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