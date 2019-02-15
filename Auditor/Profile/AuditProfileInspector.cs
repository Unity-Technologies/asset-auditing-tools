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

		public override void OnInspectorGUI()
		{
			if( m_Profile == null )
				m_Profile = (AuditProfile) target;

			Rect viewRect = EditorGUILayout.GetControlRect();
			ControlRect layout = new ControlRect( viewRect.x, viewRect.y, viewRect.width );
			
			layout.Space( 10 );

			m_Profile.m_RunOnImport = EditorGUI.Toggle( layout.Get(), "Process On Import", m_Profile.m_RunOnImport );
			
			layout.Space( 10 );

			
			EditorGUI.LabelField( layout.Get(), "Search Filter's:" );
			
			if( m_Profile.m_Filters == null )
				m_Profile.m_Filters = new List<Filter>();

			Rect boxAreaRect = layout.Get( (16 * m_Profile.m_Filters.Count) + (3 * m_Profile.m_Filters.Count) + 6 );
			GUI.Box( boxAreaRect, GUIContent.none );

			ControlRect subLayout = new ControlRect( boxAreaRect.x + 3, boxAreaRect.y + 3, boxAreaRect.width - 6, 16 );
			subLayout.padding = 3;

			int removeAt = -1;
			// list the filters
			for( int i = 0; i < m_Profile.m_Filters.Count; ++i )
			{
				Rect segmentRect = subLayout.Get();
				segmentRect.x += 3;
				segmentRect.width -= 6;

				float segWidth = (segmentRect.width - segmentRect.height) / 3;

				segmentRect.width = segWidth - 3;
				m_Profile.m_Filters[i].m_Target = (Filter.ConditionTarget) EditorGUI.EnumPopup( segmentRect, m_Profile.m_Filters[i].m_Target );

				segmentRect.x += segWidth;
				m_Profile.m_Filters[i].m_Condition = (Filter.Condition) EditorGUI.EnumPopup( segmentRect, m_Profile.m_Filters[i].m_Condition );

				segmentRect.x += segWidth;
				m_Profile.m_Filters[i].m_Wildcard = EditorGUI.TextField( segmentRect, m_Profile.m_Filters[i].m_Wildcard );

				segmentRect.x += segWidth;
				segmentRect.width = segmentRect.height;
				if( GUI.Button( segmentRect, "-" ) )
				{
					removeAt = i;
				}
			}

			if( removeAt >= 0 )
				m_Profile.m_Filters.RemoveAt( removeAt );

			Rect layoutRect = layout.Get();
			layoutRect.x = layoutRect.x + (layoutRect.width - 40);
			layoutRect.width = 40;
			if( GUI.Button( layoutRect, "Add" ) )
			{
				m_Profile.m_Filters.Add( new Filter() );
			}
			
			layout.Space( 20 );
			
			
			// TODO do the rest as modules that can be added
			ImporterPropertiesModuleInspector.Draw( m_Profile.m_ImporterModule, layout );
		}
		
		
	}
}