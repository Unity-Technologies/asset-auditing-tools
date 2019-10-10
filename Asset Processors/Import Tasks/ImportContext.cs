using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{

	public class ImportContext
	{
		public AssetImporter Importer;
		
		private Dictionary<string,object> contextData = new Dictionary<string, object>();

		public void Add( string key, object data )
		{
			if( contextData.ContainsKey( key ) )
				contextData[key] = data;
			else
				contextData.Add( key, data );
		}

		public object Get( string key )
		{
			object rtn;
			if( contextData.TryGetValue( key, out rtn ) )
				return rtn;
			Debug.LogError( "Could not find context data for " + key );

			return null;
		}

		public string AssetPath
		{
			get
			{
				if( Importer == null )
					return null;
				return Importer.assetPath;
			}
		}

		/// <summary>
		/// GetContextData for the Texture during the OnPostprocessTexture event
		/// </summary>
		public Texture2D PostprocessingTexture
		{
			get
			{
				object rtn;
				if( contextData.TryGetValue( "Texture2D", out rtn ) )
					return rtn as Texture2D;
				return null;
			}
			set
			{
				Add( "Texture2D", value );
			}
		}
	}

}