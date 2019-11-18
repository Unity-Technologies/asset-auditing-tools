using System.Collections.Generic;
using System.Reflection;
using AssetTools.GUIUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssetTools
{
	public class ImporterPropertiesImportTaskInspector
	{
		private ImporterPropertiesImportTask m_ImportTask;
		
		private SerializedProperty m_ImporterReferenceSerializedProperty;
		private SerializedProperty m_ConstrainPropertiesSerializedProperty;
		
		public void Draw( SerializedObject importTaskObject, ControlRect layout )
		{
			if( m_ImportTask == null )
			{
				m_ImportTask = importTaskObject.targetObject as ImporterPropertiesImportTask;
				if( m_ImportTask == null )
				{
					Debug.LogError( "SerializedObject must be of type ImporterPropertiesImportTask" );
					return;
				}
			}
			
			if( m_ImporterReferenceSerializedProperty == null || m_ConstrainPropertiesSerializedProperty == null )
			{
				m_ImporterReferenceSerializedProperty = importTaskObject.FindProperty( "m_ImporterReference" );
				m_ConstrainPropertiesSerializedProperty = importTaskObject.FindProperty( "m_ConstrainProperties" );
				if( m_ImporterReferenceSerializedProperty == null || m_ConstrainPropertiesSerializedProperty == null )
				{
					Debug.LogError( "Invalid properties for ImporterPropertiesImportTask" );
					return;
				}
			}
			
			if( m_ImporterReferenceSerializedProperty != null )
				ReferenceObjectGUI( layout );
			
			if( m_ImporterReferenceSerializedProperty.objectReferenceValue != null )
			{
				ConstrainToPropertiesGUI( layout );
			}
			else
			{
				Rect r = layout.Get( 25 );
				EditorGUI.HelpBox( r, "No template Object to constrain properties on.", MessageType.Warning );
			}
		}

		private void ReferenceObjectGUI( ControlRect layout )
		{
			using( var check = new EditorGUI.ChangeCheckScope() )
			{
				EditorGUI.PropertyField( layout.Get(), m_ImporterReferenceSerializedProperty, new GUIContent( "Importer Template" ), false );
				if( check.changed )
				{
					m_ImportTask.m_AssetImporter = null;
					m_ImportTask.GatherProperties();
				}
			}
		}
		
		private void ConstrainToPropertiesGUI( ControlRect layout )
		{
			layout.Space( 10 );
			EditorGUI.LabelField( layout.Get(), "Constrain to Properties:" );

			Rect boxAreaRect = layout.Get( Mathf.Max( (m_ImportTask.PropertyCount * layout.layoutHeight) + 6, 20 ) );
			GUI.Box( boxAreaRect, GUIContent.none );

			ControlRect subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, layout.layoutHeight )
			{
				padding = 0
			};
			
			// list all of the displayNames
			for( int i = 0; i < m_ImportTask.PropertyCount; ++i )
			{
				EditorGUI.LabelField( subLayout.Get(), m_ImportTask.GetPropertyDisplayName( i ) );
			}

			Rect layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 100);
			layoutRect.width = 100;
			if( GUI.Button( layoutRect, "Edit Selection" ) )
			{
				string[] propertyNamesForReference = GetPropertyNames( new SerializedObject( m_ImportTask.ReferenceAssetImporter ) );
				
				m_ImportTask.GatherPropertiesIfNeeded();
				GenericMenu menu = new GenericMenu();
				foreach( string propertyName in propertyNamesForReference )
				{
					// we do not want UserData to be included. We are required to use this in order to save information about
					// how the Asset is imported, to generate a different hash for the cache server
					if( propertyName.Contains( "m_UserData" ) )
						continue;
					
					bool isPropertySelected = m_ImportTask.m_ConstrainProperties.Contains( propertyName );
					string propertyDisplayName = m_ImportTask.GetPropertyDisplayName( propertyName );
					menu.AddItem( new GUIContent( propertyDisplayName ), isPropertySelected, TogglePropertyConstraintSelected, propertyDisplayName );
				}
				
				menu.ShowAsContext();
			}
		}

		private void TogglePropertyConstraintSelected( object selectedObject )
		{
			string propertyName = selectedObject as string;
			Assert.IsNotNull( propertyName );

			for( int i = 0; i < m_ImportTask.m_ConstrainPropertiesDisplayNames.Count; ++i )
			{
				if( m_ImportTask.m_ConstrainPropertiesDisplayNames[i].Equals( propertyName ) )
				{
					m_ConstrainPropertiesSerializedProperty.DeleteArrayElementAtIndex( i );
					return;
				}
			}
			
			m_ConstrainPropertiesSerializedProperty.arraySize += 1;
			m_ConstrainPropertiesSerializedProperty.GetArrayElementAtIndex( m_ConstrainPropertiesSerializedProperty.arraySize-1 ).stringValue = m_ImportTask.GetPropertyRealName( propertyName ) ;
			m_ImportTask.GatherDisplayNames();
		}

		private static string[] GetPropertyNames( SerializedObject serializedObject )
		{
			SerializedProperty soIter = serializedObject.GetIterator();

			List<string> propNames = new List<string>();

			soIter.NextVisible(true);
			do
			{
				propNames.Add(soIter.name);
			} while (soIter.NextVisible(false));

			return propNames.ToArray();
		}
	}
}