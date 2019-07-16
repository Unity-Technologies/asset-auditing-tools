using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public class AssetsTreeViewItem : TreeViewItem
	{
		internal bool conforms { get; set; }

		public string path = "";
		public bool isAsset;

		public List<ConformData> conformData;
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

		internal AssetsTreeViewItem( int id, int depth, string displayName, bool assetConforms ) : base( id, depth, displayName )
		{
			conforms = assetConforms;
		}

		// This is done after setting manually. But if running through the modules during import pipeline. Then perhaps simply reimport and let it handle it.
		// if manually done (window view) then add to a list of force runOnImport and reset at PostProcessAllAssets  
		public void ReimportAsset()
		{
			if( !isAsset )
				return;
			
			EditorUtility.SetDirty( AssetImporter );
			AssetImporter.SaveAndReimport();
		}

		public void Refresh()
		{
			icon = AssetDatabase.GetCachedIcon( path ) as Texture2D;
			
			if( conformData != null )
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