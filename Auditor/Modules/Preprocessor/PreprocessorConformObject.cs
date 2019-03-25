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
				return m_Conforms;
			}
			set { m_Conforms = value; }
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
				return m_MethodName; // TODO with version number
			}
			set { m_MethodName = value; }
		}

		private bool m_Conforms = true;
		private List<IConformObject> m_SubObjects = new List<IConformObject>();
		
		private string m_MethodName;
		private int m_Version;
		private AssetImporter m_Importer;
		

		// public PreprocessorConformObject( string name, int version )
		// {
		// 	m_MethodName = name;
		// 	m_Version = version;
		// }

		private PreprocessorConformObject( string name, int version, AssetImporter importer )
		{
			// method name and version for the method in question. (may need to get it when checking conforms instead)
			m_MethodName = name;
			m_Version = version;
			
			m_Importer = importer;
		}


	}
}