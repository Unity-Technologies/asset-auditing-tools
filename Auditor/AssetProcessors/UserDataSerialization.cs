using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	public class UserDataSerialization
	{
		const string searchString = "\"ImportDefinitionFiles\": { ";
		
		[Serializable]
		public struct PostprocessorData
		{
			public string importDefintionPath;
			public string moduleName;
			public string assemblyName;
			public string methodName;
			public int version;
		}
		
		[Serializable]
		public struct PostprocessorDataList
		{
			public List<PostprocessorData> assetProcessedWith;

			public void Add( PostprocessorData d )
			{
				assetProcessedWith.Add( d );
			}
		}
		
		public PostprocessorDataList m_ImporterPostprocessorData;
		
		private AssetImporter m_Importer;
		private int m_UserDataStartIndex;
		private int m_UserDataEndIndex;
		
		public static UserDataSerialization ParseForAssetPath( string assetPath )
		{
			PostprocessorDataList importersPostprocessorData = new PostprocessorDataList();
			AssetImporter importer = AssetImporter.GetAtPath( assetPath );
			string userData = importer.userData;
			int idfStartIndex = userData.IndexOf( searchString );
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
				
				// TODO make sure its not out of bounds / broken (3rd party stuff may mess with it)

				string str = userData.Substring( startIndex, idfEndIndex - startIndex );
				importersPostprocessorData = JsonUtility.FromJson<PostprocessorDataList>( str );
				idfEndIndex += 2;
			}
			
			UserDataSerialization returnData = new UserDataSerialization();
			returnData.m_Importer = importer;
			returnData.m_UserDataEndIndex = idfEndIndex;
			returnData.m_ImporterPostprocessorData = importersPostprocessorData;
			returnData.m_UserDataStartIndex = idfStartIndex;

			return returnData;
		}

		public void UpdateImporter()
		{
			string json = JsonUtility.ToJson( m_ImporterPostprocessorData );
			string importDefinitionFileUserData = "\"ImportDefinitionFiles\": { " + json + " }";

			string empty = m_Importer.userData.Remove( m_UserDataStartIndex, m_UserDataEndIndex - m_UserDataStartIndex );
			m_Importer.userData = empty.Insert( m_UserDataStartIndex, importDefinitionFileUserData );
			m_UserDataEndIndex = importDefinitionFileUserData.Length + m_UserDataStartIndex;
			EditorUtility.SetDirty( m_Importer );
			AssetDatabase.WriteImportSettingsIfDirty( m_Importer.assetPath );
		}
	}

}