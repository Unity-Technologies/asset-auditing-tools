using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using AssetTools.GUIUtility;
using UnityEngine.Assertions;
using Object = System.Object;

namespace AssetTools
{
	[CustomEditor( typeof(ImportDefinitionProfile) )]
	public class ImportDefinitionProfileInspector : Editor
	{
		private ImportDefinitionProfile m_Profile;
		
		private SerializedProperty m_ProcessOnImport;
		private SerializedProperty m_FolderOnly;
		private SerializedProperty m_FiltersListProperty;
		private SerializedProperty m_Tasks;
		private SerializedProperty m_SortIndex;
		
		List<Type> m_ImportTaskTypes;
		
		// TODO serialiseFoldoutState??
		List<bool> m_ImportTaskFoldoutStates = new List<bool>();

		void OnEnable()
		{
			m_ProcessOnImport = serializedObject.FindProperty( "m_RunOnImport" );
			m_FolderOnly = serializedObject.FindProperty( "m_FilterToFolder" );
			m_FiltersListProperty = serializedObject.FindProperty( "m_Filters" );
			m_Tasks = serializedObject.FindProperty( "m_ImportTasks" );
			m_SortIndex = serializedObject.FindProperty( "m_SortIndex" );
			
			m_ImportTaskTypes = ImportDefinitionProfileCache.GetTypes<BaseImportTask>();
		}
		
		public override void OnInspectorGUI()
		{
			if( m_Profile == null )
				m_Profile = (ImportDefinitionProfile) target;
      
			//while asset is being created, path can't be determined, which throws an exception when setting up the filters
			if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m_Profile)))
				return;
        
			Rect viewRect = GUILayoutUtility.GetRect( EditorGUIUtility.currentViewWidth-23, 0 );
			ControlRect layout = new ControlRect( viewRect.x, viewRect.y, viewRect.width );
			
			layout.Space( 10 );
			EditorGUI.PropertyField( layout.Get(), m_FolderOnly, new GUIContent("Lock to folder", "Include a filter to limit this profile to this profile only"));
			EditorGUI.PropertyField( layout.Get(), m_ProcessOnImport, new GUIContent("Process On Import"));
			EditorGUI.PropertyField( layout.Get(), m_SortIndex, new GUIContent("Sort Index"));
			layout.Space( 10 );
			
			EditorGUI.LabelField( layout.Get(), "Search Filter's" );

			List<Filter> filters = m_Profile.GetFilters( true );
			if( filters == null )
				filters = new List<Filter>();

			int filterCount = filters.Count;
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
			
			EditorGUI.LabelField( layout.Get(), "", UnityEngine.GUI.skin.horizontalSlider);
			
			size = m_Tasks.arraySize;
			for( int i = 0; i < size; ++i )
			{
				if( m_ImportTaskFoldoutStates.Count-1 < i )
					m_ImportTaskFoldoutStates.Add( true );
				
				SerializedProperty taskProperty = m_Tasks.GetArrayElementAtIndex( i );
				BaseImportTask importTask = taskProperty.objectReferenceValue as BaseImportTask;
				if( importTask == null )
					continue;

				if( i > 0 )
					layout.Space( 10 );

				Rect headerRect = layout.Get( 20 );
				m_ImportTaskFoldoutStates[i] = EditorGUI.Foldout( headerRect, m_ImportTaskFoldoutStates[i], importTask.name, true );
				
				Event current = Event.current;
				if( headerRect.Contains( current.mousePosition ) )
				{
					if( (current.type == EventType.MouseDown && current.button == 1) || current.type == EventType.ContextClick )
					{
						GenericMenu menu = new GenericMenu();
						if( i == 0 )
							menu.AddDisabledItem( new GUIContent( "Move Up" ) );
						else
							menu.AddItem( new GUIContent( "Move Up" ), false, MoveTaskUpCallback, i );
						if( i == size-1 )
							menu.AddDisabledItem( new GUIContent( "Move Down" ) );
						else
							menu.AddItem( new GUIContent( "Move Down" ), false, MoveTaskDownCallback, i );
						
						menu.AddSeparator( "" );
						menu.AddItem( new GUIContent( "Delete Import Task" ), false, RemoveTaskCallback, i );
						menu.ShowAsContext();
						current.Use();
					}
					else if( current.type == EventType.MouseDown && current.button == 0 )
					{
						m_ImportTaskFoldoutStates[i] = !m_ImportTaskFoldoutStates[i];
					}
				}

				if( m_ImportTaskFoldoutStates[i] )
				{
					layout.BeginArea( 5, 5 );

					importTask.DrawGUI( layout );

					GUI.depth = GUI.depth - 1;
					GUI.Box( layout.EndArea(), "" );
					GUI.depth = GUI.depth + 1;
				}
			}
			
			if( size > 0 )
				EditorGUI.LabelField( layout.Get(), "", UnityEngine.GUI.skin.horizontalSlider);
			
			layoutRect = layout.Get();
			if( layoutRect.width > 120 )
			{
				layoutRect.x = layoutRect.x + (layoutRect.width - 120);
				layoutRect.width = 120;
			}
			
			if (EditorGUI.DropdownButton( layoutRect, new GUIContent("Add Import Task", "Add new Task to this Definition."), FocusType.Keyboard))
			{
				var menu = new GenericMenu();
				for (int i = 0; i < m_ImportTaskTypes.Count; i++)
				{
					var type = m_ImportTaskTypes[i];
					menu.AddItem(new GUIContent(type.Name, ""), false, OnAddImportTask, type);
				}

				menu.ShowAsContext();
			}
			
			GUILayoutUtility.GetRect( viewRect.width, layoutRect.y + layoutRect.height );
			serializedObject.ApplyModifiedProperties();
		}

		void RemoveTaskCallback( object context )
		{
			int index = (int) context;
			// Ask first?
			if( m_Profile.RemoveTask( index ) )
			{
				m_Tasks.DeleteArrayElementAtIndex( index );
				m_Tasks.MoveArrayElement( index, m_Tasks.arraySize - 1 );
				m_Tasks.arraySize = m_Tasks.arraySize - 1;
				
				m_ImportTaskFoldoutStates.RemoveAt( index );
				
				EditorUtility.SetDirty( m_Profile );
				Repaint();
			}
		}
		
		void MoveTaskUpCallback( object context )
		{
			int index = (int) context;
			m_Tasks.MoveArrayElement( index, index-1 );
			Repaint();
		}
		
		void MoveTaskDownCallback( object context )
		{
			int index = (int) context;
			m_Tasks.MoveArrayElement( index, index+1 );
			Repaint();
		}
		
		void OnAddImportTask(object context)
		{
			Type t = context as Type;
			Assert.IsNotNull( t, "Null ImportTask Type" );

			BaseImportTask addedImportTask = m_Profile.AddTask( t );
			if( addedImportTask != null )
			{
				// keep the serialised property in sync
				m_Tasks.arraySize++;
				m_Tasks.GetArrayElementAtIndex( m_Tasks.arraySize-1 ).objectReferenceValue = addedImportTask;
				m_ImportTaskFoldoutStates.Add( true );
				
				EditorUtility.SetDirty( m_Profile );
				Repaint();
			}
		}
		
	}
}
