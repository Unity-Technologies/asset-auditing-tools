using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public interface IImportTask
	{
		bool CanProcess( AssetImporter item );
		
		bool IsManuallyProcessing( AssetImporter item );
		
		void SetManuallyProcessing( List<string> assetPaths, bool value );
		
		bool GetSearchFilter( out string searchFilter, List<string> ignoreAssetPaths );
		
		string AssetMenuFixString
		{
			get;
		}

		List<IConformObject> GetConformObjects( string asset, ImportDefinitionProfile profile );
		
		System.Type GetConformObjectType();

		bool Apply( AssetImporter importer, ImportDefinitionProfile profile );
	}
}