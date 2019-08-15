using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

namespace AssetTools
{
	public struct ConformData
	{
		private readonly IImportTask m_ImportTask;
		public IImportTask ImportTask
		{
			get { return m_ImportTask; }
		}
		
		private readonly List<IConformObject> m_ConformObjects;
		public List<IConformObject> ConformObjects
		{
			get { return m_ConformObjects; }
		}
		
		public bool Conforms
		{
			get
			{
				for( int i = 0; i < ConformObjects.Count; ++i )
				{
					if( ConformObjects[i].Conforms == false )
						return false;
				}

				return true;
			}
		}
		
		public ConformData( IImportTask importTask, ImportDefinitionProfile forProfile, string assetPath )
		{
			m_ImportTask = importTask;
			m_ConformObjects = m_ImportTask.GetConformObjects( assetPath, forProfile );
		}
	}

}