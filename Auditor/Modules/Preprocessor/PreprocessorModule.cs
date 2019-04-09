using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	[Serializable]
	public class PreprocessorModule : IImportProcessModule
	{
		/// <summary>
		/// TODO If any of this is changed, do the Assets imported by it need to be reimported?
		/// </summary>
		
		
		
		private const string kModuleName = "PreprocessorModule";
		
		[SerializeField] private string m_MethodString;
		[SerializeField] private string m_Data;

		// used for locking it to a particular asset type
		public string m_SearchFilter;

		
		private List<string> m_AssetsToForceApply = new List<string>();

		public string methodString
		{
			get { return m_MethodString; }
		}

		public bool CanProcess( AssetImporter item )
		{
			return true;
		}
		
		public bool IsManuallyProcessing( AssetImporter item )
		{
			return m_AssetsToForceApply.Contains( item.assetPath );
		}
		
		
		public List<IConformObject> GetConformObjects( string asset, AuditProfile profile )
		{
			// Preprocessor versionCode comparison
			// will need someway to store this. It could not work well if imported not using it
			// 1: add it to meta data. Only option is userData, which could conflict with other code packages. This would make it included in the hash for cache server. Which would be required.
			// 2: store a database of imported version data. Could be tricky to keep in sync
			// 3: AssetDatabaseV2 supports asset dependencies
			
			List<IConformObject> infos = new List<IConformObject>();

			if( Method == null )
			{
				PreprocessorConformObject conformObject = new PreprocessorConformObject( "None Selected", 0, 0 );
				infos.Add( conformObject );
				return infos;
			}

			UserDataSerialization userData = UserDataSerialization.Get( asset );
			List<UserDataSerialization.PostprocessorData> data = userData.m_ImporterPostprocessorData.assetProcessedWith;
			string profileGuid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( profile ) );

			if( data != null )
			{
				for( int i = 0; i < data.Count; ++i )
				{
					if( data[i].moduleName != kModuleName ||
					    data[i].typeName != Method.TypeName ||
					    data[i].assemblyName != Method.AssemblyName ||
					    data[i].importDefinitionGUID != profileGuid )
						continue;

					PreprocessorConformObject conformObject = new PreprocessorConformObject( data[i].typeName, data[i].version, Method.Version );
					infos.Add( conformObject );
					break;
				}
			}
			else
			{
				PreprocessorConformObject conformObject = new PreprocessorConformObject( Method.TypeName, int.MinValue, Method.Version );
				infos.Add( conformObject );
			}
			return infos;
		}

		public bool GetSearchFilter( out string typeFilter, List<string> ignoreAssetPaths )
		{
			typeFilter = m_SearchFilter;
			return true;
		}
		
		public void FixCallback( AssetDetailList calledFromTreeView, object context )
		{
			// TODO find out how to multi select
			// TODO if selection is a folder
			
			AssetViewItem selectedNodes = context as AssetViewItem;
			if( selectedNodes != null )
			{
				m_AssetsToForceApply.Add( selectedNodes.path );
				selectedNodes.ReimportAsset();
				
				foreach( IConformObject data in selectedNodes.conformData )
				{
					if( data is PreprocessorConformObject )
						data.Conforms = true;
				}
				
				selectedNodes.conforms = true;
				for( int i = 0; i < selectedNodes.conformData.Count; ++i )
				{
					if( selectedNodes.conformData[i].Conforms == false )
					{
						selectedNodes.conforms = false;
						break;
					}
				}
				
				calledFromTreeView.m_PropertyList.Reload();
			}
			else
				Debug.LogError( "Could not fix Asset with no Assets selected." );
		}

		private void SetUserData( AssetImporter importer, AuditProfile profile )
		{
			UserDataSerialization data = UserDataSerialization.Get( importer.assetPath );
			string profileGuid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( profile ) );
			data.m_ImporterPostprocessorData.UpdateOrAdd( new UserDataSerialization.PostprocessorData( profileGuid, kModuleName, Method.AssemblyName, Method.TypeName, Method.Version ) );
			data.UpdateImporterUserData();
		}
		
		public bool Apply( AssetImporter item, AuditProfile fromProfile )
		{
			if( string.IsNullOrEmpty( m_MethodString ) == false )
			{
				if( Method != null )
				{
					object returnValue = Method.Invoke( item, m_Data );
					if( returnValue != null )
					{
						SetUserData( item, fromProfile );
						return (bool) returnValue;
					}
				}
			}
			return false;
		}

		internal ProcessorMethodInfo m_ProcessorMethodInfo;

		private ProcessorMethodInfo Method
		{
			get
			{
				if( m_ProcessorMethodInfo == null && string.IsNullOrEmpty( m_MethodString ) == false )
				{
					string assemblyName;
					string typeString;
					GetMethodStrings( out assemblyName, out typeString );
					if( string.IsNullOrEmpty( typeString ) )
					{
						Debug.LogError( "Error collecting method from " + m_MethodString );
						return null;
					}
					
					List<ProcessorMethodInfo> methods = PreprocessorImplementorCache.Methods;
					for( int i = 0; i < methods.Count; ++i )
					{
						if( assemblyName != null && methods[i].AssemblyName.StartsWith( assemblyName ) == false )
							continue;

						if( methods[i].TypeName.EndsWith( typeString ) )
						{
							m_ProcessorMethodInfo = methods[i];
							break;
						}
					}
				}
		
				return m_ProcessorMethodInfo;
			}
		}
		
		public void GetMethodStrings( out string assemblyName, out string typeString )
		{
			int commaIndex = m_MethodString.IndexOf( ',' );
			if( commaIndex > 0 )
			{
				assemblyName = m_MethodString.Substring( commaIndex + 2 );
				typeString = m_MethodString.Substring( 0, commaIndex );
			}
			else
			{
				assemblyName = "";
				typeString = m_MethodString;
			}
		}
		
		/*
		 
		 // DECISION Would it be better to get the method at Apply time?
		
		internal MethodInfo m_MethodInfo = null;
		private static Assembly[] m_Assemblies;
		[InitializeOnLoadMethod]
		static void CollectAssemblies()
		{
			m_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
		}


		private static MethodInfo GetMethodInfo( string m_MethodInfoString )
		{
			Assembly selected = null;
			string typeString;
			string typeName;
			int commaIndex = m_MethodInfoString.IndexOf( ',' );
			
			if( commaIndex > 0 )
			{
				string assemblyName = m_MethodInfoString.Substring( commaIndex + 2 );
				// has an Assembly defined in the string. Use that
				for( int i = 0; i < m_Assemblies.Length; ++i )
				{
					if( m_Assemblies[i].FullName != assemblyName )
						continue;
					selected = m_Assemblies[i];
					break;
				}

				string methodInfoString = m_MethodInfoString.Substring( 0, commaIndex ) + ".OnPreprocessAsset";

				int lastStop = 0;
				for( int index = commaIndex; index >= 0; --index )
				{
					if( methodInfoString[index] == '.' )
					{
						lastStop = index;
						break;
					}
				}

				Assert.IsTrue( lastStop > 0, "Error: Invalid methodString string." );
				typeName = methodInfoString.Substring( lastStop+1 );
				typeString = methodInfoString.Substring( 0, lastStop );
			}
			else
			{
				m_MethodInfoString = m_MethodInfoString + ".OnPreprocessAsset";
				typeName = m_MethodInfoString.Substring( m_MethodInfoString.LastIndexOf( '.' ) + 1 );
				typeString = m_MethodInfoString.Substring( 0, m_MethodInfoString.LastIndexOf( '.' ) );
			}

			Type reflectedType = null;

			if( selected == null )
			{
				// search through all to find type
				for( int i = 0; i < m_Assemblies.Length; ++i )
				{
					reflectedType = m_Assemblies[i].GetType( typeString );
					if( reflectedType != null )
						break;
				}
			}
			else
				reflectedType = selected.GetType( typeString );

			if( reflectedType == null )
			{
				Debug.LogWarning( string.Format( "Invalid method address for PostProcessMethod for {0}. Could not find type", m_MethodInfoString ) );
				return null;
			}

			// will always be public void OnPreprocessAsset( AssetImporter importer, string data )
			MethodInfo postprocessorMethodToInvoke = reflectedType.GetMethod( typeName, BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new []{ typeof(AssetImporter), typeof(string) }, null );

			if( postprocessorMethodToInvoke == null )
				Debug.LogWarning( string.Format( "Invalid method address for PostProcessMethod for {0}. Could not find method", m_MethodInfoString ) );

			return postprocessorMethodToInvoke;
		}
		
		*/
	}
}