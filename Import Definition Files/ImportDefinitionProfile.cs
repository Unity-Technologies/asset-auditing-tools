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
		
		[SerializeField] private List<Filter> m_Filters;

		public List<Filter> Filters
		{
			get{ return m_Filters == null ? new List<Filter>() : new List<Filter>(m_Filters); }
		}

		private string m_DirectoryPath = null;
		
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

		public List<Filter> GetFilters( bool userFiltersOnly = false )
		{
			List<Filter> filters =Filters;
			if( m_FilterToFolder )
				filters.Add( new Filter( Filter.ConditionTarget.Directory, Filter.Condition.StartsWith, DirectoryPath ) );
			return filters;
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

		public void PreprocessAsset( ImportContext context, bool checkForConformity = true )
		{
			if( checkForConformity )
			{
				if( m_FilterToFolder )
				{
					List<Filter> filters = Filters;
					filters.Add( new Filter( Filter.ConditionTarget.Directory, Filter.Condition.StartsWith, DirectoryPath ) );
					if( Filter.Conforms( context.Importer, filters ) == false )
						return;
				}
				else if( Filter.Conforms( context.Importer, m_Filters ) == false )
					return;
			}

			bool saveMeta = false;

			if( m_RunOnImport )
			{
				for( int i = 0; i < m_ImportTasks.Count; ++i )
				{
					if( m_ImportTasks[i] != null  )
					{
						m_ImportTasks[i].PreprocessTask( context, this );
						saveMeta = true;
						if( m_ImportTasks[i].TaskProcessType == BaseImportTask.ProcessingType.Pre )
						{
							m_ImportTasks[i].Apply( context, this );
							m_ImportTasks[i].SetManuallyProcessing( context.AssetPath, false );
						}
					}
				}
			}
			else
			{
				for( int i = 0; i < m_ImportTasks.Count; ++i )
				{
					if( m_ImportTasks[i] != null && m_ImportTasks[i].IsManuallyProcessing( context.Importer ) )
					{
						m_ImportTasks[i].PreprocessTask( context, this );
						saveMeta = true;
						if( m_ImportTasks[i].TaskProcessType == BaseImportTask.ProcessingType.Pre )
						{
							m_ImportTasks[i].Apply( context, this );
							m_ImportTasks[i].SetManuallyProcessing( context.AssetPath, false );
						}
					}
				}
			}
			
			if( saveMeta )
			{
				UserDataSerialization data = UserDataSerialization.Get( context.AssetPath );
				if( data != null )
					data.SaveMetaData();
			}
		}
		
		public void PostprocessAsset( ImportContext context, bool checkForConformity = true )
		{
			if( checkForConformity )
			{
				if( m_FilterToFolder )
				{
					List<Filter> filters = Filters;
					filters.Add( new Filter( Filter.ConditionTarget.Directory, Filter.Condition.StartsWith, DirectoryPath ) );
					if( Filter.Conforms( context.Importer, filters ) == false )
						return;
				}
				else if( Filter.Conforms( context.Importer, m_Filters ) == false )
					return;
			}
			
			if( m_RunOnImport )
			{
				for( int i = 0; i < m_ImportTasks.Count; ++i )
				{
					if( m_ImportTasks[i] != null && m_ImportTasks[i].TaskProcessType == BaseImportTask.ProcessingType.Post )
					{
						m_ImportTasks[i].Apply( context, this );
						m_ImportTasks[i].SetManuallyProcessing( context.AssetPath, false );
					}
				}
			}
			else
			{
				for( int i = 0; i < m_ImportTasks.Count; ++i )
				{
					if( m_ImportTasks[i] != null && m_ImportTasks[i].IsManuallyProcessing( context.Importer ) && m_ImportTasks[i].TaskProcessType == BaseImportTask.ProcessingType.Post )
					{
						m_ImportTasks[i].Apply( context, this );
						m_ImportTasks[i].SetManuallyProcessing( context.AssetPath, false );
					}
				}
			}
		}
		
		public BaseImportTask AddTask( Type type )
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

			BaseImportTask importTaskInstance = (BaseImportTask)CreateInstance( type );
			if( importTaskInstance.MaximumCount > 0 )
			{
				int sameType = 0;
				foreach( BaseImportTask task in m_ImportTasks )
				{
					if( task.GetType() == type )
					{
						sameType++;
						if( sameType >= importTaskInstance.MaximumCount )
							break;
					}
				}

				if( sameType >= importTaskInstance.MaximumCount )
				{
					DestroyImmediate( importTaskInstance );
					Debug.LogError( "Task count exceeded" );
					return null;
				}
			}

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

		public bool RemoveTask( int index )
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