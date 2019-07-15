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
	[CustomEditor( typeof(AuditProfile) )]
	public class AuditProfileInspector : Editor
	{
		private AuditProfile m_Profile;
		
		private SerializedProperty m_ProcessOnImport;
		private SerializedProperty m_FolderOnly;
		private SerializedProperty m_FiltersListProperty;
		private SerializedProperty m_Modules;
		private SerializedProperty m_SortIndex;
		
		List<Type> m_ModuleTypes;
		
		// TODO serialiseFoldoutState??
		List<bool> m_ModuleFoldoutStates = new List<bool>();

		void OnEnable()
		{
			m_ProcessOnImport = serializedObject.FindProperty( "m_RunOnImport" );
			m_FolderOnly = serializedObject.FindProperty( "m_FilterToFolder" );
			m_FiltersListProperty = serializedObject.FindProperty( "m_Filters" );
			m_Modules = serializedObject.FindProperty( "m_Modules" );
			m_SortIndex = serializedObject.FindProperty( "m_SortIndex" );
			
			m_ModuleTypes = ProfileCache.GetTypes<BaseModule>();
		}
		
		public override void OnInspectorGUI()
		{
			if( m_Profile == null )
				m_Profile = (AuditProfile) target;

			Rect viewRect = new Rect( 18, 0, EditorGUIUtility.currentViewWidth-23, EditorGUIUtility.singleLineHeight );
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
			
			size = m_Modules.arraySize;
			for( int i = 0; i < size; ++i )
			{
				if( m_ModuleFoldoutStates.Count-1 < i )
					m_ModuleFoldoutStates.Add( true );
				
				SerializedProperty moduleProperty = m_Modules.GetArrayElementAtIndex( i );
				BaseModule module = moduleProperty.objectReferenceValue as BaseModule;
				if( module == null )
					continue;

				if( i > 0 )
					layout.Space( 10 );

				Rect headerRect = layout.Get( 20 );
				m_ModuleFoldoutStates[i] = EditorGUI.Foldout( headerRect, m_ModuleFoldoutStates[i], module.name, true );
				
				Event current = Event.current;
				if( headerRect.Contains( current.mousePosition ) )
				{
					if( (current.type == EventType.MouseDown && current.button == 1) || current.type == EventType.ContextClick )
					{
						GenericMenu menu = new GenericMenu();
						menu.AddDisabledItem( new GUIContent( "Move Up" ) );
						menu.AddDisabledItem( new GUIContent( "Move Down" ) );
						menu.AddItem( new GUIContent( "Delete Module" ), false, RemoveModuleCallback, i );
						menu.ShowAsContext();
						current.Use();
					}
					else if( current.type == EventType.MouseDown && current.button == 0 )
					{
						m_ModuleFoldoutStates[i] = !m_ModuleFoldoutStates[i];
					}
				}

				if( m_ModuleFoldoutStates[i] )
				{
					layout.BeginArea( 5, 5 );

					module.DrawGUI( layout );

					GUI.depth = GUI.depth - 1;
					GUI.Box( layout.EndArea(), "" );
					GUI.depth = GUI.depth + 1;
				}
			}
			
			if( size > 0 )
				EditorGUI.LabelField( layout.Get(), "", UnityEngine.GUI.skin.horizontalSlider);
			
			layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 100);
			layoutRect.width = 100;
			
			if (EditorGUI.DropdownButton( layoutRect, new GUIContent("Add Module", "Add new Module to this Definition."), FocusType.Keyboard))
			{
				var menu = new GenericMenu();
				for (int i = 0; i < m_ModuleTypes.Count; i++)
				{
					var type = m_ModuleTypes[i];
					menu.AddItem(new GUIContent(type.Name, ""), false, OnAddModule, type);
				}

				menu.ShowAsContext();
			}
			
#if UNITY_2019_1_OR_NEWER
			// temporary solution to reserve the rect space for drawing in 2019.1+
			// remove after Unity Editor bug is resolved, id: 1169234
			GUILayoutUtility.GetRect( viewRect.width, layoutRect.y + layoutRect.height );
#endif
			serializedObject.ApplyModifiedProperties();
		}

		void RemoveModuleCallback( object context )
		{
			int index = (int) context;
			if( m_Profile.RemoveModule( index ) )
			{
				m_Modules.DeleteArrayElementAtIndex( index );
				m_Modules.MoveArrayElement( index, m_Modules.arraySize - 1 );
				m_Modules.arraySize = m_Modules.arraySize - 1;
				
				m_ModuleFoldoutStates.RemoveAt( index );
				
				EditorUtility.SetDirty( m_Profile );
				Repaint();
			}
		}
		
		void OnAddModule(object context)
		{
			Type t = context as Type;
			Assert.IsNotNull( t, "Null Module Type" );

			BaseModule addedModule = m_Profile.AddModule( t );
			if( addedModule != null )
			{
				// keep the serialised property in sync
				m_Modules.arraySize++;
				m_Modules.GetArrayElementAtIndex( m_Modules.arraySize-1 ).objectReferenceValue = addedModule;
				m_ModuleFoldoutStates.Add( true );
				
				EditorUtility.SetDirty( m_Profile );
				Repaint();
			}
		}
		
	}
}