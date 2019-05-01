using System.Collections.Generic;
using AssetTools.GUIUtility;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	public class PreprocessorModuleInspector
	{
		private PreprocessorModule m_Module;
		
		private SerializedProperty m_MethodSerializedProperty;
		private SerializedProperty m_DataSerializedProperty;

		public void Draw( SerializedObject moduleObject, ControlRect layout )
		{
			if( m_Module == null )
			{
				m_Module = moduleObject.targetObject as PreprocessorModule;
				if( m_Module == null )
				{
					Debug.LogError( "SerializedObject must be of type PreprocessorModule" );
					return;
				}
			}
			
			if( m_MethodSerializedProperty == null || m_DataSerializedProperty == null )
			{
				List<string> propertyNames = new List<string> {"m_MethodString", "m_Data"};
				List<SerializedProperty> properties = SerializationUtilities.GetSerialisedPropertyCopiesForObject( moduleObject, propertyNames );
				m_MethodSerializedProperty = properties[0];
				m_DataSerializedProperty = properties[1];
				if( m_MethodSerializedProperty == null || m_DataSerializedProperty == null )
				{
					Debug.LogError( "Invalid properties for PreprocessorModule" );
					return;
				}
			}
			
			List<ProcessorMethodInfo> methods = PreprocessorImplementorCache.Methods;
			GUIContent[] contents = new GUIContent[methods.Count+1];
			contents[0] = new GUIContent("None Selected");

			int selectedMethod = 0;
			for( int i=1; i<methods.Count+1; ++i )
			{
				contents[i] = new GUIContent(methods[i-1].TypeName);
				if( !string.IsNullOrEmpty( m_Module.methodString ) )
				{
					if( string.Equals( m_Module.methodString, methods[i - 1].TypeName + ", " + methods[i - 1].AssemblyName ) )
						selectedMethod = i;
				}
			}

			if( !string.IsNullOrEmpty( m_Module.methodString ) && selectedMethod == 0 )
			{
				Debug.LogError( "methodString not found in project : " + m_Module.methodString );
			}

			EditorGUI.BeginChangeCheck();
			selectedMethod = EditorGUI.Popup( layout.Get(), new GUIContent("Preprocessor methodString"), selectedMethod, contents );
			if( EditorGUI.EndChangeCheck() )
			{
				if( selectedMethod == 0 )
					m_MethodSerializedProperty.stringValue = "";
				else
				{
					int id = selectedMethod - 1;
					if( id >= 0 )
					{
						m_MethodSerializedProperty.stringValue = methods[id].TypeName + ", " + methods[id].AssemblyName;
						m_Module.m_ProcessorMethodInfo = null;
					}
				}
			}

			EditorGUI.PropertyField( layout.Get(), m_DataSerializedProperty );
		}
	}
}