using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	[Serializable]
	public class PreprocessorModule : IImportProcessModule
	{
		private static Assembly[] m_Assemblies;

		
		/// <summary>
		/// If any of this is changed, do the Assets imported by it need to be reimported?
		/// </summary>
		
		public string m_Method;
		public string m_Data;

		// used for locking it to a particular asset type
		public string m_SearchFilter;
		
		
		
		private List<string> m_AssetsToForceApply = new List<string>();
		public bool DoesProcess( AssetImporter item )
		{
			if( m_AssetsToForceApply.Contains( item.assetPath ) )
				return true;
			return false;
		}
		
		
		[InitializeOnLoadMethod]
		static void CollectAssemblies()
		{
			m_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
		}
		
		public List<IConformObject> GetConformObjects( string asset )
		{
			// TODO What can be done here?
			// Preprocessor versionCode compare?
			// will need someway to store this. It could not work well if imported not using it
			// 1: add it to meta data. Only option is userData, which could conflict with other code packages. This would make it included in the hash for cache server. Which would be required.
			// 2: store a database of imported version data. however if changed version of a method you want to process with. It would 
			List<IConformObject> infos = new List<IConformObject>();
			
			AssetImporter assetImporter = AssetImporter.GetAtPath( asset );
			
			
			//  assetImporter.userData // get m_Importer methodData (if not found = false) (if version not the same = false )
			
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
				// Set the userData so the import pipeline knows that it should be m_Importer with the method data
				SetUserData( selectedNodes.AssetImporter );
				selectedNodes.ReimportAsset();
				
				// if( Apply( selectedNodes.AssetImporter ) )
				// {
				// 	selectedNodes.ReimportAsset();
				// }
				
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

		private void SetUserData( AssetImporter importer )
		{
			// TODO this
		}
		
		public bool Apply( AssetImporter item )
		{
			if( string.IsNullOrEmpty( m_Method ) == false )
			{
				MethodInfo postprocessorMethodToInvoke = GetMethodInfo( m_Method );
				if( postprocessorMethodToInvoke != null )
				{
					object returnValue = postprocessorMethodToInvoke.Invoke( null, string.IsNullOrEmpty( m_Data ) ? null : new object[] {m_Data} );
					if( returnValue != null )
					{
						return (bool) returnValue;
					}
				}
			}
			return false;
		}

		private static MethodInfo GetMethodInfo( string m_Method )
		{
			Assembly selected = null;
			string typeString;
			string methodName;
			int commaIndex = m_Method.LastIndexOf( ',' );
			
			if( commaIndex > 0 )
			{
				string assemblyName = m_Method.Substring( commaIndex + 2 );
				// has an Assembly defined in the string. Use that
				for( int i = 0; i < m_Assemblies.Length; ++i )
				{
					if( m_Assemblies[i].GetName().Name != assemblyName )
						continue;
					selected = m_Assemblies[i];
					break;
				}

				int lastStop = 0;
				for( int index = commaIndex; index >= 0; --index )
				{
					if( m_Method[index] == '.' )
					{
						lastStop = index;
						break;
					}
				}

				Assert.IsTrue( lastStop > 0, "Error: Invalid Method string." );
				methodName = m_Method.Substring( lastStop+1, (commaIndex-lastStop) - 1 );
				typeString = m_Method.Substring( 0, lastStop );
			}
			else
			{
				methodName = m_Method.Substring( m_Method.LastIndexOf( '.' ) + 1 );
				typeString = m_Method.Substring( 0, m_Method.LastIndexOf( '.' ) );
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
				Debug.LogWarning( string.Format( "Invalid method address for PostProcessMethod for {0}. Could not find type", "_______________________" ) );
				return null;
			}

			MethodInfo postprocessorMethodToInvoke = reflectedType.GetMethod( methodName, BindingFlags.Static | BindingFlags.Public );

			if( postprocessorMethodToInvoke == null )
				Debug.LogWarning( string.Format( "Invalid method address for PostProcessMethod for {0}. Could not find method", "_____________________" ) );

			return postprocessorMethodToInvoke;
		}
	}
}