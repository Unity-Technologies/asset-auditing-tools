using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{
	public interface IConformObject
	{
		string Name { get; set; }
		
		List<IConformObject> SubObjects { get; set; }
		
		bool Conforms { get; set; }
		
		string ActualValue { get; }
		
		string ExpectedValue { get; }

		bool Apply( SerializedObject toObject );

		void AddTreeViewItems( int parentId, ConformObjectTreeViewItem parent, AssetsTreeViewItem assetsTreeItem, int depth, int arrayIndex = -1 );
	}
}