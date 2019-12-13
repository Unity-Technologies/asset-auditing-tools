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

		private SearchField m_SearchFieldInternal;

		private TreeViewState m_PropertyListState;
		private ModularDetailTreeView m_ModularTreeView;
		
		private List<ImportDefinitionProfile> profiles;
		private List<string> profileNames;
		
		private int selected;
		private bool editSelective;
		private int editSelectiveProp;

		private float m_SplitterPercent = 0.7f;
		private bool m_ResizingSplitter;

		private const float horizontalBuffer = 5f;
		private const float k_toolBarY = 0;
		private const float k_toolBarHeight = 17f;
		
		[SerializeField]
		MultiColumnHeaderState m_ConformTreeHeaderState;
		
		private float ContentX
		{
			get { return horizontalBuffer; }
		}
		private float ContentWidth
		{
			get { return position.width - ContentX - horizontalBuffer; }
		}

		private Rect ProfileDropDownRect
		{
			get { return new Rect( ContentX, k_toolBarY, 150, k_toolBarHeight ); }
		}
		private Rect ProfileSelectRect
		{
			get { return new Rect( ContentX+ProfileDropDownRect.width+3, k_toolBarY, 60, k_toolBarHeight ); }
		}
		private Rect RefreshRect
		{
			get { return new Rect( (ContentX+ContentWidth) - 50, k_toolBarY, 50, k_toolBarHeight ); }
		}
		private Rect SearchBarRect
		{
			get
			{
				float x = ProfileSelectRect.x + ProfileSelectRect.width + 3+5;
				return new Rect( x, k_toolBarY+1, (RefreshRect.x-3-5)-x, k_toolBarHeight-1 );
			}
		}

		private void OnEnable()
		{
			m_SearchFieldInternal = new SearchField();
		}

		[MenuItem( "Window/Asset Auditor", priority = 2055)]
		public static AuditWindow GetWindow()
		{
			AuditWindow window = GetWindow<AuditWindow>();
			window.minSize = new Vector2(350, 350);
			window.titleContent = new GUIContent( "Auditor" );
			return window;
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
				selection = m_AssetList.GetSelection();
			
			m_AssetList = new AssetsTreeView( m_AssetListState );
			
			m_AssetList.SearchStyle = (HierarchyTreeView.SearchType)EditorPrefs.GetInt( "AuditWindow.AssetsTreeView.SearchStyle", 1 );
			m_AssetList.SearchCaseSensitive = EditorPrefs.GetBool( "AuditWindow.AssetsTreeView.SearchCaseSensitivity", false );
			
			if( profiles.Count > 0 && selected < profiles.Count )
				m_AssetList.m_Profile = profiles[selected];
			m_AssetList.Reload();
			
			if( m_PropertyListState == null )
				m_PropertyListState = new TreeViewState();
			if( m_ModularTreeView == null )
			{
				MultiColumnHeaderState headerState = CreateDefaultMultiColumnHeaderState();
				if( MultiColumnHeaderState.CanOverwriteSerializedFields( m_ConformTreeHeaderState, headerState ) )
					MultiColumnHeaderState.OverwriteSerializedFields( m_ConformTreeHeaderState, headerState );
				m_ConformTreeHeaderState = headerState;
				m_ModularTreeView = new ModularDetailTreeView( m_PropertyListState, m_ConformTreeHeaderState );
			}
			m_AssetList.m_ModularTreeView = m_ModularTreeView;
			m_ModularTreeView.Reload();

			if( selection != null )
				m_AssetList.SetupSelection( selection );
		}
		
		internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
		{
			var retVal = new MultiColumnHeaderState.Column[4];
			retVal[0] = new MultiColumnHeaderState.Column();
			retVal[0].headerContent = new GUIContent( EditorGUIUtility.FindTexture("FilterSelectedOnly"), "Does this element conform to the expected?");
			retVal[0].minWidth = 20;
			retVal[0].width = 20;
			retVal[0].maxWidth = 20;
			retVal[0].headerTextAlignment = TextAlignment.Left;
			retVal[0].canSort = true;
			retVal[0].autoResize = true;

			retVal[1] = new MultiColumnHeaderState.Column();
			retVal[1].headerContent = new GUIContent("Property Name", "Name of the Property or Parent to Property");
			retVal[1].minWidth = 150;
			retVal[1].width = 300;
			retVal[1].maxWidth = 1000;
			retVal[1].headerTextAlignment = TextAlignment.Left;
			retVal[1].canSort = true;
			retVal[1].autoResize = true;

			retVal[2] = new MultiColumnHeaderState.Column();
			retVal[2].headerContent = new GUIContent("Expected Value", "The value expected");
			retVal[2].minWidth = 100;
			retVal[2].width = 150;
			retVal[2].maxWidth = 400;
			retVal[2].headerTextAlignment = TextAlignment.Left;
			retVal[2].canSort = false;
			retVal[2].autoResize = true;

			retVal[3] = new MultiColumnHeaderState.Column();
			retVal[3].headerContent = new GUIContent("Actual Value", "The actual value reported");
			retVal[3].minWidth = 100;
			retVal[3].width = 150;
			retVal[3].maxWidth = 400;
			retVal[3].headerTextAlignment = TextAlignment.Left;
			retVal[3].canSort = false;
			retVal[3].autoResize = true;

			return new MultiColumnHeaderState(retVal);
		}
		
		void GetAuditorProfiles()
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
				profileNames.Add( assetRule.name );
		}

		void OnGUI()
		{
			HandleResize();

			if( m_AssetList == null || m_ModularTreeView == null )
				RefreshData();
			
			ToolbarGUI( new Rect( 0, 0, position.width, k_toolBarHeight) );
			
			float viewY = k_toolBarY + k_toolBarHeight;
			m_AssetList.OnGUI( new Rect( 0, viewY, position.width, (int)(position.height * m_SplitterPercent) - viewY ) );
			
			viewY = (int)(position.height * m_SplitterPercent);
			m_ModularTreeView.OnGUI( new Rect( 0, viewY, position.width, position.height - viewY ) );
			
			if( m_ResizingSplitter )
				Repaint();
		}

		private void ToolbarGUI( Rect toolbarPos )
		{
			GUI.Box( toolbarPos, GUIContent.none, EditorStyles.toolbar );
			
			string[] popupList = new string[profileNames.Count + 3];
			for( int i = 0; i < profileNames.Count; ++i )
				popupList[i] = profileNames[i];

			int preSelected = selected;
			selected = EditorGUI.Popup( ProfileDropDownRect, selected, popupList, EditorStyles.toolbarPopup );
			if( selected != preSelected && selected < profileNames.Count )
				RefreshData();
			
			if( GUI.Button( ProfileSelectRect, "Select", Styles.button ) && selected >= 0 && selected < profiles.Count && profiles[selected] != null )
			{
				Selection.activeObject = profiles[selected];
				EditorGUIUtility.PingObject( profiles[selected] );
			}
			
			if( m_SearchFieldInternal == null )
				m_SearchFieldInternal = new SearchField();
			OnSearchGUI( SearchBarRect );
			
			if( GUI.Button( RefreshRect, "Refresh", Styles.button ) )
				RefreshData();
		}
		
		private void OnSearchGUI( Rect barPosition )
		{
			string text = m_AssetList.CustomSearch;

			Rect popupPosition = new Rect( barPosition.x, barPosition.y, 20, 20 );
			if (Event.current.type == EventType.MouseDown && popupPosition.Contains(Event.current.mousePosition))
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Hierarchical Search"), m_AssetList.SearchStyle == HierarchyTreeView.SearchType.Hierarchy, () => SetSearchMode( HierarchyTreeView.SearchType.Hierarchy ));
				menu.AddItem(new GUIContent("Hierarchical Search (Assets Only)"), m_AssetList.SearchStyle == HierarchyTreeView.SearchType.HierarchyLeafOnly, () => SetSearchMode( HierarchyTreeView.SearchType.HierarchyLeafOnly ));
				menu.AddItem(new GUIContent("Flat Search"), m_AssetList.SearchStyle == HierarchyTreeView.SearchType.Flat, () => SetSearchMode( HierarchyTreeView.SearchType.Flat ));
				menu.AddItem(new GUIContent("Flat Search (Assets Only)"), m_AssetList.SearchStyle == HierarchyTreeView.SearchType.FlatLeafOnly, () => SetSearchMode( HierarchyTreeView.SearchType.FlatLeafOnly ));
				menu.AddSeparator( "" );
				menu.AddItem( new GUIContent( "Case Sensitive" ), m_AssetList.SearchCaseSensitive, () =>
				{
					m_AssetList.SearchCaseSensitive = !m_AssetList.SearchCaseSensitive;
					EditorPrefs.SetBool( "AuditWindow.AssetsTreeView.SearchCaseSensitivity", m_AssetList.SearchCaseSensitive );
					m_AssetList.Reload();
				} );
				menu.DropDown(popupPosition);
			}
			else
			{
				var searchString = m_SearchFieldInternal.OnGUI(barPosition, text, Styles.searchField, Styles.searchFieldCancelButton, Styles.searchFieldCancelButtonEmpty);
				
				if (text != searchString)
				{
					text = searchString;
					
					m_AssetList.CustomSearch = text;
					m_AssetList.Reload();
					
					if( String.IsNullOrEmpty( text ) )
						m_AssetList.ExpandForSelection();
				}
			}
		}

		private void SetSearchMode( HierarchyTreeView.SearchType searchType )
		{
			if( m_AssetList.SearchStyle == searchType )
				return;

			m_AssetList.SearchStyle = searchType;
			EditorPrefs.SetInt( "AuditWindow.AssetsTreeView.SearchStyle", (int)searchType );
			m_AssetList.Reload();
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
		private static class Styles
		{
			public static readonly GUIStyle searchField = "ToolbarSeachTextFieldPopup";
			public static readonly GUIStyle searchFieldCancelButton = "ToolbarSeachCancelButton";
			public static readonly GUIStyle searchFieldCancelButtonEmpty = "ToolbarSeachCancelButtonEmpty";
			public static readonly GUIStyle button = "ToolbarButton";
		}
		
	}
}