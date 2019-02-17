using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AssetTools.GUIUtility;

namespace AssetTools
{
	[CustomEditor( typeof(AuditProfile) )]
	public class AuditProfileInspector : Editor
	{
		private AuditProfile m_Profile;
		
		SerializedProperty m_ProcessOnImport;
		private SerializedProperty filtersListProperty;
		private SerializedProperty importerModule;

		private ImporterPropertiesModuleInspector propertiesModuleInspector = new ImporterPropertiesModuleInspector();

		void OnEnable()
		{
			m_ProcessOnImport = serializedObject.FindProperty("m_RunOnImport" );
			filtersListProperty = serializedObject.FindProperty("m_Filters" );
			importerModule = serializedObject.FindProperty("m_ImporterModule" );
		}
		
		public override void OnInspectorGUI()
		{
			if( m_Profile == null )
				m_Profile = (AuditProfile) target;

			Rect viewRect = EditorGUILayout.GetControlRect();
			ControlRect layout = new ControlRect( viewRect.x, viewRect.y, viewRect.width );
			
			layout.Space( 10 );
			EditorGUI.PropertyField( layout.Get(), m_ProcessOnImport, new GUIContent("Process On Import"));
			layout.Space( 10 );
			
			EditorGUI.LabelField( layout.Get(), "Search Filter's:" );
			
			if( m_Profile.m_Filters == null )
				m_Profile.m_Filters = new List<Filter>();

			Rect boxAreaRect = layout.Get( (16 * m_Profile.m_Filters.Count) + (3 * m_Profile.m_Filters.Count) + 6 );
			GUI.Box( boxAreaRect, GUIContent.none );

			ControlRect subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, 16 );
			subLayout.padding = 3;

			int removeAt = -1;
			for( int i = 0; i < filtersListProperty.arraySize; ++i )
			{
				Rect segmentRect = subLayout.Get();
				segmentRect.x += 3;
				segmentRect.width -= 6;

				float segWidth = (segmentRect.width - segmentRect.height) / 3;
				segmentRect.width = segWidth - 3;
				float startX = segmentRect.x;
				
				// TODO how do you get properties for this without looping all???
				SerializedProperty filterProperty = filtersListProperty.GetArrayElementAtIndex( i );
				filterProperty.NextVisible( true );
				do
				{
					if( filterProperty.propertyType == SerializedPropertyType.Enum && filterProperty.name == "m_Target" )
					{
						segmentRect.x = startX;
						EditorGUI.PropertyField( segmentRect, filterProperty, GUIContent.none );
					}
					else if( filterProperty.propertyType == SerializedPropertyType.Enum && filterProperty.name == "m_Condition" )
					{
						segmentRect.x = startX + segWidth;
						EditorGUI.PropertyField( segmentRect, filterProperty, GUIContent.none );
					}
					else if( filterProperty.propertyType == SerializedPropertyType.String && filterProperty.name == "m_Wildcard" )
					{
						segmentRect.x = startX + (segWidth*2);
						EditorGUI.PropertyField( segmentRect, filterProperty, GUIContent.none );
					}
				} while( filterProperty.NextVisible( false ) );
			
				segmentRect.x = startX + (segWidth*3);
				segmentRect.width = segmentRect.height;
				if( GUI.Button( segmentRect, "-" ) )
					removeAt = i;
			}
			
			if( removeAt >= 0 )
				filtersListProperty.DeleteArrayElementAtIndex( removeAt );

			Rect layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 40);
			layoutRect.width = 40;
			if( GUI.Button( layoutRect, "Add" ) )
			{
				filtersListProperty.arraySize += 1;
			}

			layout.Space( 20 );
			
			// TODO do the rest as modules that can be added
			propertiesModuleInspector.Draw( importerModule, layout );

			serializedObject.ApplyModifiedProperties();
		}
		
		
	}
}