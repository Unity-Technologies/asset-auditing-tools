using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	struct ProfileData : IComparer<ProfileData>, IComparable<ProfileData>
	{
		public string m_AssetPath;
		public ImportDefinitionProfile m_ImportDefinitionProfile;
		public int Compare( ProfileData x, ProfileData y )
		{
			return x.m_ImportDefinitionProfile.CompareTo( y.m_ImportDefinitionProfile );
		}

		public int CompareTo( ProfileData other )
		{
			return m_ImportDefinitionProfile.CompareTo( other.m_ImportDefinitionProfile );
		}
	}

	public class ImportDefinitionProfileCache : AssetPostprocessor
	{
		private static List<ProfileData> s_Profiles = new List<ProfileData>();
		
		/// <summary>
		/// A List of AuditProfiles within the project and their locations
		/// </summary>
		internal static List<ProfileData> Profiles
		{
			get
			{
				return s_Profiles;
			}
		}
		
		/// <summary>
		/// Whenever there is a domain reload fine all assets of type profile
		/// </summary>
		[InitializeOnLoadMethod]
		private static void RefreshCacheObjects()
		{
			s_Profiles.Clear();
			string[] guids = AssetDatabase.FindAssets( "t:ImportDefinitionProfile" );
			for( int i = 0; i < guids.Length; ++i )
			{
				string path = AssetDatabase.GUIDToAssetPath( guids[i] );
				ImportDefinitionProfile profile = AssetDatabase.LoadAssetAtPath<ImportDefinitionProfile>( path );
				s_Profiles.Add( new ProfileData
				{
					m_AssetPath = path,
					m_ImportDefinitionProfile = profile
				} );
			}
			s_Profiles.Sort();
		}
		
		/// <summary>
		/// Keep the profile list in sync with the project.
		/// TODO Could keep track of profiles without [InitialiseOnLoadMethod] with validation instead?
		/// </summary>
		/// <param name="importedAssets"></param>
		/// <param name="deletedAssets"></param>
		/// <param name="movedToAssetPaths"></param>
		/// <param name="movedFromAssetPaths"></param>
		private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedToAssetPaths, string[] movedFromAssetPaths )
		{
			for( int i = 0; i < movedFromAssetPaths.Length; ++i )
			{
				for( int d = 0; d < s_Profiles.Count; ++d )
				{
					if( s_Profiles[d].m_AssetPath == movedFromAssetPaths[i] )
					{
						ProfileData def = s_Profiles[d];
						def.m_AssetPath = movedToAssetPaths[i];
						def.m_ImportDefinitionProfile.DirectoryPath = null;
						s_Profiles[d] = def;
						break;
					}
				}
			}

			for( int i = 0; i < importedAssets.Length; ++i )
			{
				if( importedAssets[i].EndsWith( ".asset" ) == false )
					continue;
				ImportDefinitionProfile profile = AssetDatabase.LoadAssetAtPath<ImportDefinitionProfile>( importedAssets[i] );
				if( profile == null )
					continue;

				bool isInCache = false;
				for( int d = 0; d < s_Profiles.Count; ++d )
				{
					if( s_Profiles[d].m_AssetPath == importedAssets[i] )
					{
						isInCache = true;
						break;
					}
				}

				if( !isInCache )
				{
					ProfileData item = new ProfileData();
					item.m_AssetPath = importedAssets[i];
					item.m_ImportDefinitionProfile = profile;
					profile.DirectoryPath = null;
					s_Profiles.Add( item );
				}
			}
			
			for( int i = 0; i < deletedAssets.Length; ++i )
			{
				for( int d = 0; d < s_Profiles.Count; ++d )
				{
					if( s_Profiles[d].m_AssetPath == deletedAssets[i] )
					{
						s_Profiles.RemoveAt( d );
						break;
					}
				}
			}
		}
		
		
		
		
		
		
		
		
		
		
		
		/// <summary>
        /// Get all types that can be assigned to type T
        /// </summary>
        /// <typeparam name="T">The class type to use as the base class or interface for all found types.</typeparam>
        /// <returns>A list of types that are assignable to type T.  The results are cached.</returns>
        public static List<Type> GetTypes<T>()
        {
            return TypeManager<T>.Types;
        }

        /// <summary>
        /// Get all types that can be assigned to type rootType.
        /// </summary>
        /// <param name="rootType">The class type to use as the base class or interface for all found types.</param>
        /// <returns>A list of types that are assignable to type T.  The results are not cached.</returns>
        public static List<Type> GetTypes(Type rootType)
        {
            return TypeManager.GetManagerTypes(rootType);
        }

        class TypeManager
        {
            public static List<Type> GetManagerTypes(Type rootType)
            {
                var types = new List<Type>();
                try
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
#if NET_4_6
                                if (a.IsDynamic)
                                    continue;
                                foreach (var t in a.ExportedTypes)
#else
                        foreach (var t in a.GetExportedTypes())
#endif
                        {
                            if (t != rootType && rootType.IsAssignableFrom(t) && !t.IsAbstract)
                                types.Add(t);
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                return types;
            }
        }

        class TypeManager<T> : TypeManager
        {
            // ReSharper disable once StaticMemberInGenericType
            static List<Type> s_Types;
            public static List<Type> Types
            {
                get
                {
                    if (s_Types == null)
                        s_Types = GetManagerTypes(typeof(T));

                    return s_Types;
                }
            }
        }
	}
}