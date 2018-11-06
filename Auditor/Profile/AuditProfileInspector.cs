using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AssetTools.GUIUtility;

namespace AssetTools
{
	[CustomEditor( typeof(AuditProfile) )]
	public class AuditProfileInspector : Editor
	{
		private AuditProfile current;

		public override void OnInspectorGUI()
		{
			if( current == null )
				current = (AuditProfile) target;

			Rect viewRect = EditorGUILayout.GetControlRect();
			ControlRect layout = new ControlRect( viewRect.x, viewRect.y, viewRect.width );

			layout.Space( 10 );
			// TODO use SerialisedObject and Properties???
			current.m_ImporterReference = EditorGUI.ObjectField( layout.Get(), "Template", current.m_ImporterReference, typeof(UnityEngine.Object), false );

			layout.Space( 10 );
			EditorGUI.LabelField( layout.Get(), "Search Filter's:" );
			
			if( current.m_Filters == null )
				current.m_Filters = new List<Filter>();

			Rect boxAreaRect = layout.Get( (16 * current.m_Filters.Count) + (3 * current.m_Filters.Count) + 6 );
			GUI.Box( boxAreaRect, GUIContent.none );

			ControlRect subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, 16 );
			subLayout.padding = 3;

			int removeAt = -1;
			// list the filters
			for( int i = 0; i < current.m_Filters.Count; ++i )
			{
				Rect segmentRect = subLayout.Get();
				segmentRect.x += 3;
				segmentRect.width -= 6;

				float segWidth = (segmentRect.width - segmentRect.height) / 3;

				segmentRect.width = segWidth - 3;
				current.m_Filters[i].m_Target = (Filter.ConditionTarget) EditorGUI.EnumPopup( segmentRect, current.m_Filters[i].m_Target );

				segmentRect.x += segWidth;
				current.m_Filters[i].m_Condition = (Filter.Condition) EditorGUI.EnumPopup( segmentRect, current.m_Filters[i].m_Condition );

				segmentRect.x += segWidth;
				current.m_Filters[i].m_Wildcard = EditorGUI.TextField( segmentRect, current.m_Filters[i].m_Wildcard );

				segmentRect.x += segWidth;
				segmentRect.width = segmentRect.height;
				if( GUI.Button( segmentRect, "-" ) )
				{
					removeAt = i;
				}
			}

			if( removeAt >= 0 )
				current.m_Filters.RemoveAt( removeAt );

			Rect layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 40);
			layoutRect.width = 40;
			if( GUI.Button( layoutRect, "Add" ) )
			{
				current.m_Filters.Add( new Filter() );
			}

			layout.Space( 20 );
			EditorGUI.LabelField( layout.Get(), "Constrain to Properties:" );

			current.GatherProperties();

			boxAreaRect = layout.Get( Mathf.Max( (current.PropertyCount * layout.layoutHeight) + 6, 20 ) );
			GUI.Box( boxAreaRect, GUIContent.none );

			subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, layout.layoutHeight );
			subLayout.padding = 0;
			removeAt = -1;
			for( int i = 0; i < current.PropertyCount; ++i )
			{
				EditorGUI.LabelField( subLayout.Get(), current.GetDisplayName( i ) );
			}

			if( removeAt >= 0 )
				current.RemoveProperty( removeAt );

			layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 100);
			layoutRect.width = 100;
			if( GUI.Button( layoutRect, "Edit Selection" ) )
			{
				string[] props = GetPropertyNames( new SerializedObject( current.GetAssetImporter() ) );
				GenericMenu menu = new GenericMenu();
				foreach( string prop in props )
				{
					bool isPropertySelected = current.m_ConstrainProperties.Contains( prop );
					string disName = current.GetDisplayName( prop );
					menu.AddItem( new GUIContent( disName ), isPropertySelected, TogglePropertySelected, disName );
				}

				menu.ShowAsContext();
			}
		}
		
		string[] GetPropertyNames(SerializedObject so)
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

		void TogglePropertySelected( object selectedObject )
		{
			string propertyName = selectedObject as string;
			if( current.m_ConstrainPropertiesDisplayNames.Contains( propertyName ) )
				current.RemoveProperty( propertyName, false );
			else
				current.AddProperty( propertyName, false );
		}
	}
}