using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

namespace AssetTools
{


	[CreateAssetMenu(fileName = "New Import Definition", menuName = "Asset Tools/New Import Definition", order = 0)]
	public class ImportDefinitionProfile : ScriptableObject, IComparable<ImportDefinitionProfile>
	{
		public bool m_RunOnImport = false;
		public bool m_FilterToFolder = true;
		public int m_SortIndex = 0;
		private string m_DirectoryPath = null;
		public List<Filter> m_Filters;
		
		[FormerlySerializedAs( "m_Modules" )]
		public List<BaseImportTask> m_ImportTasks = new List<BaseImportTask>();

		internal string DirectoryPath
		{
			get
			{
				if( string.IsNullOrEmpty( m_DirectoryPath ) )
					m_DirectoryPath = Path.GetDirectoryName( AssetDatabase.GetAssetPath( this ) );
				return m_DirectoryPath;
			}
			set { m_DirectoryPath = null; }
		}

		public List<ConformData> GetConformData( string asset )
		{
			List<ConformData> data = new List<ConformData>();
			for( int i = 0; i < m_ImportTasks.Count; ++i )
			{
				if( m_ImportTasks[i] != null )
				{
					ConformData d = new ConformData( m_ImportTasks[i], this, asset );
					data.Add( d );
				}
			}
			return data;
		}

		public void ProcessAsset( AssetImporter asset, bool checkForConformity = true )
		{
			if( checkForConformity )
			{
				if( m_FilterToFolder )
				{
					List<Filter> filters = new List<Filter>(m_Filters);
					filters.Add( new Filter( Filter.ConditionTarget.Directory, Filter.Condition.StartsWith, DirectoryPath ) );
					if( Filter.Conforms( asset, filters ) == false )
						return;
				}
				else if( Filter.Conforms( asset, m_Filters ) == false )
					return;
			}

			if( m_RunOnImport )
			{
				for( int i = 0; i < m_ImportTasks.Count; ++i )
				{
					if( m_ImportTasks[i] != null )
						m_ImportTasks[i].Apply( asset, this );
				}
			}
			else
			{
				for( int i = 0; i < m_ImportTasks.Count; ++i )
				{
					if( m_ImportTasks[i] != null && m_ImportTasks[i].IsManuallyProcessing( asset ) )
						m_ImportTasks[i].Apply( asset, this );
				}
			}
		}
		
		public BaseImportTask AddModule( Type type )
		{
			if (type == null)
			{
				Debug.LogWarning("Cannot remove schema with null type.");
				return null;
			}
			if (!typeof(BaseImportTask).IsAssignableFrom(type))
			{
				Debug.LogWarningFormat("Invalid Schema type {0}. Schemas must inherit from AddressableAssetGroupSchema.", type.FullName);
				return null;
			}
            
			foreach( BaseImportTask moduleObject in m_ImportTasks )
			{
				if( moduleObject.GetType() == type )
				{
					// TODO check to make sure has to be unique
					Debug.LogError( "Module already exists" );
					//return false;
				}
			}

			BaseImportTask importTaskInstance = (BaseImportTask)CreateInstance( type );
			if( importTaskInstance != null )
			{
				importTaskInstance.name = type.Name;
				try
				{
					importTaskInstance.hideFlags |= HideFlags.HideInHierarchy;
					AssetDatabase.AddObjectToAsset( importTaskInstance, this );
				}
				catch( Exception e )
				{
					Console.WriteLine( e );
					throw;
				}
				
				m_ImportTasks.Add( importTaskInstance );
				EditorUtility.SetDirty( this );
			}

			return importTaskInstance;
		}

		public bool RemoveModule( int index )
		{
			if( index < 0 || index >= m_ImportTasks.Count )
				return false;
			if( m_ImportTasks[index] == null )
				return true;
			
#if UNITY_2018_3_OR_NEWER
			AssetDatabase.RemoveObjectFromAsset( m_ImportTasks[index] );
#else
			DestroyImmediate( m_ImportTasks[index], true );
#endif
			m_ImportTasks.RemoveAt( index );
			EditorUtility.SetDirty( this );
			return true;
		}
		
		public int CompareTo( ImportDefinitionProfile other )
		{
			if( other == null )
				return 1;
			
			int s = m_SortIndex.CompareTo( other.m_SortIndex );
			if( s == 0 )
			{
				// if in same index, sort by shortest path length first
				int lengthCompare = DirectoryPath.Length.CompareTo( other.DirectoryPath.Length );
				return lengthCompare;
			}
			return s;
		}
	}
}