using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	struct AuditProfileData
	{
		public string m_FolderPath;
		public string m_AssetPath;
		public AuditProfile m_AuditProfile;
	}

	public class ProfileCache : AssetPostprocessor
	{
		private static List<AuditProfileData> s_Profiles = new List<AuditProfileData>();
		
		/// <summary>
		/// A List of AuditProfiles within the project and their locations
		/// </summary>
		internal static List<AuditProfileData> Profiles
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
			string[] guids = AssetDatabase.FindAssets( "t:AuditProfile" );
			for( int i = 0; i < guids.Length; ++i )
			{
				string path = AssetDatabase.GUIDToAssetPath( guids[i] );
				AuditProfile profile = AssetDatabase.LoadAssetAtPath<AuditProfile>( path );
				s_Profiles.Add( new AuditProfileData
				{
					m_AssetPath = path,
					m_FolderPath = path.Remove( path.LastIndexOf( '/' ) ),
					m_AuditProfile = profile
				} );
			}
		}
		
		/// <summary>
		/// Keep the profile list in sync with the project.
		/// TODO Could keep track of profiles without [InitialiseOnLoadMethod] with validation instead?
		/// </summary>
		/// <param name="importedAssets"></param>
		/// <param name="deletedAssets"></param>
		/// <param name="movedAssets"></param>
		/// <param name="movedFromAssetPaths"></param>
		private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
			for( int i = 0; i < movedFromAssetPaths.Length; ++i )
			{
				for( int d = 0; d < s_Profiles.Count; ++d )
				{
					
					if( s_Profiles[d].m_AssetPath == movedFromAssetPaths[i] )
					{
						AuditProfileData def = s_Profiles[d];
						def.m_AssetPath = movedAssets[i];
						def.m_FolderPath = def.m_AssetPath.Remove( def.m_AssetPath.LastIndexOf( '/' ) );
						s_Profiles[d] = def;
						break;
					}
				}
			}

			for( int i = 0; i < importedAssets.Length; ++i )
			{
				if( importedAssets[i].EndsWith( ".asset" ) == false )
					continue;
				AuditProfile profile = AssetDatabase.LoadAssetAtPath<AuditProfile>( importedAssets[i] );
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
					AuditProfileData item = new AuditProfileData();
					item.m_AssetPath = importedAssets[i];
					item.m_FolderPath = item.m_AssetPath.Remove( item.m_AssetPath.LastIndexOf( '/' ) );
					item.m_AuditProfile = profile;
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
	}
}