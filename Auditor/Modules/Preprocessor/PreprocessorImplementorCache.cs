using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	public struct ProcessorMethodInfo
	{
		public MethodInfo m_MethodInfo;
		public string m_AssemblyName;
		public string m_ClassName;
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
						ProcessorMethodInfo mi = new ProcessorMethodInfo();
						mi.m_ClassName = types[t].FullName;
						mi.m_AssemblyName = assemblies[i].FullName;
						mi.m_MethodInfo = m;
						m_Methods.Add( mi );
					}
					else
						Debug.LogError( "Could not find OnPreprocessAsset on "+ types[t].Name );
				}
			}
		}
		
		private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
			// TODO decide if want to keep track of anything that implement IPreprocessor and cache it
			for( int i = 0; i < movedFromAssetPaths.Length; ++i )
			{
			}

			for( int i = 0; i < importedAssets.Length; ++i )
			{
				if( importedAssets[i].EndsWith( ".cs" ) == false )
					continue;
				
				// TODO MonoScript would limit the class to the same standard as others (one per file, like MonoBehaviour/ScriptableObject)
				MonoScript s = AssetDatabase.LoadAssetAtPath<MonoScript>( importedAssets[i] );
				if( s == null )
					continue;
				Type t = s.GetClass();
				if( t.IsInterface )
					continue;
				if( !typeof(IPreprocessor).IsAssignableFrom( t ) )
					continue;
				
				
			}
			
			for( int i = 0; i < deletedAssets.Length; ++i )
			{
			}
		}
	}
}