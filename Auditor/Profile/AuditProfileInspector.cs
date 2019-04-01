using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using AssetTools.GUIUtility;

namespace AssetTools
{
	[CustomEditor( typeof(AuditProfile) )]
	public class AuditProfileInspector : Editor
	{
		private AuditProfile m_Profile;
		
		private SerializedProperty m_ProcessOnImport;
		private SerializedProperty m_FolderOnly;
		private SerializedProperty m_FiltersListProperty;
		private SerializedProperty m_ImporterModule;
		private SerializedProperty m_PreprocessorModule;
		private SerializedProperty m_SortIndex;

		private ImporterPropertiesModuleInspector propertiesModuleInspector = new ImporterPropertiesModuleInspector();
		private PreprocessorModuleInspector PreprocessorModuleInspector = new PreprocessorModuleInspector();

		void OnEnable()
		{
			m_ProcessOnImport = serializedObject.FindProperty("m_RunOnImport" );
			m_FolderOnly = serializedObject.FindProperty("m_FilterToFolder" );
			m_FiltersListProperty = serializedObject.FindProperty("m_Filters" );
			m_ImporterModule = serializedObject.FindProperty("m_ImporterModule" );
			m_PreprocessorModule = serializedObject.FindProperty("m_PreprocessorModule" );
			m_SortIndex = serializedObject.FindProperty("m_SortIndex" );
		}
		
		public override void OnInspectorGUI()
		{
			if( m_Profile == null )
				m_Profile = (AuditProfile) target;

			Rect viewRect = EditorGUILayout.GetControlRect();
			ControlRect layout = new ControlRect( viewRect.x, viewRect.y, viewRect.width );
			
			layout.Space( 10 );
			EditorGUI.PropertyField( layout.Get(), m_FolderOnly, new GUIContent("Lock to folder", "Include a filter to limit this profile to this profile only"));
			EditorGUI.PropertyField( layout.Get(), m_ProcessOnImport, new GUIContent("Process On Import"));
			EditorGUI.PropertyField( layout.Get(), m_SortIndex, new GUIContent("Sort Index"));
			layout.Space( 10 );
			
			EditorGUI.LabelField( layout.Get(), "Search Filter's" );
			
			if( m_Profile.m_Filters == null )
				m_Profile.m_Filters = new List<Filter>();

			int filterCount = m_Profile.m_Filters.Count;
			if( m_FolderOnly.boolValue )
				filterCount++;

			Rect boxAreaRect = layout.Get( (16 * filterCount) + (3 * filterCount) + 6 );
			GUI.Box( boxAreaRect, GUIContent.none );

			ControlRect subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, 16 );
			subLayout.padding = 3;

			int removeAt = -1;
			int size = m_FiltersListProperty.arraySize;
			if( m_FolderOnly.boolValue )
				size++;
			
			for( int i = 0; i < size; ++i )
			{
				Rect segmentRect = subLayout.Get();
				segmentRect.x += 3;
				segmentRect.width -= 6;

				float segWidth = (segmentRect.width - segmentRect.height) / 3;
				segmentRect.width = segWidth - 3;
				float startX = segmentRect.x;
				
				if( m_FolderOnly.boolValue && i == size-1 )
				{
					EditorGUI.BeginDisabledGroup( true );
					EditorGUI.EnumPopup( segmentRect, Filter.ConditionTarget.Directory );
					segmentRect.x = startX + segWidth;
					EditorGUI.EnumPopup( segmentRect, Filter.Condition.StartsWith );
					segmentRect.x = startX + (segWidth * 2);
					EditorGUI.TextField( segmentRect, m_Profile.DirectoryPath );
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					// TODO how do you get properties for this without looping all???
					SerializedProperty filterProperty = m_FiltersListProperty.GetArrayElementAtIndex( i );
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
							segmentRect.x = startX + (segWidth * 2);
							EditorGUI.PropertyField( segmentRect, filterProperty, GUIContent.none );
						}
					} while( filterProperty.NextVisible( false ) );

					segmentRect.x = startX + (segWidth * 3);
					segmentRect.width = segmentRect.height;
					if( GUI.Button( segmentRect, "-" ) )
						removeAt = i;
				}
			}
			
			if( removeAt >= 0 )
				m_FiltersListProperty.DeleteArrayElementAtIndex( removeAt );

			Rect layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 40);
			layoutRect.width = 40;
			if( GUI.Button( layoutRect, "Add" ) )
			{
				m_FiltersListProperty.arraySize += 1;
			}

			layout.Space( 20 );
			
			// TODO do the rest as modules that can be added
			propertiesModuleInspector.Draw( m_ImporterModule, layout );
			PreprocessorModuleInspector.Draw( m_PreprocessorModule, layout );
			

			serializedObject.ApplyModifiedProperties();
		}
		
		
	}
}