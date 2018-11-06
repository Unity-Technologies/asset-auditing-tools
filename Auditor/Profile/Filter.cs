using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssetTools
{

	[System.Serializable]
	public class Filter
	{
		public enum ConditionTarget
		{
			Filename = 0,
			FullFilename,
			FolderName,
			Directory,
			Extension,
			AssetBundle,
			FileSize,
			Labels
			// TODO investigate more using post processing, Texture width etc. This will require multiple imports which can result in slow import times
		}

		public enum Condition
		{
			Contains = 0,
			Equals,
			Regex,
			DoesNotContain,
			StartsWith,
			EndsWith,
			GreaterThan,
			GreaterThanEqual,
			LessThan,
			LessThanEqual
		}

		public ConditionTarget m_Target;
		public Condition m_Condition;
		public string m_Wildcard;

		public Filter()
		{
			m_Target = ConditionTarget.Filename;
			m_Condition = Condition.Contains;
			m_Wildcard = "";

		}

		public Filter( ConditionTarget target, Condition condition, string wildcard )
		{
			m_Target = target;
			m_Condition = condition;
			m_Wildcard = wildcard;
		}

		public static bool Conforms( string path, IList<Filter> filters )
		{
			return Conforms( AssetImporter.GetAtPath( path ), filters );
		}

		public static bool Conforms( AssetImporter importer, IList<Filter> filters )
		{
			if( filters == null || filters.Count == 0 )
				return true;

			FileInfo fi = new FileInfo( importer.assetPath );
			DirectoryInfo di = new DirectoryInfo( importer.assetPath );
			
			Assert.IsTrue( fi.Exists || di.Exists );

			for( int i = 0; i < filters.Count; ++i )
			{
				switch( filters[i].m_Target )
				{
					case ConditionTarget.Filename:
						string name = fi.Name;
						name = name.Remove( name.Length - fi.Extension.Length );
						if( !Target( name, filters[i] ) )
							return false;
						break;
					case ConditionTarget.FullFilename:
						if( !Target( importer.assetPath, filters[i] ) )
							return false;
						break;
					case ConditionTarget.FolderName:
						if( fi.Directory == null || !Target( fi.Directory.Name, filters[i] ) )
							return false;
						break;
					case ConditionTarget.Directory:
						string path = importer.assetPath;
						if( !Target( path.Remove( path.Length - fi.Name.Length - 1 ), filters[i] ) )
							return false;
						break;
					case ConditionTarget.Extension:
						if( !Target( fi.Extension, filters[i] ) )
							return false;
						break;
					case ConditionTarget.FileSize:
						if( !Target( fi.Length, filters[i] ) )
							return false;
						break;
					case ConditionTarget.AssetBundle:
						if( !Target( importer.assetBundleName, filters[i] ) )
							return false;
						break;
					case ConditionTarget.Labels:
						// TODO get labels and check, each individually?
						Debug.Log( "need to implement this" );
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return true;
		}

		private static bool Target( string target, Filter filter )
		{
			// TODO decide on if case sensitivity should be disabled
			switch( filter.m_Condition )
			{
				case Condition.Equals:
					return target.Equals( filter.m_Wildcard );
				case Condition.Contains:
					return target.Contains( filter.m_Wildcard );
				case Condition.DoesNotContain:
					return !target.Contains( filter.m_Wildcard );
				case Condition.EndsWith:
					return target.EndsWith( filter.m_Wildcard );
				case Condition.StartsWith:
					return target.StartsWith( filter.m_Wildcard );
				case Condition.Regex:
					return Regex.IsMatch( target, filter.m_Wildcard );
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static bool Target( long target, Filter filter )
		{
			int wildcard;
			if( int.TryParse( filter.m_Wildcard, out wildcard ) == false )
			{
				Debug.LogError( string.Format( "Wildcard as number is not long parsable \"{0}\"", filter.m_Wildcard ) );
				return false;
			}

			switch( filter.m_Condition )
			{
				case Condition.Equals:
					return target.Equals( wildcard );
				case Condition.LessThan:
					return target < wildcard;
				case Condition.LessThanEqual:
					return target <= wildcard;
				case Condition.GreaterThan:
					return target > wildcard;
				case Condition.GreaterThanEqual:
					return target >= wildcard;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

}