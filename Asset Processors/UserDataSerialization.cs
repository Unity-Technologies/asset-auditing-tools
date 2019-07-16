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
		
		public PostprocessorDataList m_ImporterPostprocessorData;
		public AssetImporter m_Importer;
		
		private int m_UserDataStartIndex;
		private int m_UserDataEndIndex;
		
		private static List<UserDataSerialization> m_Cache = new List<UserDataSerialization>();

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

			m_Cache.Add( ParseForAssetPath( assetPath ) );
			return m_Cache[m_Cache.Count-1];
		}
		
		/// <summary>
		/// Parses the userData for the assetPath to PostprocessorData 
		/// </summary>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		private static UserDataSerialization ParseForAssetPath( string assetPath )
		{
			AssetImporter importer = AssetImporter.GetAtPath( assetPath );
			Assert.IsNotNull( importer );
			
			string userData = importer.userData;
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
				//idfEndIndex += 2;
			}
			
			UserDataSerialization returnData = new UserDataSerialization();
			returnData.m_Importer = importer;
			returnData.m_ImporterPostprocessorData = importersPostprocessorData;
			returnData.m_UserDataStartIndex = idfStartIndex;
			returnData.m_UserDataEndIndex = idfEndIndex;

			return returnData;
		}

		/// <summary>
		/// Set the userData for this Object
		/// </summary>
		public void UpdateImporterUserData()
		{
			string json = JsonUtility.ToJson( m_ImporterPostprocessorData );
			string importDefinitionFileUserData = "\"ImportDefinitionFiles\": { " + json + " }";

			string userData = m_Importer.userData;
			if( m_UserDataStartIndex >= 0 && m_UserDataEndIndex > m_UserDataStartIndex )
			{
				if( importDefinitionFileUserData == userData.Substring( m_UserDataStartIndex, m_UserDataEndIndex - m_UserDataStartIndex ) )
					return;
				
				userData = userData.Remove( m_UserDataStartIndex, m_UserDataEndIndex - m_UserDataStartIndex );
			}
			else
				m_UserDataStartIndex = 0;
			
			m_Importer.userData = userData.Insert( m_UserDataStartIndex, importDefinitionFileUserData );
			m_UserDataEndIndex = importDefinitionFileUserData.Length + m_UserDataStartIndex;
			EditorUtility.SetDirty( m_Importer );
			AssetDatabase.WriteImportSettingsIfDirty( m_Importer.assetPath );
		}
	}

}