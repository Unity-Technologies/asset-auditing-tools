using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{

	public class PostprocessorImplementorCache : AssetPostprocessor
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
		/// Brute force get all the methods that implement IPostprocessor
		/// May not want this as it limits the ability to move over to MonoScript dependency in AssetDatabaseV2
		/// </summary>
		private static void FindMethods()
		{
			Type p = typeof(IPostprocessor);
			
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
					
					MethodInfo m = types[t].GetMethod( "OnPostprocessAsset" );
					if( m != null )
					{
						m_Methods.Add( new ProcessorMethodInfo( types[t], m ) );
					}
					else
						Debug.LogError( "Could not find OnPostprocessAsset on "+ types[t].Name );
				}
			}
		}
		
		// // TODO MonoScripts will allow for AssetDatabaseV2 to have an Asset dependant on the MonoScript, so it is included in the asset hash without .userData
		// private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		// {
		// 	// TODO decide if want to keep track of anything that implement IPostprocessor and cache it
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
		// 		if( !typeof(IPostprocessor).IsAssignableFrom( t ) )
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