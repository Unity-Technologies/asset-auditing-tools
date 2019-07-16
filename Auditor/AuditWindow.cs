using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetTools
{
	public class AuditWindow : EditorWindow
	{
		private TreeViewState m_AssetListState;
		private AssetsTreeView m_AssetList;

		private TreeViewState m_PropertyListState;
		private ModularDetailTreeView m_ModularTreeView;
		
		private List<ImportDefinitionProfile> profiles;
		private List<string> profileNames;
		
		private int selected;
		private bool editSelective;
		private int editSelectiveProp;

		private float m_SplitterPercent = 0.8f;
		private bool m_ResizingSplitter;

		private GUIStyle italicStyle;
		private GUIStyle boldStyle;
		[NonSerialized] private Texture2D m_RefreshTexture;

		[MenuItem( "Window/Asset Auditor", priority = 2055)]
		public static AuditWindow GetWindow()
		{
			AuditWindow window = GetWindow<AuditWindow>();
			window.minSize = new Vector2(350, 350);
			window.titleContent = new GUIContent( "Auditor" );
			return window;
		}

		private const float horizontalBuffer = 5f;
		
		private float contentX
		{
			get { return horizontalBuffer; }
		}
		private float contentWidth
		{
			get { return position.width - contentX - horizontalBuffer; }
		}


		private const float k_toolBarY = 3;
		private const float k_toolBarHeight = 17f;
		Rect profileDropDownRect
		{
			get { return new Rect( contentX, k_toolBarY, 150, k_toolBarHeight ); }
		}
		Rect profileSelectRect
		{
			get { return new Rect( contentX+profileDropDownRect.width+3, k_toolBarY, 60, k_toolBarHeight ); }
		}
		Rect refreshRect
		{
			get { return new Rect( (contentX+contentWidth) - 50, k_toolBarY, 50, k_toolBarHeight ); }
		}
		private Rect searchBarRect
		{
			get
			{
				float x = profileSelectRect.x + profileSelectRect.width + 3;
				return new Rect( x, k_toolBarY, (refreshRect.x-3)-x, k_toolBarHeight );
			}
		}

		private void RefreshData()
		{
			profiles = new List<ImportDefinitionProfile>();
			profileNames = new List<string>();
			GetAuditorProfiles( );
			
			if( m_AssetListState == null )
				m_AssetListState = new TreeViewState();

			IList<int> selection = null;
			if( m_AssetList != null )
			{
				selection = m_AssetList.GetSelection();
			}
			
			m_AssetList = new AssetsTreeView( m_AssetListState );
			if( profiles.Count > 0 && selected < profiles.Count )
				m_AssetList.m_Profile = profiles[selected];
			m_AssetList.Reload();
			
			if( m_PropertyListState == null )
				m_PropertyListState = new TreeViewState();
			
			m_ModularTreeView = new ModularDetailTreeView( m_PropertyListState );
			m_AssetList.m_ModularTreeView = m_ModularTreeView;
			m_ModularTreeView.Reload();

			if( selection != null )
			{
				m_AssetList.SetupSelection( selection );
			}
		}
		
		void GetAuditorProfiles(  )
		{
			string[] auditorProfileGUIDs = AssetDatabase.FindAssets( "t:ImportDefinitionProfile" );

			profiles.Clear();
			foreach( string asset in auditorProfileGUIDs )
			{
				string guidToAssetPath = AssetDatabase.GUIDToAssetPath( asset );
				ImportDefinitionProfile profile = AssetDatabase.LoadAssetAtPath<ImportDefinitionProfile>( guidToAssetPath );
				if( profile != null )
					profiles.Add( profile );
			}

			profileNames.Clear();
			foreach( ImportDefinitionProfile assetRule in profiles )
			{
				profileNames.Add( assetRule.name );
			}
		}

		void OnGUI()
		{
			// hacky way to see if its setup
			if( m_RefreshTexture == null )// || m_FixAction == null )
			{
				m_RefreshTexture = EditorGUIUtility.FindTexture( "Refresh" );
				italicStyle = new GUIStyle( GUI.skin.label );
				boldStyle = new GUIStyle( GUI.skin.label );
				italicStyle.fontStyle = FontStyle.BoldAndItalic;
				boldStyle.fontStyle = FontStyle.Bold;
				//m_FixAction = FixCallback;
			}

			if( m_AssetList == null || m_ModularTreeView == null )
			{
				RefreshData();
			}

			if( GUI.Button( refreshRect, m_RefreshTexture ) )
			{
				RefreshData();
			}
			
			HandleResize();
			DoProfileView();

			m_AssetList.searchString = SearchField.OnGUI( searchBarRect, m_AssetList.searchString );
			float listY = k_toolBarY + k_toolBarHeight + 3;
			m_AssetList.OnGUI( new Rect( 0, listY, position.width, (int)(position.height * m_SplitterPercent) - listY ) );
			
			listY = (int)(position.height * m_SplitterPercent) + 3;
			float h = position.height - listY;
			m_ModularTreeView.OnGUI( new Rect( 0, listY, position.width, h ) );
			
			if( m_ResizingSplitter )
				Repaint();
		}

		private void DoProfileView()
		{
			EditorGUI.BeginChangeCheck();
			string[] popupList = new string[profileNames.Count + 3];
			for( int i = 0; i < profileNames.Count; ++i )
			{
				popupList[i] = profileNames[i];
			}

			int preSelected = selected;
			selected = EditorGUI.Popup( profileDropDownRect, selected, popupList );
			
			if( EditorGUI.EndChangeCheck() )
			{
				if( selected == profileNames.Count )
				{
					Debug.Log( "Need to create a new profile" );
					// change the GUI to input 
				}
				else if( selected == profileNames.Count+1 )
				{
					Debug.Log( "Need to rename profile" );
					// rename popup
				}
				else if( selected == profileNames.Count+2 )
				{
					if( EditorUtility.DisplayDialog( "Delete Profile", "Are you sure you want to delete " + profileNames[preSelected], "Delete", "Cancel" ) )
					{
						if( AssetDatabase.DeleteAsset( AssetDatabase.GetAssetPath( profiles[preSelected] ) ) )
						{
							profiles.RemoveAt( preSelected );
							profileNames.RemoveAt( preSelected );
						}

						if( selected > 0 )
							selected = preSelected - 1;
					}
				}
				else if( selected < profileNames.Count )
				{
					RefreshData();
				}
			}
			
			if( GUI.Button( profileSelectRect, "select" ) && selected >= 0 && selected < profiles.Count && profiles[selected] != null )
			{
				Selection.activeObject = profiles[selected];
				EditorGUIUtility.PingObject( profiles[selected] );
			}
		}
		
		private void HandleResize()
		{
			Rect splitterRect = new Rect(0, (int)(position.height * m_SplitterPercent), position.width, 3);

			EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);
			if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
				m_ResizingSplitter = true;

			if (m_ResizingSplitter)
			{
				m_SplitterPercent = Mathf.Clamp(Event.current.mousePosition.y / position.height, 0.1f, 0.9f);
				splitterRect.y = (int)(position.height * m_SplitterPercent);
			}

			if (Event.current.type == EventType.MouseUp)
				m_ResizingSplitter = false;
		}
	}
	
	
	internal static class SearchField
	{
		private static class Styles
		{
			public static readonly GUIStyle searchField = "SearchTextField";
			public static readonly GUIStyle searchFieldCancelButton = "SearchCancelButton";
			public static readonly GUIStyle searchFieldCancelButtonEmpty = "SearchCancelButtonEmpty";
		}

		public static string OnGUI( Rect position, string text )
		{
			// Search field 
			Rect textRect = position;
			textRect.width -= 15;
			text = EditorGUI.TextField( textRect, GUIContent.none, text, Styles.searchField );

			// Cancel button
			Rect buttonRect = position;
			buttonRect.x += position.width - 15;
			buttonRect.width = 15;
			if( GUI.Button( buttonRect, GUIContent.none,
				    text != "" ? Styles.searchFieldCancelButton : Styles.searchFieldCancelButtonEmpty ) && text != "" )
			{
				text = "";
				UnityEngine.GUIUtility.keyboardControl = 0;
			}

			return text;
		}
	}
}