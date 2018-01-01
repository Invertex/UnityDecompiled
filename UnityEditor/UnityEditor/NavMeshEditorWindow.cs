using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.AI;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;

namespace UnityEditor
{
	[EditorWindowTitle(title = "Navigation", icon = "Navigation")]
	internal class NavMeshEditorWindow : EditorWindow, IHasCustomMenu
	{
		private enum Mode
		{
			AgentSettings,
			AreaSettings,
			SceneBakeSettings,
			ObjectSettings
		}

		private class Styles
		{
			public readonly GUIContent m_AgentRadiusContent = EditorGUIUtility.TrTextContent("Agent Radius", "How close to the walls navigation mesh exist.", null);

			public readonly GUIContent m_AgentHeightContent = EditorGUIUtility.TrTextContent("Agent Height", "How much vertical clearance space must exist.", null);

			public readonly GUIContent m_AgentSlopeContent = EditorGUIUtility.TrTextContent("Max Slope", "Maximum slope the agent can walk up.", null);

			public readonly GUIContent m_AgentDropContent = EditorGUIUtility.TrTextContent("Drop Height", "Maximum agent drop height.", null);

			public readonly GUIContent m_AgentClimbContent = EditorGUIUtility.TrTextContent("Step Height", "The height of discontinuities in the level the agent can climb over (i.e. steps and stairs).", null);

			public readonly GUIContent m_AgentJumpContent = EditorGUIUtility.TrTextContent("Jump Distance", "Maximum agent jump distance.", null);

			public readonly GUIContent m_AgentPlacementContent = EditorGUIUtility.TrTextContent("Height Mesh", "Generate an accurate height mesh for precise agent placement (slower).", null);

			public readonly GUIContent m_MinRegionAreaContent = EditorGUIUtility.TrTextContent("Min Region Area", "Minimum area that a navmesh region can be.", null);

			public readonly GUIContent m_ManualCellSizeContent = EditorGUIUtility.TrTextContent("Manual Voxel Size", "Enable to set voxel size manually.", null);

			public readonly GUIContent m_CellSizeContent = EditorGUIUtility.TrTextContent("Voxel Size", "Specifies at the voxelization resolution at which the NavMesh is build.", null);

			public readonly GUIContent m_LearnAboutComponent = EditorGUIUtility.TrTextContent("Learn instead about the component workflow.", "Components available for building and using navmesh data for different agent types.", null);

			public readonly GUIContent m_AgentSizeHeader = EditorGUIUtility.TrTextContent("Baked Agent Size", null, null);

			public readonly GUIContent m_OffmeshHeader = EditorGUIUtility.TrTextContent("Generated Off Mesh Links", null, null);

			public readonly GUIContent m_AdvancedHeader = EditorGUIUtility.TrTextContent("Advanced", null, null);

			public readonly GUIContent m_AgentTypesHeader = EditorGUIUtility.TrTextContent("Agent Types", null, null);

			public readonly GUIContent m_NameLabel = EditorGUIUtility.TrTextContent("Name", null, null);

			public readonly GUIContent m_CostLabel = EditorGUIUtility.TrTextContent("Cost", null, null);

			public readonly GUIContent[] m_ModeToggles = new GUIContent[]
			{
				EditorGUIUtility.TrTextContent("Agents", "Navmesh agent settings.", null),
				EditorGUIUtility.TrTextContent("Areas", "Navmesh area settings.", null),
				EditorGUIUtility.TrTextContent("Bake", "Navmesh bake settings.", null),
				EditorGUIUtility.TrTextContent("Object", "Bake settings for the currently selected object.", null)
			};
		}

		private static NavMeshEditorWindow s_NavMeshEditorWindow;

		private SerializedObject m_SettingsObject;

		private SerializedProperty m_AgentRadius;

		private SerializedProperty m_AgentHeight;

		private SerializedProperty m_AgentSlope;

		private SerializedProperty m_AgentClimb;

		private SerializedProperty m_LedgeDropHeight;

		private SerializedProperty m_MaxJumpAcrossDistance;

		private SerializedProperty m_MinRegionArea;

		private SerializedProperty m_ManualCellSize;

		private SerializedProperty m_CellSize;

		private SerializedProperty m_AccuratePlacement;

		private SerializedObject m_NavMeshProjectSettingsObject;

		private SerializedProperty m_Areas;

		private SerializedProperty m_Agents;

		private SerializedProperty m_SettingNames;

		private const string kRootPath = "m_BuildSettings.";

		private static NavMeshEditorWindow.Styles s_Styles;

		private Vector2 m_ScrollPos = Vector2.zero;

		private int m_SelectedNavMeshAgentCount = 0;

		private int m_SelectedNavMeshObstacleCount = 0;

		private bool m_Advanced;

		private bool m_HasPendingAgentDebugInfo = false;

		private bool m_HasRepaintedForPendingAgentDebugInfo = false;

		private ReorderableList m_AreasList = null;

		private ReorderableList m_AgentsList = null;

		private NavMeshEditorWindow.Mode m_Mode = NavMeshEditorWindow.Mode.ObjectSettings;

		private bool m_BecameVisibleCalled = false;

		[CompilerGenerated]
		private static SceneViewOverlay.WindowFunction <>f__mg$cache0;

		[CompilerGenerated]
		private static SceneViewOverlay.WindowFunction <>f__mg$cache1;

		[CompilerGenerated]
		private static SceneViewOverlay.WindowFunction <>f__mg$cache2;

		[MenuItem("Window/Navigation", false, 2100)]
		public static void SetupWindow()
		{
			NavMeshEditorWindow window = EditorWindow.GetWindow<NavMeshEditorWindow>(new Type[]
			{
				typeof(InspectorWindow)
			});
			window.minSize = new Vector2(300f, 360f);
		}

		public static void OpenAreaSettings()
		{
			NavMeshEditorWindow.SetupWindow();
			if (!(NavMeshEditorWindow.s_NavMeshEditorWindow == null))
			{
				NavMeshEditorWindow.s_NavMeshEditorWindow.m_Mode = NavMeshEditorWindow.Mode.AreaSettings;
				NavMeshEditorWindow.s_NavMeshEditorWindow.InitProjectSettings();
				NavMeshEditorWindow.s_NavMeshEditorWindow.InitAgents();
			}
		}

		public static void OpenAgentSettings(int agentTypeID)
		{
			NavMeshEditorWindow.SetupWindow();
			if (!(NavMeshEditorWindow.s_NavMeshEditorWindow == null))
			{
				NavMeshEditorWindow.s_NavMeshEditorWindow.m_Mode = NavMeshEditorWindow.Mode.AgentSettings;
				NavMeshEditorWindow.s_NavMeshEditorWindow.InitProjectSettings();
				NavMeshEditorWindow.s_NavMeshEditorWindow.InitAgents();
				NavMeshEditorWindow.s_NavMeshEditorWindow.m_AgentsList.index = -1;
				for (int i = 0; i < NavMeshEditorWindow.s_NavMeshEditorWindow.m_Agents.arraySize; i++)
				{
					SerializedProperty arrayElementAtIndex = NavMeshEditorWindow.s_NavMeshEditorWindow.m_Agents.GetArrayElementAtIndex(i);
					SerializedProperty serializedProperty = arrayElementAtIndex.FindPropertyRelative("agentTypeID");
					if (serializedProperty.intValue == agentTypeID)
					{
						NavMeshEditorWindow.s_NavMeshEditorWindow.m_AgentsList.index = i;
						break;
					}
				}
			}
		}

		public void OnEnable()
		{
			base.titleContent = base.GetLocalizedTitleContent();
			NavMeshEditorWindow.s_NavMeshEditorWindow = this;
			EditorApplication.searchChanged = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.searchChanged, new EditorApplication.CallbackFunction(base.Repaint));
			SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.Combine(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(this.OnSceneViewGUI));
			this.UpdateSelectedAgentAndObstacleState();
			base.Repaint();
		}

		private void InitProjectSettings()
		{
			if (this.m_NavMeshProjectSettingsObject == null)
			{
				UnityEngine.Object serializedAssetInterfaceSingleton = Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings");
				this.m_NavMeshProjectSettingsObject = new SerializedObject(serializedAssetInterfaceSingleton);
			}
		}

		private void InitSceneBakeSettings()
		{
			this.m_SettingsObject = new SerializedObject(UnityEditor.AI.NavMeshBuilder.navMeshSettingsObject);
			this.m_AgentRadius = this.m_SettingsObject.FindProperty("m_BuildSettings.agentRadius");
			this.m_AgentHeight = this.m_SettingsObject.FindProperty("m_BuildSettings.agentHeight");
			this.m_AgentSlope = this.m_SettingsObject.FindProperty("m_BuildSettings.agentSlope");
			this.m_LedgeDropHeight = this.m_SettingsObject.FindProperty("m_BuildSettings.ledgeDropHeight");
			this.m_AgentClimb = this.m_SettingsObject.FindProperty("m_BuildSettings.agentClimb");
			this.m_MaxJumpAcrossDistance = this.m_SettingsObject.FindProperty("m_BuildSettings.maxJumpAcrossDistance");
			this.m_MinRegionArea = this.m_SettingsObject.FindProperty("m_BuildSettings.minRegionArea");
			this.m_ManualCellSize = this.m_SettingsObject.FindProperty("m_BuildSettings.manualCellSize");
			this.m_CellSize = this.m_SettingsObject.FindProperty("m_BuildSettings.cellSize");
			this.m_AccuratePlacement = this.m_SettingsObject.FindProperty("m_BuildSettings.accuratePlacement");
		}

		private void InitAreas()
		{
			if (this.m_Areas == null)
			{
				this.m_Areas = this.m_NavMeshProjectSettingsObject.FindProperty("areas");
			}
			if (this.m_AreasList == null)
			{
				this.m_AreasList = new ReorderableList(this.m_NavMeshProjectSettingsObject, this.m_Areas, false, false, false, false);
				this.m_AreasList.drawElementCallback = new ReorderableList.ElementCallbackDelegate(this.DrawAreaListElement);
				this.m_AreasList.drawHeaderCallback = new ReorderableList.HeaderCallbackDelegate(this.DrawAreaListHeader);
				this.m_AreasList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		private void InitAgents()
		{
			if (this.m_Agents == null)
			{
				this.m_Agents = this.m_NavMeshProjectSettingsObject.FindProperty("m_Settings");
				this.m_SettingNames = this.m_NavMeshProjectSettingsObject.FindProperty("m_SettingNames");
			}
			if (this.m_AgentsList == null)
			{
				this.m_AgentsList = new ReorderableList(this.m_NavMeshProjectSettingsObject, this.m_Agents, false, false, true, true);
				this.m_AgentsList.drawElementCallback = new ReorderableList.ElementCallbackDelegate(this.DrawAgentListElement);
				this.m_AgentsList.drawHeaderCallback = new ReorderableList.HeaderCallbackDelegate(this.DrawAgentListHeader);
				this.m_AgentsList.onAddCallback = new ReorderableList.AddCallbackDelegate(this.AddAgent);
				this.m_AgentsList.onRemoveCallback = new ReorderableList.RemoveCallbackDelegate(this.RemoveAgent);
				this.m_AgentsList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		private int Bit(int a, int b)
		{
			return (a & 1 << b) >> b;
		}

		private Color GetAreaColor(int i)
		{
			Color result;
			if (i == 0)
			{
				result = new Color(0f, 0.75f, 1f, 0.5f);
			}
			else
			{
				int num = (this.Bit(i, 4) + this.Bit(i, 1) * 2 + 1) * 63;
				int num2 = (this.Bit(i, 3) + this.Bit(i, 2) * 2 + 1) * 63;
				int num3 = (this.Bit(i, 5) + this.Bit(i, 0) * 2 + 1) * 63;
				result = new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f, 0.5f);
			}
			return result;
		}

		public void OnDisable()
		{
			NavMeshEditorWindow.s_NavMeshEditorWindow = null;
			EditorApplication.searchChanged = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.searchChanged, new EditorApplication.CallbackFunction(base.Repaint));
			SceneView.onSceneGUIDelegate = (SceneView.OnSceneFunc)Delegate.Remove(SceneView.onSceneGUIDelegate, new SceneView.OnSceneFunc(this.OnSceneViewGUI));
		}

		private void UpdateSelectedAgentAndObstacleState()
		{
			UnityEngine.Object[] filtered = Selection.GetFiltered(typeof(NavMeshAgent), (SelectionMode)12);
			UnityEngine.Object[] filtered2 = Selection.GetFiltered(typeof(NavMeshObstacle), (SelectionMode)12);
			this.m_SelectedNavMeshAgentCount = filtered.Length;
			this.m_SelectedNavMeshObstacleCount = filtered2.Length;
		}

		private void OnSelectionChange()
		{
			this.UpdateSelectedAgentAndObstacleState();
			this.m_ScrollPos = Vector2.zero;
			if (this.m_Mode == NavMeshEditorWindow.Mode.ObjectSettings)
			{
				base.Repaint();
			}
		}

		private void ModeToggle()
		{
			if (NavMeshEditorWindow.s_Styles == null)
			{
				NavMeshEditorWindow.s_Styles = new NavMeshEditorWindow.Styles();
			}
			EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			this.m_Mode = (NavMeshEditorWindow.Mode)GUILayout.Toolbar((int)this.m_Mode, NavMeshEditorWindow.s_Styles.m_ModeToggles, "LargeButton", GUI.ToolbarButtonSize.FitToContents, new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		private void GetAreaListRects(Rect rect, out Rect stripeRect, out Rect labelRect, out Rect nameRect, out Rect costRect)
		{
			float num = EditorGUIUtility.singleLineHeight * 0.8f;
			float num2 = EditorGUIUtility.singleLineHeight * 5f;
			float num3 = EditorGUIUtility.singleLineHeight * 4f;
			float num4 = rect.width - num - num2 - num3;
			float num5 = rect.x;
			stripeRect = new Rect(num5, rect.y, num - 4f, rect.height);
			num5 += num;
			labelRect = new Rect(num5, rect.y, num2 - 4f, rect.height);
			num5 += num2;
			nameRect = new Rect(num5, rect.y, num4 - 4f, rect.height);
			num5 += num4;
			costRect = new Rect(num5, rect.y, num3, rect.height);
		}

		private void DrawAreaListHeader(Rect rect)
		{
			Rect rect2;
			Rect rect3;
			Rect position;
			Rect position2;
			this.GetAreaListRects(rect, out rect2, out rect3, out position, out position2);
			GUI.Label(position, NavMeshEditorWindow.s_Styles.m_NameLabel);
			GUI.Label(position2, NavMeshEditorWindow.s_Styles.m_CostLabel);
		}

		private void DrawAreaListElement(Rect rect, int index, bool selected, bool focused)
		{
			SerializedProperty arrayElementAtIndex = this.m_Areas.GetArrayElementAtIndex(index);
			if (arrayElementAtIndex != null)
			{
				SerializedProperty serializedProperty = arrayElementAtIndex.FindPropertyRelative("name");
				SerializedProperty serializedProperty2 = arrayElementAtIndex.FindPropertyRelative("cost");
				if (serializedProperty != null && serializedProperty2 != null)
				{
					rect.height -= 2f;
					bool flag;
					bool flag2;
					bool flag3;
					switch (index)
					{
					case 0:
						flag = true;
						flag2 = false;
						flag3 = true;
						break;
					case 1:
						flag = true;
						flag2 = false;
						flag3 = false;
						break;
					case 2:
						flag = true;
						flag2 = false;
						flag3 = true;
						break;
					default:
						flag = false;
						flag2 = true;
						flag3 = true;
						break;
					}
					Rect rect2;
					Rect position;
					Rect position2;
					Rect position3;
					this.GetAreaListRects(rect, out rect2, out position, out position2, out position3);
					bool enabled = GUI.enabled;
					Color areaColor = this.GetAreaColor(index);
					Color color = new Color(areaColor.r * 0.1f, areaColor.g * 0.1f, areaColor.b * 0.1f, 0.6f);
					EditorGUI.DrawRect(rect2, areaColor);
					EditorGUI.DrawRect(new Rect(rect2.x, rect2.y, 1f, rect2.height), color);
					EditorGUI.DrawRect(new Rect(rect2.x + rect2.width - 1f, rect2.y, 1f, rect2.height), color);
					EditorGUI.DrawRect(new Rect(rect2.x + 1f, rect2.y, rect2.width - 2f, 1f), color);
					EditorGUI.DrawRect(new Rect(rect2.x + 1f, rect2.y + rect2.height - 1f, rect2.width - 2f, 1f), color);
					if (flag)
					{
						GUI.Label(position, EditorGUIUtility.TempContent("Built-in " + index));
					}
					else
					{
						GUI.Label(position, EditorGUIUtility.TempContent("User " + index));
					}
					int indentLevel = EditorGUI.indentLevel;
					EditorGUI.indentLevel = 0;
					GUI.enabled = (enabled && flag2);
					EditorGUI.PropertyField(position2, serializedProperty, GUIContent.none);
					GUI.enabled = (enabled && flag3);
					EditorGUI.PropertyField(position3, serializedProperty2, GUIContent.none);
					GUI.enabled = enabled;
					EditorGUI.indentLevel = indentLevel;
				}
			}
		}

		private void AddAgent(ReorderableList list)
		{
			NavMesh.CreateSettings();
			list.index = NavMesh.GetSettingsCount() - 1;
		}

		private void RemoveAgent(ReorderableList list)
		{
			SerializedProperty arrayElementAtIndex = this.m_Agents.GetArrayElementAtIndex(list.index);
			if (arrayElementAtIndex != null)
			{
				SerializedProperty serializedProperty = arrayElementAtIndex.FindPropertyRelative("agentTypeID");
				if (serializedProperty != null)
				{
					if (serializedProperty.intValue != 0)
					{
						this.m_SettingNames.DeleteArrayElementAtIndex(list.index);
						ReorderableList.defaultBehaviours.DoRemoveButton(list);
					}
				}
			}
		}

		private void DrawAgentListHeader(Rect rect)
		{
			GUI.Label(rect, NavMeshEditorWindow.s_Styles.m_AgentTypesHeader);
		}

		private void DrawAgentListElement(Rect rect, int index, bool selected, bool focused)
		{
			SerializedProperty arrayElementAtIndex = this.m_Agents.GetArrayElementAtIndex(index);
			if (arrayElementAtIndex != null)
			{
				SerializedProperty serializedProperty = arrayElementAtIndex.FindPropertyRelative("agentTypeID");
				if (serializedProperty != null)
				{
					rect.height -= 2f;
					bool disabled = serializedProperty.intValue == 0;
					using (new EditorGUI.DisabledScope(disabled))
					{
						string settingsNameFromID = NavMesh.GetSettingsNameFromID(serializedProperty.intValue);
						GUI.Label(rect, EditorGUIUtility.TempContent(settingsNameFromID));
					}
				}
			}
		}

		public void OnGUI()
		{
			EditorGUILayout.Space();
			this.ModeToggle();
			EditorGUILayout.Space();
			this.InitProjectSettings();
			this.m_ScrollPos = EditorGUILayout.BeginScrollView(this.m_ScrollPos, new GUILayoutOption[0]);
			switch (this.m_Mode)
			{
			case NavMeshEditorWindow.Mode.AgentSettings:
				this.AgentSettings();
				break;
			case NavMeshEditorWindow.Mode.AreaSettings:
				this.AreaSettings();
				break;
			case NavMeshEditorWindow.Mode.SceneBakeSettings:
				this.SceneBakeSettings();
				break;
			case NavMeshEditorWindow.Mode.ObjectSettings:
				NavMeshEditorWindow.ObjectSettings();
				break;
			}
			EditorGUILayout.EndScrollView();
		}

		public void OnBecameVisible()
		{
			if (!this.m_BecameVisibleCalled)
			{
				bool flag = NavMeshVisualizationSettings.showNavigation == 0;
				NavMeshVisualizationSettings.showNavigation++;
				if (flag)
				{
					NavMeshEditorWindow.RepaintSceneAndGameViews();
				}
				this.m_BecameVisibleCalled = true;
			}
		}

		public void OnBecameInvisible()
		{
			if (this.m_BecameVisibleCalled)
			{
				NavMeshVisualizationSettings.showNavigation--;
				NavMeshEditorWindow.RepaintSceneAndGameViews();
				this.m_BecameVisibleCalled = false;
			}
		}

		private static void RepaintSceneAndGameViews()
		{
			SceneView.RepaintAll();
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameView));
			for (int i = 0; i < array.Length; i++)
			{
				GameView gameView = (GameView)array[i];
				gameView.Repaint();
			}
		}

		public void OnSceneViewGUI(SceneView sceneView)
		{
			if (NavMeshVisualizationSettings.showNavigation != 0)
			{
				GUIContent arg_3F_0 = EditorGUIUtility.TrTextContent("Navmesh Display", null, null);
				if (NavMeshEditorWindow.<>f__mg$cache0 == null)
				{
					NavMeshEditorWindow.<>f__mg$cache0 = new SceneViewOverlay.WindowFunction(NavMeshEditorWindow.DisplayControls);
				}
				SceneViewOverlay.Window(arg_3F_0, NavMeshEditorWindow.<>f__mg$cache0, 400, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
				if (this.m_SelectedNavMeshAgentCount > 0)
				{
					GUIContent arg_80_0 = EditorGUIUtility.TrTextContent("Agent Display", null, null);
					if (NavMeshEditorWindow.<>f__mg$cache1 == null)
					{
						NavMeshEditorWindow.<>f__mg$cache1 = new SceneViewOverlay.WindowFunction(NavMeshEditorWindow.DisplayAgentControls);
					}
					SceneViewOverlay.Window(arg_80_0, NavMeshEditorWindow.<>f__mg$cache1, 400, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
				}
				if (this.m_SelectedNavMeshObstacleCount > 0)
				{
					GUIContent arg_C2_0 = EditorGUIUtility.TrTextContent("Obstacle Display", null, null);
					if (NavMeshEditorWindow.<>f__mg$cache2 == null)
					{
						NavMeshEditorWindow.<>f__mg$cache2 = new SceneViewOverlay.WindowFunction(NavMeshEditorWindow.DisplayObstacleControls);
					}
					SceneViewOverlay.Window(arg_C2_0, NavMeshEditorWindow.<>f__mg$cache2, 400, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
				}
			}
		}

		private static void DisplayControls(UnityEngine.Object target, SceneView sceneView)
		{
			EditorGUIUtility.labelWidth = 150f;
			bool flag = false;
			bool showNavMesh = NavMeshVisualizationSettings.showNavMesh;
			if (showNavMesh != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show NavMesh", null, null), showNavMesh, new GUILayoutOption[0]))
			{
				NavMeshVisualizationSettings.showNavMesh = !showNavMesh;
				flag = true;
			}
			using (new EditorGUI.DisabledScope(!NavMeshVisualizationSettings.hasHeightMesh))
			{
				bool showHeightMesh = NavMeshVisualizationSettings.showHeightMesh;
				if (showHeightMesh != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show HeightMesh", null, null), showHeightMesh, new GUILayoutOption[0]))
				{
					NavMeshVisualizationSettings.showHeightMesh = !showHeightMesh;
					flag = true;
				}
			}
			if (Unsupported.IsDeveloperMode())
			{
				GUILayout.Label("Internal", new GUILayoutOption[0]);
				bool showNavMeshPortals = NavMeshVisualizationSettings.showNavMeshPortals;
				if (showNavMeshPortals != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show NavMesh Portals", null, null), showNavMeshPortals, new GUILayoutOption[0]))
				{
					NavMeshVisualizationSettings.showNavMeshPortals = !showNavMeshPortals;
					flag = true;
				}
				bool showNavMeshLinks = NavMeshVisualizationSettings.showNavMeshLinks;
				if (showNavMeshLinks != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show NavMesh Links", null, null), showNavMeshLinks, new GUILayoutOption[0]))
				{
					NavMeshVisualizationSettings.showNavMeshLinks = !showNavMeshLinks;
					flag = true;
				}
				bool showProximityGrid = NavMeshVisualizationSettings.showProximityGrid;
				if (showProximityGrid != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show Proximity Grid", null, null), showProximityGrid, new GUILayoutOption[0]))
				{
					NavMeshVisualizationSettings.showProximityGrid = !showProximityGrid;
					flag = true;
				}
				bool showHeightMeshBVTree = NavMeshVisualizationSettings.showHeightMeshBVTree;
				if (showHeightMeshBVTree != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show HeightMesh BV-Tree", null, null), showHeightMeshBVTree, new GUILayoutOption[0]))
				{
					NavMeshVisualizationSettings.showHeightMeshBVTree = !showHeightMeshBVTree;
					flag = true;
				}
			}
			if (flag)
			{
				NavMeshEditorWindow.RepaintSceneAndGameViews();
			}
		}

		private void OnInspectorUpdate()
		{
			if (this.m_SelectedNavMeshAgentCount > 0)
			{
				if (this.m_HasPendingAgentDebugInfo != NavMeshVisualizationSettings.hasPendingAgentDebugInfo)
				{
					if (!this.m_HasRepaintedForPendingAgentDebugInfo)
					{
						this.m_HasRepaintedForPendingAgentDebugInfo = true;
						NavMeshEditorWindow.RepaintSceneAndGameViews();
					}
				}
				else
				{
					this.m_HasRepaintedForPendingAgentDebugInfo = false;
				}
			}
		}

		private static void DisplayAgentControls(UnityEngine.Object target, SceneView sceneView)
		{
			EditorGUIUtility.labelWidth = 150f;
			bool flag = false;
			if (Event.current.type == EventType.Layout)
			{
				NavMeshEditorWindow.s_NavMeshEditorWindow.m_HasPendingAgentDebugInfo = NavMeshVisualizationSettings.hasPendingAgentDebugInfo;
			}
			bool showAgentPath = NavMeshVisualizationSettings.showAgentPath;
			if (showAgentPath != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show Path Polygons", "Shows the polygons leading to goal.", null), showAgentPath, new GUILayoutOption[0]))
			{
				NavMeshVisualizationSettings.showAgentPath = !showAgentPath;
				flag = true;
			}
			bool showAgentPathInfo = NavMeshVisualizationSettings.showAgentPathInfo;
			if (showAgentPathInfo != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show Path Query Nodes", "Shows the nodes expanded during last path query.", null), showAgentPathInfo, new GUILayoutOption[0]))
			{
				NavMeshVisualizationSettings.showAgentPathInfo = !showAgentPathInfo;
				flag = true;
			}
			bool showAgentNeighbours = NavMeshVisualizationSettings.showAgentNeighbours;
			if (showAgentNeighbours != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show Neighbours", "Show the agent neighbours cosidered during simulation.", null), showAgentNeighbours, new GUILayoutOption[0]))
			{
				NavMeshVisualizationSettings.showAgentNeighbours = !showAgentNeighbours;
				flag = true;
			}
			bool showAgentWalls = NavMeshVisualizationSettings.showAgentWalls;
			if (showAgentWalls != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show Walls", "Shows the wall segments handled during simulation.", null), showAgentWalls, new GUILayoutOption[0]))
			{
				NavMeshVisualizationSettings.showAgentWalls = !showAgentWalls;
				flag = true;
			}
			bool showAgentAvoidance = NavMeshVisualizationSettings.showAgentAvoidance;
			if (showAgentAvoidance != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show Avoidance", "Shows the processed avoidance geometry from simulation.", null), showAgentAvoidance, new GUILayoutOption[0]))
			{
				NavMeshVisualizationSettings.showAgentAvoidance = !showAgentAvoidance;
				flag = true;
			}
			if (showAgentAvoidance)
			{
				if (NavMeshEditorWindow.s_NavMeshEditorWindow.m_HasPendingAgentDebugInfo)
				{
					EditorGUILayout.BeginVertical(new GUILayoutOption[]
					{
						GUILayout.MaxWidth(165f)
					});
					EditorGUILayout.HelpBox("Avoidance display is not valid until after next game update.", MessageType.Warning);
					EditorGUILayout.EndVertical();
				}
				if (NavMeshEditorWindow.s_NavMeshEditorWindow.m_SelectedNavMeshAgentCount > 10)
				{
					EditorGUILayout.BeginVertical(new GUILayoutOption[]
					{
						GUILayout.MaxWidth(165f)
					});
					EditorGUILayout.HelpBox(string.Format("Avoidance visualization can be drawn for {0} agents ({1} selected).", 10, NavMeshEditorWindow.s_NavMeshEditorWindow.m_SelectedNavMeshAgentCount), MessageType.Warning);
					EditorGUILayout.EndVertical();
				}
			}
			if (flag)
			{
				NavMeshEditorWindow.RepaintSceneAndGameViews();
			}
		}

		private static void DisplayObstacleControls(UnityEngine.Object target, SceneView sceneView)
		{
			EditorGUIUtility.labelWidth = 150f;
			bool flag = false;
			bool showObstacleCarveHull = NavMeshVisualizationSettings.showObstacleCarveHull;
			if (showObstacleCarveHull != EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Show Carve Hull", "Shows the hull used to carve the obstacle from navmesh.", null), showObstacleCarveHull, new GUILayoutOption[0]))
			{
				NavMeshVisualizationSettings.showObstacleCarveHull = !showObstacleCarveHull;
				flag = true;
			}
			if (flag)
			{
				NavMeshEditorWindow.RepaintSceneAndGameViews();
			}
		}

		public virtual void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(EditorGUIUtility.TrTextContent("Reset Legacy Bake Settings", null, null), false, new GenericMenu.MenuFunction(this.ResetBakeSettings));
		}

		private void ResetBakeSettings()
		{
			Unsupported.SmartReset(UnityEditor.AI.NavMeshBuilder.navMeshSettingsObject);
		}

		public static void BackgroundTaskStatusChanged()
		{
			if (NavMeshEditorWindow.s_NavMeshEditorWindow != null)
			{
				NavMeshEditorWindow.s_NavMeshEditorWindow.Repaint();
			}
		}

		private static IEnumerable<GameObject> GetObjectsRecurse(GameObject root)
		{
			List<GameObject> list = new List<GameObject>
			{
				root
			};
			IEnumerator enumerator = root.transform.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					Transform transform = (Transform)enumerator.Current;
					list.AddRange(NavMeshEditorWindow.GetObjectsRecurse(transform.gameObject));
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
			return list;
		}

		private static List<GameObject> GetObjects(bool includeChildren)
		{
			List<GameObject> result;
			if (includeChildren)
			{
				List<GameObject> list = new List<GameObject>();
				GameObject[] gameObjects = Selection.gameObjects;
				for (int i = 0; i < gameObjects.Length; i++)
				{
					GameObject root = gameObjects[i];
					list.AddRange(NavMeshEditorWindow.GetObjectsRecurse(root));
				}
				result = list;
			}
			else
			{
				result = new List<GameObject>(Selection.gameObjects);
			}
			return result;
		}

		private static bool SelectionHasChildren()
		{
			return Selection.gameObjects.Any((GameObject obj) => obj.transform.childCount > 0);
		}

		private static void SetNavMeshArea(int area, bool includeChildren)
		{
			List<GameObject> objects = NavMeshEditorWindow.GetObjects(includeChildren);
			if (objects.Count > 0)
			{
				Undo.RecordObjects(objects.ToArray(), "Change NavMesh area");
				foreach (GameObject current in objects)
				{
					GameObjectUtility.SetNavMeshArea(current, area);
				}
			}
		}

		private static void ObjectSettings()
		{
			bool flag = true;
			SceneModeUtility.SearchBar(new Type[]
			{
				typeof(MeshRenderer),
				typeof(Terrain)
			});
			EditorGUILayout.Space();
			GameObject[] array;
			MeshRenderer[] selectedObjectsOfType = SceneModeUtility.GetSelectedObjectsOfType<MeshRenderer>(out array, new Type[0]);
			if (array.Length > 0)
			{
				flag = false;
				NavMeshEditorWindow.ObjectSettings(selectedObjectsOfType, array);
			}
			Terrain[] selectedObjectsOfType2 = SceneModeUtility.GetSelectedObjectsOfType<Terrain>(out array, new Type[0]);
			if (array.Length > 0)
			{
				flag = false;
				NavMeshEditorWindow.ObjectSettings(selectedObjectsOfType2, array);
			}
			if (flag)
			{
				GUILayout.Label("Select a MeshRenderer or a Terrain from the scene.", EditorStyles.helpBox, new GUILayoutOption[0]);
			}
		}

		private static void ComponentBasedWorkflowButton()
		{
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			if (EditorGUILayout.LinkLabel(NavMeshEditorWindow.s_Styles.m_LearnAboutComponent, new GUILayoutOption[0]))
			{
				Help.BrowseURL("https://github.com/Unity-Technologies/NavMeshComponents");
			}
			GUILayout.EndHorizontal();
		}

		private static void ObjectSettings(UnityEngine.Object[] components, GameObject[] gos)
		{
			NavMeshEditorWindow.ComponentBasedWorkflowButton();
			EditorGUILayout.MultiSelectionObjectTitleBar(components);
			SerializedObject serializedObject = new SerializedObject(gos);
			using (new EditorGUI.DisabledScope(!SceneModeUtility.StaticFlagField("Navigation Static", serializedObject.FindProperty("m_StaticEditorFlags"), 8)))
			{
				SceneModeUtility.StaticFlagField("Generate OffMeshLinks", serializedObject.FindProperty("m_StaticEditorFlags"), 32);
				SerializedProperty serializedProperty = serializedObject.FindProperty("m_NavMeshLayer");
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = serializedProperty.hasMultipleDifferentValues;
				string[] navMeshAreaNames = GameObjectUtility.GetNavMeshAreaNames();
				int navMeshArea = GameObjectUtility.GetNavMeshArea(gos[0]);
				int selectedIndex = -1;
				for (int i = 0; i < navMeshAreaNames.Length; i++)
				{
					if (GameObjectUtility.GetNavMeshAreaFromName(navMeshAreaNames[i]) == navMeshArea)
					{
						selectedIndex = i;
						break;
					}
				}
				int num = EditorGUILayout.Popup("Navigation Area", selectedIndex, navMeshAreaNames, new GUILayoutOption[0]);
				EditorGUI.showMixedValue = false;
				if (EditorGUI.EndChangeCheck())
				{
					int navMeshAreaFromName = GameObjectUtility.GetNavMeshAreaFromName(navMeshAreaNames[num]);
					GameObjectUtility.ShouldIncludeChildren shouldIncludeChildren = GameObjectUtility.DisplayUpdateChildrenDialogIfNeeded(Selection.gameObjects, "Change Navigation Area", "Do you want change the navigation area to " + navMeshAreaNames[num] + " for all the child objects as well?");
					if (shouldIncludeChildren != GameObjectUtility.ShouldIncludeChildren.Cancel)
					{
						serializedProperty.intValue = navMeshAreaFromName;
						NavMeshEditorWindow.SetNavMeshArea(navMeshAreaFromName, shouldIncludeChildren == GameObjectUtility.ShouldIncludeChildren.IncludeChildren);
					}
				}
			}
			serializedObject.ApplyModifiedProperties();
		}

		private void SceneBakeSettings()
		{
			NavMeshEditorWindow.ComponentBasedWorkflowButton();
			if (this.m_SettingsObject == null || this.m_SettingsObject.targetObject == null)
			{
				this.InitSceneBakeSettings();
			}
			this.m_SettingsObject.Update();
			EditorGUILayout.LabelField(NavMeshEditorWindow.s_Styles.m_AgentSizeHeader, EditorStyles.boldLabel, new GUILayoutOption[0]);
			Rect controlRect = EditorGUILayout.GetControlRect(false, 120f, new GUILayoutOption[0]);
			NavMeshEditorHelpers.DrawAgentDiagram(controlRect, this.m_AgentRadius.floatValue, this.m_AgentHeight.floatValue, this.m_AgentClimb.floatValue, this.m_AgentSlope.floatValue);
			float num = EditorGUILayout.FloatField(NavMeshEditorWindow.s_Styles.m_AgentRadiusContent, this.m_AgentRadius.floatValue, new GUILayoutOption[0]);
			if (num >= 0.001f && !Mathf.Approximately(num - this.m_AgentRadius.floatValue, 0f))
			{
				this.m_AgentRadius.floatValue = num;
				if (!this.m_ManualCellSize.boolValue)
				{
					this.m_CellSize.floatValue = 2f * this.m_AgentRadius.floatValue / 6f;
				}
			}
			if (this.m_AgentRadius.floatValue < 0.05f && !this.m_ManualCellSize.boolValue)
			{
				EditorGUILayout.HelpBox("The agent radius you've set is really small, this can slow down the build.\nIf you intended to allow the agent to move close to the borders and walls, please adjust voxel size in advaced settings to ensure correct bake.", MessageType.Warning);
			}
			float num2 = EditorGUILayout.FloatField(NavMeshEditorWindow.s_Styles.m_AgentHeightContent, this.m_AgentHeight.floatValue, new GUILayoutOption[0]);
			if (num2 >= 0.001f && !Mathf.Approximately(num2 - this.m_AgentHeight.floatValue, 0f))
			{
				this.m_AgentHeight.floatValue = num2;
			}
			EditorGUILayout.Slider(this.m_AgentSlope, 0f, 60f, NavMeshEditorWindow.s_Styles.m_AgentSlopeContent, new GUILayoutOption[0]);
			if (this.m_AgentSlope.floatValue > 60f)
			{
				EditorGUILayout.HelpBox("The maximum slope should be set to less than " + 60f + " degrees to prevent NavMesh build artifacts on slopes. ", MessageType.Warning);
			}
			float num3 = EditorGUILayout.FloatField(NavMeshEditorWindow.s_Styles.m_AgentClimbContent, this.m_AgentClimb.floatValue, new GUILayoutOption[0]);
			if (num3 >= 0f && !Mathf.Approximately(this.m_AgentClimb.floatValue - num3, 0f))
			{
				this.m_AgentClimb.floatValue = num3;
			}
			if (this.m_AgentClimb.floatValue > this.m_AgentHeight.floatValue)
			{
				EditorGUILayout.HelpBox("Step height should be less than agent height.\nClamping step height to " + this.m_AgentHeight.floatValue + " internally when baking.", MessageType.Warning);
			}
			float floatValue = this.m_CellSize.floatValue;
			float num4 = floatValue * 0.5f;
			int num5 = (int)Mathf.Ceil(this.m_AgentClimb.floatValue / num4);
			float num6 = Mathf.Tan(this.m_AgentSlope.floatValue / 180f * 3.14159274f) * floatValue;
			int num7 = (int)Mathf.Ceil(num6 * 2f / num4);
			if (num7 > num5)
			{
				float f = (float)num5 * num4 / (floatValue * 2f);
				float num8 = Mathf.Atan(f) / 3.14159274f * 180f;
				float num9 = (float)(num7 - 1) * num4;
				EditorGUILayout.HelpBox(string.Concat(new string[]
				{
					"Step Height conflicts with Max Slope. This makes some slopes unwalkable.\nConsider decreasing Max Slope to < ",
					num8.ToString("0.0"),
					" degrees.\nOr, increase Step Height to > ",
					num9.ToString("0.00"),
					"."
				}), MessageType.Warning);
			}
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(NavMeshEditorWindow.s_Styles.m_OffmeshHeader, EditorStyles.boldLabel, new GUILayoutOption[0]);
			float num10 = EditorGUILayout.FloatField(NavMeshEditorWindow.s_Styles.m_AgentDropContent, this.m_LedgeDropHeight.floatValue, new GUILayoutOption[0]);
			if (num10 >= 0f && !Mathf.Approximately(num10 - this.m_LedgeDropHeight.floatValue, 0f))
			{
				this.m_LedgeDropHeight.floatValue = num10;
			}
			float num11 = EditorGUILayout.FloatField(NavMeshEditorWindow.s_Styles.m_AgentJumpContent, this.m_MaxJumpAcrossDistance.floatValue, new GUILayoutOption[0]);
			if (num11 >= 0f && !Mathf.Approximately(num11 - this.m_MaxJumpAcrossDistance.floatValue, 0f))
			{
				this.m_MaxJumpAcrossDistance.floatValue = num11;
			}
			EditorGUILayout.Space();
			this.m_Advanced = GUILayout.Toggle(this.m_Advanced, NavMeshEditorWindow.s_Styles.m_AdvancedHeader, EditorStyles.foldout, new GUILayoutOption[0]);
			if (this.m_Advanced)
			{
				EditorGUI.indentLevel++;
				bool flag = EditorGUILayout.Toggle(NavMeshEditorWindow.s_Styles.m_ManualCellSizeContent, this.m_ManualCellSize.boolValue, new GUILayoutOption[0]);
				if (flag != this.m_ManualCellSize.boolValue)
				{
					this.m_ManualCellSize.boolValue = flag;
					if (!flag)
					{
						this.m_CellSize.floatValue = 2f * this.m_AgentRadius.floatValue / 6f;
					}
				}
				EditorGUI.indentLevel++;
				using (new EditorGUI.DisabledScope(!this.m_ManualCellSize.boolValue))
				{
					float num12 = EditorGUILayout.FloatField(NavMeshEditorWindow.s_Styles.m_CellSizeContent, this.m_CellSize.floatValue, new GUILayoutOption[0]);
					if (num12 > 0f && !Mathf.Approximately(num12 - this.m_CellSize.floatValue, 0f))
					{
						this.m_CellSize.floatValue = Math.Max(0.01f, num12);
					}
					if (num12 < 0.01f)
					{
						EditorGUILayout.HelpBox("The voxel size should be larger than 0.01.", MessageType.Warning);
					}
					float num13 = (this.m_CellSize.floatValue <= 0f) ? 0f : (this.m_AgentRadius.floatValue / this.m_CellSize.floatValue);
					EditorGUILayout.LabelField(" ", num13.ToString("0.00") + " voxels per agent radius", EditorStyles.miniLabel, new GUILayoutOption[0]);
					if (this.m_ManualCellSize.boolValue)
					{
						float num14 = this.m_CellSize.floatValue * 0.5f;
						if ((int)Mathf.Floor(this.m_AgentHeight.floatValue / num14) > 250)
						{
							EditorGUILayout.HelpBox("The number of voxels per agent height is too high. This will reduce the accuracy of the navmesh. Consider using voxel size of at least " + (this.m_AgentHeight.floatValue / 250f / 0.5f).ToString("0.000") + ".", MessageType.Warning);
						}
						if (num13 < 1f)
						{
							EditorGUILayout.HelpBox("The number of voxels per agent radius is too small. The agent may not avoid walls and ledges properly. Consider using a voxel size less than " + (this.m_AgentRadius.floatValue / 2f).ToString("0.000") + " (2 voxels per agent radius).", MessageType.Warning);
						}
						else if (num13 > 8f)
						{
							EditorGUILayout.HelpBox("The number of voxels per agent radius is too high. It can cause excessive build times. Consider using voxel size closer to " + (this.m_AgentRadius.floatValue / 8f).ToString("0.000") + " (8 voxels per radius).", MessageType.Warning);
						}
					}
					if (this.m_ManualCellSize.boolValue)
					{
						EditorGUILayout.HelpBox("Voxel size controls how accurately the navigation mesh is generated from the level geometry. A good voxel size is 2-4 voxels per agent radius. Making voxel size smaller will increase build time.", MessageType.None);
					}
				}
				EditorGUI.indentLevel--;
				EditorGUILayout.Space();
				float num15 = EditorGUILayout.FloatField(NavMeshEditorWindow.s_Styles.m_MinRegionAreaContent, this.m_MinRegionArea.floatValue, new GUILayoutOption[0]);
				if (num15 >= 0f && num15 != this.m_MinRegionArea.floatValue)
				{
					this.m_MinRegionArea.floatValue = num15;
				}
				EditorGUILayout.Space();
				bool flag2 = EditorGUILayout.Toggle(NavMeshEditorWindow.s_Styles.m_AgentPlacementContent, this.m_AccuratePlacement.boolValue, new GUILayoutOption[0]);
				if (flag2 != this.m_AccuratePlacement.boolValue)
				{
					this.m_AccuratePlacement.boolValue = flag2;
				}
				EditorGUI.indentLevel--;
			}
			this.m_SettingsObject.ApplyModifiedProperties();
			NavMeshEditorWindow.BakeButtons();
		}

		private void AreaSettings()
		{
			if (this.m_Areas == null)
			{
				this.InitAreas();
			}
			this.m_NavMeshProjectSettingsObject.Update();
			this.m_AreasList.DoLayoutList();
			this.m_NavMeshProjectSettingsObject.ApplyModifiedProperties();
		}

		private void AgentSettings()
		{
			if (this.m_Agents == null)
			{
				this.InitAgents();
			}
			this.m_NavMeshProjectSettingsObject.Update();
			if (this.m_AgentsList.index < 0)
			{
				this.m_AgentsList.index = 0;
			}
			this.m_AgentsList.DoLayoutList();
			if (this.m_AgentsList.index >= 0 && this.m_AgentsList.index < this.m_Agents.arraySize)
			{
				SerializedProperty arrayElementAtIndex = this.m_SettingNames.GetArrayElementAtIndex(this.m_AgentsList.index);
				SerializedProperty arrayElementAtIndex2 = this.m_Agents.GetArrayElementAtIndex(this.m_AgentsList.index);
				SerializedProperty serializedProperty = arrayElementAtIndex2.FindPropertyRelative("agentRadius");
				SerializedProperty serializedProperty2 = arrayElementAtIndex2.FindPropertyRelative("agentHeight");
				SerializedProperty serializedProperty3 = arrayElementAtIndex2.FindPropertyRelative("agentClimb");
				SerializedProperty serializedProperty4 = arrayElementAtIndex2.FindPropertyRelative("agentSlope");
				Rect controlRect = EditorGUILayout.GetControlRect(false, 120f, new GUILayoutOption[0]);
				NavMeshEditorHelpers.DrawAgentDiagram(controlRect, serializedProperty.floatValue, serializedProperty2.floatValue, serializedProperty3.floatValue, serializedProperty4.floatValue);
				EditorGUILayout.PropertyField(arrayElementAtIndex, EditorGUIUtility.TempContent("Name"), new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(serializedProperty, EditorGUIUtility.TempContent("Radius"), new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(serializedProperty2, EditorGUIUtility.TempContent("Height"), new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(serializedProperty3, EditorGUIUtility.TempContent("Step Height"), new GUILayoutOption[0]);
				EditorGUILayout.Slider(serializedProperty4, 0f, 60f, EditorGUIUtility.TempContent("Max Slope"), new GUILayoutOption[0]);
			}
			EditorGUILayout.Space();
			this.m_NavMeshProjectSettingsObject.ApplyModifiedProperties();
		}

		private static void BakeButtons()
		{
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			bool enabled = GUI.enabled;
			GUI.enabled &= !Application.isPlaying;
			if (GUILayout.Button("Clear", new GUILayoutOption[]
			{
				GUILayout.Width(95f)
			}))
			{
				UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
			}
			GUI.enabled = enabled;
			if (UnityEditor.AI.NavMeshBuilder.isRunning)
			{
				if (GUILayout.Button("Cancel", new GUILayoutOption[]
				{
					GUILayout.Width(95f)
				}))
				{
					UnityEditor.AI.NavMeshBuilder.Cancel();
				}
			}
			else
			{
				enabled = GUI.enabled;
				GUI.enabled &= !Application.isPlaying;
				if (GUILayout.Button("Bake", new GUILayoutOption[]
				{
					GUILayout.Width(95f)
				}))
				{
					UnityEditor.AI.NavMeshBuilder.BuildNavMeshAsync();
				}
				GUI.enabled = enabled;
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}
	}
}
