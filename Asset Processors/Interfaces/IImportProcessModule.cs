using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public interface IImportProcessModule
	{
		bool CanProcess( AssetImporter item );
		
		bool IsManuallyProcessing( AssetImporter item );
		
		string AssetMenuFixString
		{
			get;
		}
		
		void SetManuallyProcessing( List<string> assetPaths, bool value );
		
		List<IConformObject> GetConformObjects( string asset, AuditProfile profile );
		
		bool GetSearchFilter( out string searchFilter, List<string> ignoreAssetPaths );
		
		System.Type GetConformObjectType();
		
		bool Apply( AssetImporter importer, AuditProfile profile );
	}
}