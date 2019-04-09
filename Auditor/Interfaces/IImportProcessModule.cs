using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	public interface IImportProcessModule
	{
		bool CanProcess( AssetImporter item );
		
		bool IsManuallyProcessing( AssetImporter item );
		
		List<IConformObject> GetConformObjects( string asset, AuditProfile profile );
		bool GetSearchFilter( out string typeFilter, List<string> ignoreAssetPaths );

		void FixCallback( AssetDetailList calledFromTreeView, object context );
		
		bool Apply( AssetImporter importer, AuditProfile profile );
	}
}