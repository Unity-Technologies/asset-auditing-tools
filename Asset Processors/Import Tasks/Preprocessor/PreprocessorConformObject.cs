using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{
	public class PreprocessorConformObject : IConformObject
	{
		public bool Conforms
		{
			get
			{
				for( int i = 0; i < m_SubObjects.Count; ++i )
				{
					if( !m_SubObjects[i].Conforms )
						return false;
				}
				
				return m_ImportedVersion == m_MethodVersion;
			}
			set
			{
				if( value )
					m_ImportedVersion = m_MethodVersion;
			}
		}

		public List<IConformObject> SubObjects
		{
			get { return m_SubObjects; }
			set { m_SubObjects = value; }
		}

		public string Name
		{
			get { return m_MethodName; }
			set { m_MethodName = value; }
		}

		private List<IConformObject> m_SubObjects = new List<IConformObject>();
		
		private string m_MethodName;
		private readonly int m_MethodVersion;
		private int m_ImportedVersion = Int32.MinValue;
		
		public string ActualValue
		{
			get
			{
				if( m_ImportedVersion == Int32.MinValue )
					return "None";
				return m_ImportedVersion.ToString();
			}
		}

		public string ExpectedValue
		{
			get { return m_MethodVersion.ToString(); }
		}
		
		public bool Apply( SerializedObject toObject )
		{
			return false;
		}
		
		public void AddTreeViewItems( int parentId, ConformObjectTreeViewItem parent, AssetsTreeViewItem assetsTreeItem, int depth, int arrayIndex = -1 )
		{
			int activePath = parentId + (Name.GetHashCode()*31);
			ConformObjectTreeViewItem conformObjectTree = new ConformObjectTreeViewItem( activePath, depth, this )
			{
				AssetsTreeViewItem = assetsTreeItem
			};
			parent.AddChild( conformObjectTree );

			for( int i=0; i<SubObjects.Count; ++i )
			{
				SubObjects[i].AddTreeViewItems( activePath, conformObjectTree, assetsTreeItem, depth+1 );
			}
		}

		public PreprocessorConformObject( string name, int methodVersion )
		{
			// method name and importedVersion for the method in question. (may need to get it when checking conforms instead)
			m_MethodName = name;
			m_MethodVersion = methodVersion;
		}
		
		public PreprocessorConformObject( string name, int methodVersion, int importedVersion )
		{
			// method name and importedVersion for the method in question. (may need to get it when checking conforms instead)
			m_MethodName = name;
			m_MethodVersion = methodVersion;
			m_ImportedVersion = importedVersion;
		}


	}
}