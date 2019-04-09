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
		
		// TODO is these better as a list of IImportProcessModule?
		public ImporterPropertiesModule m_ImporterModule;
		public PreprocessorModule m_PreprocessorModule;

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
			// TODO each module
			List<IConformObject> lst = m_ImporterModule.GetConformObjects( asset );
			lst.AddRange( m_PreprocessorModule.GetConformObjects( asset ) );
			
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
				m_ImporterModule.Apply( asset, this );
				m_PreprocessorModule.Apply( asset, this );
			}
			else
			{
				if( m_ImporterModule.IsManuallyProcessing( asset ) )
					m_ImporterModule.Apply( asset, this );
				if( m_PreprocessorModule.IsManuallyProcessing( asset ) )
					m_PreprocessorModule.Apply( asset, this );
			}
		}

		public int CompareTo( AuditProfile other )
		{
			if( other == null )
				return 1;
			
			int s = m_SortIndex.CompareTo( other.m_SortIndex );
			if( s == 0 )
			{
				int me = DirectoryPath.Length;
				int o = other.DirectoryPath.Length;
				int lengthCompare = DirectoryPath.Length.CompareTo( other.DirectoryPath.Length );
				// if in same index, sort by shortest path length first
				return lengthCompare;
			}
			return s;
		}
	}
}