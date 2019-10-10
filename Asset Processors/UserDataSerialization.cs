using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace AssetTools
{
	public class UserDataSerialization
	{
		// TODO this clears every domain reload. See if we can improve this
		private static List<UserDataSerialization> m_Cache = new List<UserDataSerialization>();
		
		
		const string searchString = "\"ImportDefinitionFiles\": { ";
		
		[Serializable]
		public struct PostprocessorData
		{
			/// <summary>
			/// The guid of the Import Definition File that processed this asset
			/// </summary>
			public string importDefinitionGUID;
			
			/// <summary>
			/// The importTask name of the importTask that processed this asset
			/// </summary>
			public string moduleName;
			
			/// <summary>
			/// The assembly the Method used to process is contained
			/// </summary>
			public string assemblyName;
			
			/// <summary>
			/// The typeName of the class that is Invoked
			/// </summary>
			public string typeName;
			
			/// <summary>
			/// The version number of the processing method
			/// </summary>
			public int version;

			public PostprocessorData( string importDefinitionGUID, string moduleName, string assemblyName, string typeName, int version )
			{
				this.importDefinitionGUID = importDefinitionGUID;
				this.moduleName = moduleName;
				this.assemblyName = assemblyName;
				this.typeName = typeName;
				this.version = version;
			}
		}
		
		[Serializable]
		public struct ImportTaskData
		{
			/// <summary>
			/// The guid of the Import Definition File that processed this asset
			/// </summary>
			public string importDefinitionGUID;
			
			/// <summary>
			/// The importTask name of the importTask that processed this asset
			/// </summary>
			public string taskName;

			/// <summary>
			/// The version number of the task
			/// </summary>
			public int version;

			public ImportTaskData( string importDefinitionGUID, string taskName, int version )
			{
				this.importDefinitionGUID = importDefinitionGUID;
				this.taskName = taskName;
				this.version = version;
			}
		}
		
		/// <summary>
		/// A list of serialised data for the Processing done on the asset
		/// </summary>
		[Serializable]
		public struct PostprocessorDataList
		{
			[FormerlySerializedAs( "assetProcessedWith" )]
			public List<PostprocessorData> assetProcessedWithMethods;
			
			public List<ImportTaskData> assetProcessedWithTasks;

			public void UpdateOrAdd( PostprocessorData d )
			{
				if( assetProcessedWithMethods == null )
					assetProcessedWithMethods = new List<PostprocessorData>();
				for( int i = 0; i < assetProcessedWithMethods.Count; ++i )
				{
					if( assetProcessedWithMethods[i].importDefinitionGUID == d.importDefinitionGUID &&
					    assetProcessedWithMethods[i].moduleName == d.moduleName )
					{
						PostprocessorData p = assetProcessedWithMethods[i];
						p.assemblyName = d.assemblyName;
						p.typeName = d.typeName;
						p.version = d.version;
						assetProcessedWithMethods[i] = p;
						return;
					}
				}
				assetProcessedWithMethods.Add( d );
			}
			
			public void UpdateOrAdd( ImportTaskData d )
			{
				if( assetProcessedWithTasks == null )
					assetProcessedWithTasks = new List<ImportTaskData>();
				for( int i = 0; i < assetProcessedWithTasks.Count; ++i )
				{
					if( assetProcessedWithTasks[i].importDefinitionGUID == d.importDefinitionGUID &&
					    assetProcessedWithTasks[i].taskName == d.taskName )
					{
						ImportTaskData p = assetProcessedWithTasks[i];
						p.version = d.version;
						assetProcessedWithTasks[i] = p;
						return;
					}
				}
				assetProcessedWithTasks.Add( d );
			}
		}
		
		
		private PostprocessorDataList m_ImporterPostprocessorData;
		private AssetImporter m_Importer;
		private string m_ImporterJson;

		//private int m_UserDataStartIndex;
		//private int m_UserDataEndIndex;


		public List<PostprocessorData> GetProcessedMethodsData()
		{
			// TODO make a copy?
			return m_ImporterPostprocessorData.assetProcessedWithMethods;
		}
		
		public List<ImportTaskData> GetProcessedTasksData()
		{
			// TODO make a copy?
			return m_ImporterPostprocessorData.assetProcessedWithTasks;
		}

		public UserDataSerialization( string assetPath )
		{
			m_Importer = AssetImporter.GetAtPath( assetPath );
			if( m_Importer == null )
			{
				Debug.LogError( "Could not find AssetImporter for " + assetPath );
				return;
			}
			
			ParseMetaFile();
		}

		/// <summary>
		/// Get a userData representation for processing on the asset at assetPath
		/// </summary>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public static UserDataSerialization Get( string assetPath )
		{
			for( int i = 0; i < m_Cache.Count; ++i )
			{
				if( m_Cache[i].m_Importer != null )
				{
					if( m_Cache[i].m_Importer.assetPath == assetPath )
						return m_Cache[i];
				}
				else
				{
					Debug.LogError( "AssetImporter is null for Asset - this is unexpect to happen and a bug" );
				}
			}

			UserDataSerialization ud = new UserDataSerialization( assetPath );
			if( ud.m_Importer != null )
			{
				m_Cache.Add(ud);
				return m_Cache[m_Cache.Count - 1];
			}
			
			return null;
		}
		
		private void ParseMetaFile()
		{
			GetImporterJson();
			PostprocessorDataList importersPostprocessorData = new PostprocessorDataList();
			if( ! string.IsNullOrEmpty( m_ImporterJson ))
				importersPostprocessorData = JsonUtility.FromJson<PostprocessorDataList>( m_ImporterJson );
			m_ImporterPostprocessorData = importersPostprocessorData;
		}

		private void GetImporterJson()
		{
			Assert.IsNotNull( m_Importer );

			string userData = m_Importer.userData;
			
			int idfStartIndex = userData.IndexOf( searchString, StringComparison.Ordinal );
			int idfEndIndex = -1;
			if( idfStartIndex != -1 )
			{
				idfEndIndex = idfStartIndex + searchString.Length;
				int counter = 0;
				int startIndex = idfEndIndex;
				while( idfEndIndex < userData.Length )
				{
					if( userData[idfEndIndex] == '{' )
						counter++;
					else if( userData[idfEndIndex] == '}' )
						counter--;

					if( counter == -1 )
						break;
					++idfEndIndex;
				}
				Assert.AreEqual( -1, counter );

				int length = idfEndIndex - startIndex;
				m_ImporterJson = userData.Substring( startIndex, length );
				if( m_ImporterJson.Length > 0 )
				{
					for( int i = m_ImporterJson.Length - 1; i >= 0; --i )
					{
						if( m_ImporterJson[i] == ' ' )
							m_ImporterJson = m_ImporterJson.Remove( i );
						else
							break;
					}
				}
			}
		}

		public void UpdateProcessing( PostprocessorData d )
		{
			m_ImporterPostprocessorData.UpdateOrAdd( d );
		}
		
		public void UpdateProcessing( ImportTaskData d )
		{
			m_ImporterPostprocessorData.UpdateOrAdd( d );
		}
		
		public void SaveMetaData()
		{
			//GetImporterJson(); // use this when testing
			string json = JsonUtility.ToJson( m_ImporterPostprocessorData );
			if( string.Equals( json, m_ImporterJson ) )
				return;
			
			string importDefinitionFileUserData = "\"ImportDefinitionFiles\": { " + json + " }";

			string userData = m_Importer.userData;
			
			int idfStartIndex = userData.IndexOf( searchString, StringComparison.Ordinal );
			int idfEndIndex = -1;
			if( idfStartIndex != -1 )
			{
				idfEndIndex = idfStartIndex + searchString.Length;
				int counter = 0;
				int startIndex = idfEndIndex;
				while( idfEndIndex < userData.Length )
				{
					if( userData[idfEndIndex] == '{' )
						counter++;
					else if( userData[idfEndIndex] == '}' )
						counter--;

					if( counter == -1 )
						break;
					++idfEndIndex;
				}
				Assert.AreEqual( -1, counter );
			}
			
			if( idfStartIndex >= 0 && idfEndIndex > idfStartIndex )
			{
				int length = idfEndIndex - idfStartIndex;
				if( userData.Length < idfStartIndex + length )
				{
					Debug.LogError( "Problem setting user data" );
				}
				
				if( importDefinitionFileUserData == userData.Substring( idfStartIndex, length ) )
				{
					Debug.LogError( "Bad checks" );
					return; //
				}
				
				userData = userData.Remove( idfStartIndex, (idfEndIndex - idfStartIndex)+1 );
			}
			else
				idfStartIndex = 0;

			m_ImporterJson = json;
			m_Importer.userData = userData.Insert( idfStartIndex, importDefinitionFileUserData );
		}
	}

}