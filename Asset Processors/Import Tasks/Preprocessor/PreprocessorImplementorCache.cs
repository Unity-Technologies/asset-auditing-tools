using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	
	public class ProcessorMethodInfo
	{
		private readonly Type m_Type;
		private readonly MethodInfo m_MethodInfo;
		private MethodInfo m_VersionMethodInfo;
		private object m_Instance;
		

		public ProcessorMethodInfo( Type type, MethodInfo info )
		{
			m_Type = type;
			m_MethodInfo = info;
			m_Instance = null;
		}
		
		public string TypeName
		{
			get { return m_Type.FullName; }
		}

		public string AssemblyName
		{
			get { return m_Type.Assembly.FullName; }
		}

		public int Version
		{
			get
			{
				if( m_VersionMethodInfo == null )
				{
					m_VersionMethodInfo = m_Type.GetMethod( "GetVersion" );
					if( m_VersionMethodInfo == null )
					{
						Debug.LogError( m_Type.FullName + " does not implement GetVersion" );
						return 0;
					}
				}
				object o = m_VersionMethodInfo.Invoke( Instance, null );
				return (int) o;
			}
		}

		private object Instance
		{
			get { return m_Instance ?? (m_Instance = Activator.CreateInstance( m_Type )); }
		}

		public object Invoke( AssetImporter importer, string data )
		{
			return Instance == null ? null : m_MethodInfo.Invoke( m_Instance, new object[] {importer, data} );
		}
	}

	public class PreprocessorImplementorCache : AssetPostprocessor
	{
		private static List<ProcessorMethodInfo> m_Methods;

		public static List<ProcessorMethodInfo> Methods
		{
			get
			{
				if( m_Methods == null )
				{
					m_Methods = new List<ProcessorMethodInfo>();
					FindMethods();
				}
				return m_Methods;
			}
		}

		/// <summary>
		/// Brute force get all the methods that implement IPreprocessor
		/// May not want this as it limits the ability to move over to MonoScript dependency in AssetDatabaseV2
		/// </summary>
		private static void FindMethods()
		{
			Type p = typeof(IPreprocessor);
			
			m_Methods.Clear();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for( int i=0; i<assemblies.Length; ++i )
			{
				// TODO skip some assemblies we know we would not want to search
				Type[] types = assemblies[i].GetTypes();
				for( int t = 0; t < types.Length; ++t )
				{
					if( !types[t].IsClass || types[t].IsInterface || !p.IsAssignableFrom( types[t] ) )
						continue;
					
					MethodInfo m = types[t].GetMethod( "OnPreprocessAsset" );
					if( m != null )
					{
						m_Methods.Add( new ProcessorMethodInfo( types[t], m ) );
					}
					else
						Debug.LogError( "Could not find OnPreprocessAsset on "+ types[t].Name );
				}
			}
		}
		
		// // TODO MonoScripts will allow for AssetDatabaseV2 to have an Asset dependant on the MonoScript, so it is included in the asset hash without .userData
		// private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		// {
		// 	// TODO decide if want to keep track of anything that implement IPreprocessor and cache it
		// 	for( int i = 0; i < movedFromAssetPaths.Length; ++i )
		// 	{
		// 	}
		//
		// 	for( int i = 0; i < importedAssets.Length; ++i )
		// 	{
		// 		if( importedAssets[i].EndsWith( ".cs" ) == false )
		// 			continue;
		// 		
		// 		// TODO MonoScript would limit the class to the same standard as others (one per file, like MonoBehaviour/ScriptableObject)
		// 		MonoScript s = AssetDatabase.LoadAssetAtPath<MonoScript>( importedAssets[i] );
		// 		if( s == null )
		// 			continue;
		// 		Type t = s.GetClass();
		// 		if( t.IsInterface )
		// 			continue;
		// 		if( !typeof(IPreprocessor).IsAssignableFrom( t ) )
		// 			continue;
		// 		
		//
		// 	}
		// 	
		// 	for( int i = 0; i < deletedAssets.Length; ++i )
		// 	{
		// 	}
		// }
	}
}