using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssetTools
{
	public class UserDataSerialization
	{
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
		
		/// <summary>
		/// A list of serialised data for the Processing done on the asset
		/// </summary>
		[Serializable]
		public struct PostprocessorDataList
		{
			public List<PostprocessorData> assetProcessedWith;

			public void UpdateOrAdd( PostprocessorData d )
			{
				if( assetProcessedWith == null )
					assetProcessedWith = new List<PostprocessorData>();
				for( int i = 0; i < assetProcessedWith.Count; ++i )
				{
					if( assetProcessedWith[i].importDefinitionGUID == d.importDefinitionGUID &&
					    assetProcessedWith[i].moduleName == d.moduleName )
					{
						PostprocessorData p = assetProcessedWith[i];
						p.assemblyName = d.assemblyName;
						p.typeName = d.typeName;
						p.version = d.version;
						assetProcessedWith[i] = p;
						return;
					}
				}
				assetProcessedWith.Add( d );
			}
		}
		
		
		
		
		private PostprocessorDataList m_ImporterPostprocessorData;
		private AssetImporter m_Importer;
		
		private int m_UserDataStartIndex;
		private int m_UserDataEndIndex;

		public List<PostprocessorData> GetPostprocessorData()
		{
			// TODO make a copy?
			return m_ImporterPostprocessorData.assetProcessedWith;
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
			else
			{
				return null;
			}
		}
		
		private void ParseMetaFile()
		{
			Assert.IsNotNull( m_Importer );
			
			string userData = m_Importer.userData;
			int idfStartIndex = userData.IndexOf( searchString, StringComparison.Ordinal );
			int idfEndIndex = -1;
			
			PostprocessorDataList importersPostprocessorData = new PostprocessorDataList();
			
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

				string str = userData.Substring( startIndex, idfEndIndex - startIndex );
				importersPostprocessorData = JsonUtility.FromJson<PostprocessorDataList>( str );
			}
			
			m_ImporterPostprocessorData = importersPostprocessorData;
			m_UserDataStartIndex = idfStartIndex;
			m_UserDataEndIndex = idfEndIndex;
		}

		public void UpdateProcessing( PostprocessorData d )
		{
			m_ImporterPostprocessorData.UpdateOrAdd( d );
			SaveMetaData();
		}
		
		private void SaveMetaData()
		{
			string json = JsonUtility.ToJson( m_ImporterPostprocessorData );
			string importDefinitionFileUserData = "\"ImportDefinitionFiles\": { " + json + " }";

			string userData = m_Importer.userData;
			if( m_UserDataStartIndex >= 0 && m_UserDataEndIndex > m_UserDataStartIndex )
			{
				int length = m_UserDataEndIndex - m_UserDataStartIndex;
				if( userData.Length < m_UserDataStartIndex + length )
				{
					Debug.LogError( "Problem setting user data" );
				}
				if( importDefinitionFileUserData == userData.Substring( m_UserDataStartIndex, length ) )
					return;
				
				userData = userData.Remove( m_UserDataStartIndex, m_UserDataEndIndex - m_UserDataStartIndex );
			}
			else
				m_UserDataStartIndex = 0;
			
			m_Importer.userData = userData.Insert( m_UserDataStartIndex, importDefinitionFileUserData );
			m_UserDataEndIndex = importDefinitionFileUserData.Length + m_UserDataStartIndex;
			EditorUtility.SetDirty( m_Importer );
			AssetDatabase.WriteImportSettingsIfDirty( m_Importer.assetPath );
			
			// TODO need to update the anchor points in a more optimised way
			ParseMetaFile();
		}
	}

}