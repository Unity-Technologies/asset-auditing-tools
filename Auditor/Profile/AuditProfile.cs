using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{
	public enum AssetType
	{
		Texture,
		Model,
		Audio,
		Folder,
		Native,
		NA
	}
	
	[CreateAssetMenu(fileName = "NewAssetAuditorProfile", menuName = "Asset Tools/New Auditor Profile", order = 0)]
	public class AuditProfile : ScriptableObject, IComparable<AuditProfile>
	{
		public bool m_RunOnImport = false;
		public bool m_FilterToFolder = true;
		public int m_SortIndex = 0;
		
		public List<Filter> m_Filters;

		public List<BaseModule> m_Modules = new List<BaseModule>();

		private string m_DirectoryPath = null;

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

		public List<IConformObject> GetConformData( string asset )
		{
			
			List<IConformObject> lst = new List<IConformObject>(m_Modules.Count);
			for( int i = 0; i < m_Modules.Count; ++i )
			{
				if( m_Modules[i] != null )
					lst.AddRange( m_Modules[i].GetConformObjects( asset, this ) );
			}
			return lst;
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
				for( int i = 0; i < m_Modules.Count; ++i )
				{
					if( m_Modules[i] != null )
						m_Modules[i].Apply( asset, this );
				}
			}
			else
			{
				for( int i = 0; i < m_Modules.Count; ++i )
				{
					if( m_Modules[i] != null && m_Modules[i].IsManuallyProcessing( asset ))
						m_Modules[i].Apply( asset, this );
				}
			}
		}
		
		public BaseModule AddModule( Type type )
		{
			if (type == null)
			{
				Debug.LogWarning("Cannot remove schema with null type.");
				return null;
			}
			if (!typeof(BaseModule).IsAssignableFrom(type))
			{
				Debug.LogWarningFormat("Invalid Schema type {0}. Schemas must inherit from AddressableAssetGroupSchema.", type.FullName);
				return null;
			}
            
			foreach( BaseModule moduleObject in m_Modules )
			{
				if( moduleObject.GetType() == type )
				{
					// TODO check to make sure has to be unique
					Debug.LogError( "Module already exists" );
					//return false;
				}
			}

			BaseModule moduleInstance = (BaseModule)CreateInstance( type );
			if( moduleInstance != null )
			{
				moduleInstance.name = type.Name;
				try
				{
					moduleInstance.hideFlags |= HideFlags.HideInHierarchy;
					AssetDatabase.AddObjectToAsset( moduleInstance, this );
				}
				catch( Exception e )
				{
					Console.WriteLine( e );
					throw;
				}
				
				m_Modules.Add( moduleInstance );
				EditorUtility.SetDirty( this );
			}

			return moduleInstance;
		}

		public bool RemoveModule( int index )
		{
			if( index < 0 || index >= m_Modules.Count )
				return false;
			if( m_Modules[index] == null )
				return true;
			
#if UNITY_2018_3_OR_NEWER
			AssetDatabase.RemoveObjectFromAsset( m_Modules[index] );
#else
			DestroyImmediate( m_Modules[index], true );
#endif
			m_Modules.RemoveAt( index );
			EditorUtility.SetDirty( this );
			return true;
		}
		
		public int CompareTo( AuditProfile other )
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