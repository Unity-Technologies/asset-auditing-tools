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
			get
			{
				return m_MethodName; // TODO with importedVersion number
			}
			set { m_MethodName = value; }
		}

		private List<IConformObject> m_SubObjects = new List<IConformObject>();
		
		private string m_MethodName;
		private readonly int m_MethodVersion;
		private int m_ImportedVersion;
		

		public PreprocessorConformObject( string name, int importedVersion, int methodVersion )
		{
			// method name and importedVersion for the method in question. (may need to get it when checking conforms instead)
			m_MethodName = name;
			m_ImportedVersion = importedVersion;
			m_MethodVersion = methodVersion;
		}


	}
}