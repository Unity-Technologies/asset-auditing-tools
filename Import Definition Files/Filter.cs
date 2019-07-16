using System;
using System.Collections.Generic;
using System.ComponentModel;
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
			AssetBundleName,
			FileSize,
			ImporterType,
			// TODO remove labels??
			Labels // Labels are a bad use-case if doing Preprocessors step - They cannot be obtained until after the import. It is possible to read the meta file. But if it is not Text based or formatting changes it could run into trouble

			// TODO Can get original Texture width/height via reflection, test if can get it during preimport
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
			AssetImporter importerForPath = AssetImporter.GetAtPath( path );
			if( importerForPath == null )
			{
				Debug.LogError( "Could not find AssetImporter for " + path );
				return false;
			}
			return Conforms( importerForPath, filters );
		}

		public static bool Conforms( AssetImporter importer, IList<Filter> filters )
		{
			if( importer == null || filters == null || filters.Count == 0 )
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
						if( ! string.IsNullOrEmpty( fi.Extension ) )
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
					case ConditionTarget.AssetBundleName:
						if( !Target( importer.assetBundleName, filters[i] ) )
							return false;
						break;
					case ConditionTarget.Labels:
						string[] wildLabels = filters[i].m_Wildcard.Split( ',' );
						string[] labels = AssetDatabase.GetLabels( AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( importer.assetPath ) );
						switch( filters[i].m_Condition )
						{
							case Condition.Equals:
								if( wildLabels.Length != labels.Length )
									return false;
								for( int wlId = 0; wlId < wildLabels.Length; ++wlId )
								{
									bool contains = false;
									for( int lId = 0; lId < labels.Length; ++lId )
									{
                                        if( wildLabels[wlId].Equals( labels[lId], StringComparison.OrdinalIgnoreCase ) )
										{
											contains = true;
											break;
										}
									}
									if( contains == false )
										return false;
								}
								return true;
							case Condition.Contains:
                                for (int wlId = 0; wlId < wildLabels.Length; ++wlId)
                                {
                                    bool contains = false;
                                    for (int lId = 0; lId < labels.Length; ++lId)
                                    {
                                        if( wildLabels[wlId].Equals(labels[lId], StringComparison.OrdinalIgnoreCase) )
                                        {
                                            contains = true;
                                            break;
                                        }
                                    }
                                    if( contains == false )
                                        return false;
                                }
                                return true;
							case Condition.DoesNotContain:
                                for (int wlId = 0; wlId < wildLabels.Length; ++wlId)
                                {
                                    for (int lId = 0; lId < labels.Length; ++lId)
                                    {
                                        if (wildLabels[wlId].Equals(labels[lId], StringComparison.OrdinalIgnoreCase))
                                            return false;
                                    }
                                }
                                return true;
						}

						break;
					case ConditionTarget.ImporterType:
						if( !Target( importer.GetType().Name, filters[i] ) )
							return false;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return true;
		}

		private static bool Target( string target, Filter filter )
		{
			Assert.IsNotNull( target );
			Assert.IsNotNull( filter );
			Assert.IsNotNull( filter.m_Wildcard );
			
			switch( filter.m_Condition )
			{
				case Condition.Equals:
					return target.Equals( filter.m_Wildcard, StringComparison.OrdinalIgnoreCase );
				case Condition.Contains:
					return target.Contains( filter.m_Wildcard );
				case Condition.DoesNotContain:
					return !target.Contains( filter.m_Wildcard );
				case Condition.EndsWith:
					return target.EndsWith( filter.m_Wildcard, StringComparison.OrdinalIgnoreCase );
				case Condition.StartsWith:
					return target.StartsWith( filter.m_Wildcard, StringComparison.OrdinalIgnoreCase );
				case Condition.Regex:
					return Regex.IsMatch( target, filter.m_Wildcard );
				default:
					throw new Exception( );
			}
		}

		private static bool Target( long target, Filter filter )
		{
			Assert.IsNotNull( filter );
			Assert.IsNotNull( filter.m_Wildcard );
			
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