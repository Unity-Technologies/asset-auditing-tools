using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{
	public interface IConformObject
	{
		string Name { get; set; }
		
		bool Conforms { get; set; }

		List<IConformObject> SubObjects { get; set; }
		
		string ActualValue { get; }
		
		string ExpectedValue { get; }

		bool ApplyConform( SerializedObject toObject );

		void AddTreeViewItems( string parentPath, ConformObjectTreeViewItem parent, AssetsTreeViewItem assetsTreeItem, int depth, int arrayIndex = -1 );
	}
}