using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public interface IImportTask
	{
		string ImportTaskName { get; }

		int Version { get; }

		/// <summary>
		/// UI
		/// </summary>

		bool GetSearchFilter( out string searchFilter, List<string> ignoreAssetPaths );
		
		string AssetMenuFixString
		{
			get;
		}

		/// <summary>
		/// Checking conformity
		/// </summary>

		List<IConformObject> GetConformObjects( string asset, ImportDefinitionProfile profile );
		
		System.Type GetConformObjectType();
		
		/// <summary>
		/// Processing
		/// </summary>
		
		bool CanProcess( AssetImporter item );
		
		bool IsManuallyProcessing( AssetImporter item );
		
		void SetManuallyProcessing( List<string> assetPaths, bool value );

		void PreprocessTask( ImportContext context, ImportDefinitionProfile profile );

		bool Apply( ImportContext context, ImportDefinitionProfile profile );
	}
}