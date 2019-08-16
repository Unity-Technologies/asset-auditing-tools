using System.Collections.Generic;
using AssetTools.GUIUtility;
using UnityEditor;
using UnityEngine;

namespace AssetTools
{
	public class PostprocessorImportTaskInspector
	{
		private PostprocessorImportTask m_ImportTask;
		
		private SerializedProperty m_MethodSerializedProperty;
		private SerializedProperty m_DataSerializedProperty;

		public void Draw( SerializedObject moduleObject, ControlRect layout )
		{
			if( m_ImportTask == null )
			{
				m_ImportTask = moduleObject.targetObject as PostprocessorImportTask;
				if( m_ImportTask == null )
				{
					Debug.LogError( "SerializedObject must be of type PostprocessorImportTask" );
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
					Debug.LogError( "Invalid properties for PostprocessorImportTask" );
					return;
				}
			}
			
			List<ProcessorMethodInfo> methods = PostprocessorImplementorCache.Methods;
			GUIContent[] contents = new GUIContent[methods.Count+1];
			contents[0] = new GUIContent("None Selected");

			int selectedMethod = 0;
			for( int i=1; i<methods.Count+1; ++i )
			{
				contents[i] = new GUIContent(methods[i-1].TypeName);
				if( !string.IsNullOrEmpty( m_ImportTask.methodString ) )
				{
					if( string.Equals( m_ImportTask.methodString, methods[i - 1].TypeName + ", " + methods[i - 1].AssemblyName ) )
						selectedMethod = i;
				}
			}

			if( !string.IsNullOrEmpty( m_ImportTask.methodString ) && selectedMethod == 0 )
			{
				Debug.LogError( "methodString not found in project : " + m_ImportTask.methodString );
			}

			EditorGUI.BeginChangeCheck();
			selectedMethod = EditorGUI.Popup( layout.Get(), new GUIContent("Postprocessor methodString"), selectedMethod, contents );
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
						m_ImportTask.m_ProcessorMethodInfo = null;
					}
				}
			}

			EditorGUI.PropertyField( layout.Get(), m_DataSerializedProperty );
		}
	}
}