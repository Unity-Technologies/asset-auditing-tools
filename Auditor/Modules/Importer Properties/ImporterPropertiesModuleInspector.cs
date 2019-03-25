using System.Collections.Generic;
using AssetTools.GUIUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssetTools
{
	public class ImporterPropertiesModuleInspector
	{
		// TODO constructor to setup << why did I add this?
		
		private ImporterPropertiesModule m_Module;
		
		private SerializedProperty m_ImporterReferenceSerializedProperty;
		private SerializedProperty m_PropertiesArraySerializedProperty;
		
		
		public void Draw( SerializedProperty property, ControlRect layout )
		{
			if( m_ImporterReferenceSerializedProperty == null || m_PropertiesArraySerializedProperty == null )
			{
				List<string> propertyNames = new List<string> {"m_ImporterReference", "m_ConstrainProperties"};
				List<SerializedProperty> properties = SerializationUtilities.FindPropertiesInClass( property, propertyNames );
				m_ImporterReferenceSerializedProperty = properties[0];
				m_PropertiesArraySerializedProperty = properties[1];
				if( m_ImporterReferenceSerializedProperty == null || m_PropertiesArraySerializedProperty == null )
				{
					Debug.LogError( "Invalid properties for ImporterPropertiesModule" );
					return;
				}
			}
			
			if( m_Module == null )
			{
				AuditProfile profile = property.serializedObject.targetObject as AuditProfile;
				if( profile == null )
				{
					Debug.LogError( "ImporterPropertiesModule must be apart of a profile Object" );
					return;
				}
				m_Module = profile.m_ImporterModule;
			}

			if( m_ImporterReferenceSerializedProperty != null )
			{
				using( var check = new EditorGUI.ChangeCheckScope() )
				{
					EditorGUI.PropertyField( layout.Get(), m_ImporterReferenceSerializedProperty, new GUIContent( "Importer Template" ) );
					if( check.changed )
					{
						m_Module.m_AssetImporter = null;
						m_Module.GatherProperties();
					}
				}
			}
			
			if( m_ImporterReferenceSerializedProperty.objectReferenceValue != null )
				ConstrainToPropertiesArea( layout );
			else
			{
				Rect r = layout.Get( 25 );
				EditorGUI.HelpBox( r, "No template Object to constrain properties on.", MessageType.Warning );
			}
		}
		
		private void ConstrainToPropertiesArea( ControlRect layout )
		{
			layout.Space( 10 );
			EditorGUI.LabelField( layout.Get(), "Constrain to Properties:" );

			Rect boxAreaRect = layout.Get( Mathf.Max( (m_Module.PropertyCount * layout.layoutHeight) + 6, 20 ) );
			GUI.Box( boxAreaRect, GUIContent.none );

			ControlRect subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, layout.layoutHeight )
			{
				padding = 0
			};
			
			// list all of the displayNames
			for( int i = 0; i < m_Module.PropertyCount; ++i )
			{
				EditorGUI.LabelField( subLayout.Get(), m_Module.GetPropertyDisplayName( i ) );
			}

			Rect layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 100);
			layoutRect.width = 100;
			if( GUI.Button( layoutRect, "Edit Selection" ) )
			{
				string[] propertyNamesForReference = GetPropertyNames( new SerializedObject( m_Module.ReferenceAssetImporter ) );
				
				m_Module.GatherPropertiesIfNeeded();
				GenericMenu menu = new GenericMenu();
				foreach( string propertyName in propertyNamesForReference )
				{
					// we do not want UserData to be included. We are required to use this in order to save information about
					// how the Asset is imported, to generate a different hash for the cache server
					if( propertyName.Contains( "m_UserData" ) )
						continue;
					
					bool isPropertySelected = m_Module.m_ConstrainProperties.Contains( propertyName );
					string propertyDisplayName = m_Module.GetPropertyDisplayName( propertyName );
					menu.AddItem( new GUIContent( propertyDisplayName ), isPropertySelected, TogglePropertyConstraintSelected, propertyDisplayName );
				}
				
				menu.ShowAsContext();
			}
		}

		private void TogglePropertyConstraintSelected( object selectedObject )
		{
			string propertyName = selectedObject as string;
			Assert.IsNotNull( propertyName );

			for( int i = 0; i < m_Module.m_ConstrainPropertiesDisplayNames.Count; ++i )
			{
				if( m_Module.m_ConstrainPropertiesDisplayNames[i].Equals( propertyName ) )
				{
					m_PropertiesArraySerializedProperty.DeleteArrayElementAtIndex( i );
					return;
				}
			}
			
			m_PropertiesArraySerializedProperty.arraySize += 1;
			m_PropertiesArraySerializedProperty.GetArrayElementAtIndex( m_PropertiesArraySerializedProperty.arraySize-1 ).stringValue = m_Module.GetPropertyRealName( propertyName ) ;
			m_Module.GatherDisplayNames();
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