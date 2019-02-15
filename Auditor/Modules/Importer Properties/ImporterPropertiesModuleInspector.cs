using System.Collections;
using System.Collections.Generic;
using AssetTools.GUIUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssetTools
{
	public static class ImporterPropertiesModuleInspector
	{
		public static void Draw( ImporterPropertiesModule propertiesModule, ControlRect layout )
		{
			using( var check = new EditorGUI.ChangeCheckScope() )
			{
				// TODO use SerialisedObject and Properties???
				propertiesModule.m_ImporterReference = EditorGUI.ObjectField( layout.Get(), "Template", propertiesModule.m_ImporterReference, typeof(UnityEngine.Object), false );
				
				if( check.changed )
					propertiesModule.GatherProperties();
			}
			layout.Space( 10 );
			EditorGUI.LabelField( layout.Get(), "Constrain to Properties:" );

			Rect boxAreaRect = layout.Get( Mathf.Max( (propertiesModule.PropertyCount * layout.layoutHeight) + 6, 20 ) );
			GUI.Box( boxAreaRect, GUIContent.none );

			ControlRect subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, layout.layoutHeight );
			subLayout.padding = 0;
			int removeAt = -1;
			for( int i = 0; i < propertiesModule.PropertyCount; ++i )
			{
				EditorGUI.LabelField( subLayout.Get(), propertiesModule.GetPropertyDisplayName( i ) );
			}

			if( removeAt >= 0 )
				propertiesModule.RemoveProperty( removeAt );

			Rect layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 100);
			layoutRect.width = 100;
			if( GUI.Button( layoutRect, "Edit Selection" ) )
			{
				string[] props = GetPropertyNames( new SerializedObject( propertiesModule.ReferenceAssetImporter ) );
				GenericMenu menu = new GenericMenu();
				foreach( string prop in props )
				{
					bool isPropertySelected = propertiesModule.m_ConstrainProperties.Contains( prop );
					string disName = propertiesModule.GetPropertyDisplayName( prop );
					menu.AddItem( new GUIContent( disName ), isPropertySelected, propertiesModule.TogglePropertyIncludedInConstraints, disName );
				}

				menu.ShowAsContext();
			}
		}
		
		static string[] GetPropertyNames(SerializedObject so)
		{
			SerializedProperty soIter = so.GetIterator();

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