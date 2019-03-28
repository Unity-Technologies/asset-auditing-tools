using System.Collections.Generic;
using System.Reflection;
using AssetTools.GUIUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssetTools
{
	public class PreprocessorModuleInspector
	{
		private PreprocessorModule m_Module;
		
		private SerializedProperty m_ImporterReferenceSerializedProperty;
		private SerializedProperty m_PropertiesArraySerializedProperty;

		private int m_MethodSelected = 0;
		
		public void Draw( SerializedProperty property, ControlRect layout )
		{
			List<ProcessorMethodInfo> methods = PreprocessorImplementorCache.Methods;
			GUIContent[] contents = new GUIContent[methods.Count+1];
			contents[0] = new GUIContent("None Selected");
			
			for( int i=1; i<methods.Count+1; ++i )
			{
				contents[i] = new GUIContent(methods[i-1].m_ClassName);
			}

			EditorGUI.BeginChangeCheck();
			m_MethodSelected = EditorGUI.Popup( layout.Get(), new GUIContent("Preprocessor Method"), m_MethodSelected, contents );
			if( EditorGUI.EndChangeCheck() )
			{
				int id = m_MethodSelected - 1;
				if( id >= 0 )
				{
					// TODO set the method info to the module
					MethodInfo m = methods[id].m_MethodInfo;
				}

			}
		}

	}
}