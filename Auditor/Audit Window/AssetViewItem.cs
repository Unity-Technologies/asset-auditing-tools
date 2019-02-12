using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	internal class AssetViewItem : TreeViewItem
	{
		internal bool conforms { get; set; }

		public string path = "";
		public bool isAsset;

		public List<IConformObject> conformData;
		public SerializedObject assetObject;
		

		private AssetImporter m_AssetImporter;
		internal AssetImporter AssetImporter
		{
			get
			{
				if( m_AssetImporter == null )
					m_AssetImporter = AssetImporter.GetAtPath( path );
				return m_AssetImporter;
			}
		}

		internal AssetViewItem( int id, int depth, string displayName, bool assetConforms ) : base( id, depth, displayName )
		{
			conforms = assetConforms;
		}

		public void ReimportAsset()
		{
			if( !isAsset )
				return;
			
			AssetImporter.SaveAndReimport();
			icon = AssetDatabase.GetCachedIcon( path ) as Texture2D;
			
			if( !conforms )
			{
				conforms = true;
				for( int i = 0; i < conformData.Count; ++i )
				{
					if( conformData[i].Conforms == false )
					{
						conforms = false;
						break;
					}
				}
			}
		}
	}
}