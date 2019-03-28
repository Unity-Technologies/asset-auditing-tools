using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetTools
{
	public enum AssetType
	{
		Texture,
		Model,
		Audio,
		Folder,
		Native,
		NA
	}
	
	[CreateAssetMenu(fileName = "NewAssetAuditorProfile", menuName = "Asset Tools/New Auditor Profile", order = 0)]
	public class AuditProfile : ScriptableObject
	{
		public bool m_RunOnImport = true;
		public bool m_FilterToFolder = true;
			
		public List<Filter> m_Filters;
		
		// TODO is these better as a list of IImportProcessModule?
		public ImporterPropertiesModule m_ImporterModule;
		public PreprocessorModule m_PreprocessorModule;

		public List<IConformObject> GetConformData( string asset )
		{
			// TODO each module
			return m_ImporterModule.GetConformObjects( asset );
		}

		public void ProcessAsset( AssetImporter asset )
		{
			if( Filter.Conforms( asset, m_Filters ) == false )
				return;
			if( !m_ImporterModule.CanProcess( asset ) )
				return;
			
			if( m_RunOnImport )
				m_ImporterModule.Apply( asset );
			else if( m_ImporterModule.IsManuallyProcessing( asset )  )
				m_ImporterModule.Apply( asset );
		}
	}
}