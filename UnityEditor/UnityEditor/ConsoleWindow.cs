using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
	[EditorWindowTitle(title = "Console", useTypeNameAsIconName = true)]
	internal class ConsoleWindow : EditorWindow, IHasCustomMenu
	{
		internal delegate void EntryDoubleClickedDelegate(LogEntry entry);

		internal class Constants
		{
			private static bool ms_Loaded;

			private static int ms_logStyleLineCount;

			public static GUIStyle Box;

			public static GUIStyle Button;

			public static GUIStyle MiniButton;

			public static GUIStyle MiniButtonLeft;

			public static GUIStyle MiniButtonMiddle;

			public static GUIStyle MiniButtonRight;

			public static GUIStyle LogStyle;

			public static GUIStyle WarningStyle;

			public static GUIStyle ErrorStyle;

			public static GUIStyle IconLogStyle;

			public static GUIStyle IconWarningStyle;

			public static GUIStyle IconErrorStyle;

			public static GUIStyle EvenBackground;

			public static GUIStyle OddBackground;

			public static GUIStyle MessageStyle;

			public static GUIStyle StatusError;

			public static GUIStyle StatusWarn;

			public static GUIStyle StatusLog;

			public static GUIStyle Toolbar;

			public static GUIStyle CountBadge;

			public static GUIStyle LogSmallStyle;

			public static GUIStyle WarningSmallStyle;

			public static GUIStyle ErrorSmallStyle;

			public static GUIStyle IconLogSmallStyle;

			public static GUIStyle IconWarningSmallStyle;

			public static GUIStyle IconErrorSmallStyle;

			public static readonly string ClearLabel = L10n.Tr("Clear");

			public static readonly string ClearOnPlayLabel = L10n.Tr("Clear on Play");

			public static readonly string ErrorPauseLabel = L10n.Tr("Error Pause");

			public static readonly string CollapseLabel = L10n.Tr("Collapse");

			public static readonly string StopForAssertLabel = L10n.Tr("Stop for Assert");

			public static readonly string StopForErrorLabel = L10n.Tr("Stop for Error");

			public static int LogStyleLineCount
			{
				get
				{
					return ConsoleWindow.Constants.ms_logStyleLineCount;
				}
				set
				{
					ConsoleWindow.Constants.ms_logStyleLineCount = value;
					if (ConsoleWindow.Constants.ms_Loaded)
					{
						ConsoleWindow.Constants.UpdateLogStyleFixedHeights();
					}
				}
			}

			public static void Init()
			{
				if (!ConsoleWindow.Constants.ms_Loaded)
				{
					ConsoleWindow.Constants.ms_Loaded = true;
					ConsoleWindow.Constants.Box = new GUIStyle("CN Box");
					ConsoleWindow.Constants.Button = new GUIStyle("Button");
					ConsoleWindow.Constants.MiniButton = new GUIStyle("ToolbarButton");
					ConsoleWindow.Constants.MiniButtonLeft = new GUIStyle("ToolbarButton");
					ConsoleWindow.Constants.MiniButtonMiddle = new GUIStyle("ToolbarButton");
					ConsoleWindow.Constants.MiniButtonRight = new GUIStyle("ToolbarButton");
					ConsoleWindow.Constants.Toolbar = new GUIStyle("Toolbar");
					ConsoleWindow.Constants.LogStyle = new GUIStyle("CN EntryInfo");
					ConsoleWindow.Constants.LogSmallStyle = new GUIStyle("CN EntryInfoSmall");
					ConsoleWindow.Constants.WarningStyle = new GUIStyle("CN EntryWarn");
					ConsoleWindow.Constants.WarningSmallStyle = new GUIStyle("CN EntryWarnSmall");
					ConsoleWindow.Constants.ErrorStyle = new GUIStyle("CN EntryError");
					ConsoleWindow.Constants.ErrorSmallStyle = new GUIStyle("CN EntryErrorSmall");
					ConsoleWindow.Constants.IconLogStyle = new GUIStyle("CN EntryInfoIcon");
					ConsoleWindow.Constants.IconLogSmallStyle = new GUIStyle("CN EntryInfoIconSmall");
					ConsoleWindow.Constants.IconWarningStyle = new GUIStyle("CN EntryWarnIcon");
					ConsoleWindow.Constants.IconWarningSmallStyle = new GUIStyle("CN EntryWarnIconSmall");
					ConsoleWindow.Constants.IconErrorStyle = new GUIStyle("CN EntryErrorIcon");
					ConsoleWindow.Constants.IconErrorSmallStyle = new GUIStyle("CN EntryErrorIconSmall");
					ConsoleWindow.Constants.EvenBackground = new GUIStyle("CN EntryBackEven");
					ConsoleWindow.Constants.OddBackground = new GUIStyle("CN EntryBackodd");
					ConsoleWindow.Constants.MessageStyle = new GUIStyle("CN Message");
					ConsoleWindow.Constants.StatusError = new GUIStyle("CN StatusError");
					ConsoleWindow.Constants.StatusWarn = new GUIStyle("CN StatusWarn");
					ConsoleWindow.Constants.StatusLog = new GUIStyle("CN StatusInfo");
					ConsoleWindow.Constants.CountBadge = new GUIStyle("CN CountBadge");
					ConsoleWindow.Constants.LogStyleLineCount = EditorPrefs.GetInt("ConsoleWindowLogLineCount", 2);
				}
			}

			private static void UpdateLogStyleFixedHeights()
			{
				ConsoleWindow.Constants.ErrorStyle.fixedHeight = (float)ConsoleWindow.Constants.LogStyleLineCount * ConsoleWindow.Constants.ErrorStyle.lineHeight + (float)ConsoleWindow.Constants.ErrorStyle.border.top;
				ConsoleWindow.Constants.WarningStyle.fixedHeight = (float)ConsoleWindow.Constants.LogStyleLineCount * ConsoleWindow.Constants.WarningStyle.lineHeight + (float)ConsoleWindow.Constants.WarningStyle.border.top;
				ConsoleWindow.Constants.LogStyle.fixedHeight = (float)ConsoleWindow.Constants.LogStyleLineCount * ConsoleWindow.Constants.LogStyle.lineHeight + (float)ConsoleWindow.Constants.LogStyle.border.top;
			}
		}

		private class ConsoleAttachProfilerUI : AttachProfilerUI
		{
			private enum MenuItemIndex
			{
				PlayerLogging,
				FullLog
			}

			private List<string> additionalMenuItems = null;

			protected void SelectClick(object userData, string[] options, int selected)
			{
				if (selected == 0)
				{
					bool flag = ScriptableSingleton<PlayerConnectionLogReceiver>.instance.State != PlayerConnectionLogReceiver.ConnectionState.Disconnected;
					ScriptableSingleton<PlayerConnectionLogReceiver>.instance.State = ((!flag) ? PlayerConnectionLogReceiver.ConnectionState.CleanLog : PlayerConnectionLogReceiver.ConnectionState.Disconnected);
				}
				else if (selected == 1)
				{
					bool flag2 = ScriptableSingleton<PlayerConnectionLogReceiver>.instance.State == PlayerConnectionLogReceiver.ConnectionState.CleanLog;
					ScriptableSingleton<PlayerConnectionLogReceiver>.instance.State = ((!flag2) ? PlayerConnectionLogReceiver.ConnectionState.CleanLog : PlayerConnectionLogReceiver.ConnectionState.FullLog);
				}
				else if (selected >= this.additionalMenuItems.Count)
				{
					base.SelectProfilerClick(userData, options, selected - this.additionalMenuItems.Count);
				}
			}

			protected override void OnGUIMenu(Rect connectRect, List<ProfilerChoise> profilers)
			{
				if (this.additionalMenuItems == null)
				{
					this.additionalMenuItems = new List<string>();
					this.additionalMenuItems.Add("Player Logging");
					if (Unsupported.IsDeveloperMode())
					{
						this.additionalMenuItems.Add("Full Log (Developer Mode Only)");
					}
					this.additionalMenuItems.Add("");
				}
				IEnumerable<string> source = this.additionalMenuItems.Concat(from p in profilers
				select p.Name);
				List<bool> list = new List<bool>();
				list.Add(true);
				List<int> list2 = new List<int>();
				bool flag = ScriptableSingleton<PlayerConnectionLogReceiver>.instance.State != PlayerConnectionLogReceiver.ConnectionState.Disconnected;
				if (flag)
				{
					list2.Add(0);
					if (Unsupported.IsDeveloperMode())
					{
						if (ScriptableSingleton<PlayerConnectionLogReceiver>.instance.State == PlayerConnectionLogReceiver.ConnectionState.FullLog)
						{
							list2.Add(1);
						}
						list.Add(true);
					}
					list.Add(true);
					list.AddRange(from p in profilers
					select p.Enabled);
				}
				else
				{
					list.AddRange(new bool[source.Count<string>() - 1]);
				}
				int num = profilers.FindIndex((ProfilerChoise p) => p.IsSelected());
				if (num != -1)
				{
					list2.Add(num + this.additionalMenuItems.Count);
				}
				bool[] array = new bool[list.Count];
				array[this.additionalMenuItems.Count - 1] = true;
				EditorUtility.DisplayCustomMenuWithSeparators(connectRect, source.ToArray<string>(), list.ToArray(), array, list2.ToArray(), new EditorUtility.SelectMenuItemFunction(this.SelectClick), profilers);
			}
		}

		internal enum Mode
		{
			Error = 1,
			Assert,
			Log = 4,
			Fatal = 16,
			DontPreprocessCondition = 32,
			AssetImportError = 64,
			AssetImportWarning = 128,
			ScriptingError = 256,
			ScriptingWarning = 512,
			ScriptingLog = 1024,
			ScriptCompileError = 2048,
			ScriptCompileWarning = 4096,
			StickyError = 8192,
			MayIgnoreLineNumber = 16384,
			ReportBug = 32768,
			DisplayPreviousErrorInStatusBar = 65536,
			ScriptingException = 131072,
			DontExtractStacktrace = 262144,
			ShouldClearOnPlay = 524288,
			GraphCompileError = 1048576,
			ScriptingAssertion = 2097152,
			VisualScriptingError = 4194304
		}

		private enum ConsoleFlags
		{
			Collapse = 1,
			ClearOnPlay,
			ErrorPause = 4,
			Verbose = 8,
			StopForAssert = 16,
			StopForError = 32,
			Autoscroll = 64,
			LogLevelLog = 128,
			LogLevelWarning = 256,
			LogLevelError = 512,
			ShowTimestamp = 1024
		}

		public struct StackTraceLogTypeData
		{
			public LogType logType;

			public StackTraceLogType stackTraceLogType;
		}

		private int m_LineHeight;

		private int m_BorderHeight;

		private bool m_HasUpdatedGuiStyles;

		private ListViewState m_ListView;

		private string m_ActiveText = "";

		private int m_ActiveInstanceID = 0;

		private bool m_DevBuild;

		private Vector2 m_TextScroll = Vector2.zero;

		private SplitterState spl = new SplitterState(new float[]
		{
			70f,
			30f
		}, new int[]
		{
			32,
			32
		}, null);

		private static bool ms_LoadedIcons = false;

		internal static Texture2D iconInfo;

		internal static Texture2D iconWarn;

		internal static Texture2D iconError;

		internal static Texture2D iconInfoSmall;

		internal static Texture2D iconWarnSmall;

		internal static Texture2D iconErrorSmall;

		internal static Texture2D iconInfoMono;

		internal static Texture2D iconWarnMono;

		internal static Texture2D iconErrorMono;

		private int ms_LVHeight = 0;

		private ConsoleWindow.ConsoleAttachProfilerUI m_AttachProfilerUI = new ConsoleWindow.ConsoleAttachProfilerUI();

		private static ConsoleWindow ms_ConsoleWindow = null;

		[CompilerGenerated]
		private static GenericMenu.MenuFunction <>f__mg$cache0;

		[CompilerGenerated]
		private static GenericMenu.MenuFunction <>f__mg$cache1;

		private static event ConsoleWindow.EntryDoubleClickedDelegate entryWithManagedCallbackDoubleClicked
		{
			add
			{
				ConsoleWindow.EntryDoubleClickedDelegate entryDoubleClickedDelegate = ConsoleWindow.entryWithManagedCallbackDoubleClicked;
				ConsoleWindow.EntryDoubleClickedDelegate entryDoubleClickedDelegate2;
				do
				{
					entryDoubleClickedDelegate2 = entryDoubleClickedDelegate;
					entryDoubleClickedDelegate = Interlocked.CompareExchange<ConsoleWindow.EntryDoubleClickedDelegate>(ref ConsoleWindow.entryWithManagedCallbackDoubleClicked, (ConsoleWindow.EntryDoubleClickedDelegate)Delegate.Combine(entryDoubleClickedDelegate2, value), entryDoubleClickedDelegate);
				}
				while (entryDoubleClickedDelegate != entryDoubleClickedDelegate2);
			}
			remove
			{
				ConsoleWindow.EntryDoubleClickedDelegate entryDoubleClickedDelegate = ConsoleWindow.entryWithManagedCallbackDoubleClicked;
				ConsoleWindow.EntryDoubleClickedDelegate entryDoubleClickedDelegate2;
				do
				{
					entryDoubleClickedDelegate2 = entryDoubleClickedDelegate;
					entryDoubleClickedDelegate = Interlocked.CompareExchange<ConsoleWindow.EntryDoubleClickedDelegate>(ref ConsoleWindow.entryWithManagedCallbackDoubleClicked, (ConsoleWindow.EntryDoubleClickedDelegate)Delegate.Remove(entryDoubleClickedDelegate2, value), entryDoubleClickedDelegate);
				}
				while (entryDoubleClickedDelegate != entryDoubleClickedDelegate2);
			}
		}

		private int RowHeight
		{
			get
			{
				return ConsoleWindow.Constants.LogStyleLineCount * this.m_LineHeight + this.m_BorderHeight;
			}
		}

		public ConsoleWindow()
		{
			base.position = new Rect(200f, 200f, 800f, 400f);
			this.m_ListView = new ListViewState(0, 0);
		}

		private static void ShowConsoleWindowImmediate()
		{
			ConsoleWindow.ShowConsoleWindow(true);
		}

		public static void ShowConsoleWindow(bool immediate)
		{
			if (ConsoleWindow.ms_ConsoleWindow == null)
			{
				ConsoleWindow.ms_ConsoleWindow = ScriptableObject.CreateInstance<ConsoleWindow>();
				ConsoleWindow.ms_ConsoleWindow.Show(immediate);
				ConsoleWindow.ms_ConsoleWindow.Focus();
			}
			else
			{
				ConsoleWindow.ms_ConsoleWindow.Show(immediate);
				ConsoleWindow.ms_ConsoleWindow.Focus();
			}
		}

		internal static void LoadIcons()
		{
			if (!ConsoleWindow.ms_LoadedIcons)
			{
				ConsoleWindow.ms_LoadedIcons = true;
				ConsoleWindow.iconInfo = EditorGUIUtility.LoadIcon("console.infoicon");
				ConsoleWindow.iconWarn = EditorGUIUtility.LoadIcon("console.warnicon");
				ConsoleWindow.iconError = EditorGUIUtility.LoadIcon("console.erroricon");
				ConsoleWindow.iconInfoSmall = EditorGUIUtility.LoadIcon("console.infoicon.sml");
				ConsoleWindow.iconWarnSmall = EditorGUIUtility.LoadIcon("console.warnicon.sml");
				ConsoleWindow.iconErrorSmall = EditorGUIUtility.LoadIcon("console.erroricon.sml");
				ConsoleWindow.iconInfoMono = EditorGUIUtility.LoadIcon("console.infoicon.sml");
				ConsoleWindow.iconWarnMono = EditorGUIUtility.LoadIcon("console.warnicon.inactive.sml");
				ConsoleWindow.iconErrorMono = EditorGUIUtility.LoadIcon("console.erroricon.inactive.sml");
				ConsoleWindow.Constants.Init();
			}
		}

		[RequiredByNativeCode]
		public static void LogChanged()
		{
			if (!(ConsoleWindow.ms_ConsoleWindow == null))
			{
				ConsoleWindow.ms_ConsoleWindow.DoLogChanged();
			}
		}

		public void DoLogChanged()
		{
			ConsoleWindow.ms_ConsoleWindow.Repaint();
		}

		private void OnEnable()
		{
			base.titleContent = base.GetLocalizedTitleContent();
			ConsoleWindow.ms_ConsoleWindow = this;
			this.m_DevBuild = Unsupported.IsDeveloperMode();
			ConsoleWindow.Constants.LogStyleLineCount = EditorPrefs.GetInt("ConsoleWindowLogLineCount", 2);
		}

		private void OnDisable()
		{
			if (ConsoleWindow.ms_ConsoleWindow == this)
			{
				ConsoleWindow.ms_ConsoleWindow = null;
			}
		}

		private static bool HasMode(int mode, ConsoleWindow.Mode modeToCheck)
		{
			return (mode & (int)modeToCheck) != 0;
		}

		private static bool HasFlag(ConsoleWindow.ConsoleFlags flags)
		{
			return (LogEntries.consoleFlags & (int)flags) != 0;
		}

		private static void SetFlag(ConsoleWindow.ConsoleFlags flags, bool val)
		{
			LogEntries.SetConsoleFlag((int)flags, val);
		}

		internal static Texture2D GetIconForErrorMode(int mode, bool large)
		{
			Texture2D result;
			if (ConsoleWindow.HasMode(mode, (ConsoleWindow.Mode)3148115))
			{
				result = ((!large) ? ConsoleWindow.iconErrorSmall : ConsoleWindow.iconError);
			}
			else if (ConsoleWindow.HasMode(mode, (ConsoleWindow.Mode)4736))
			{
				result = ((!large) ? ConsoleWindow.iconWarnSmall : ConsoleWindow.iconWarn);
			}
			else if (ConsoleWindow.HasMode(mode, (ConsoleWindow.Mode)1028))
			{
				result = ((!large) ? ConsoleWindow.iconInfoSmall : ConsoleWindow.iconInfo);
			}
			else
			{
				result = null;
			}
			return result;
		}

		internal static GUIStyle GetStyleForErrorMode(int mode, bool isIcon, bool isSmall)
		{
			GUIStyle result;
			if (ConsoleWindow.HasMode(mode, (ConsoleWindow.Mode)3148115))
			{
				if (isIcon)
				{
					if (isSmall)
					{
						result = ConsoleWindow.Constants.IconErrorSmallStyle;
					}
					else
					{
						result = ConsoleWindow.Constants.IconErrorStyle;
					}
				}
				else if (isSmall)
				{
					result = ConsoleWindow.Constants.ErrorSmallStyle;
				}
				else
				{
					result = ConsoleWindow.Constants.ErrorStyle;
				}
			}
			else if (ConsoleWindow.HasMode(mode, (ConsoleWindow.Mode)4736))
			{
				if (isIcon)
				{
					if (isSmall)
					{
						result = ConsoleWindow.Constants.IconWarningSmallStyle;
					}
					else
					{
						result = ConsoleWindow.Constants.IconWarningStyle;
					}
				}
				else if (isSmall)
				{
					result = ConsoleWindow.Constants.WarningSmallStyle;
				}
				else
				{
					result = ConsoleWindow.Constants.WarningStyle;
				}
			}
			else if (isIcon)
			{
				if (isSmall)
				{
					result = ConsoleWindow.Constants.IconLogSmallStyle;
				}
				else
				{
					result = ConsoleWindow.Constants.IconLogStyle;
				}
			}
			else if (isSmall)
			{
				result = ConsoleWindow.Constants.LogSmallStyle;
			}
			else
			{
				result = ConsoleWindow.Constants.LogStyle;
			}
			return result;
		}

		internal static GUIStyle GetStatusStyleForErrorMode(int mode)
		{
			GUIStyle result;
			if (ConsoleWindow.HasMode(mode, (ConsoleWindow.Mode)3148115))
			{
				result = ConsoleWindow.Constants.StatusError;
			}
			else if (ConsoleWindow.HasMode(mode, (ConsoleWindow.Mode)4736))
			{
				result = ConsoleWindow.Constants.StatusWarn;
			}
			else
			{
				result = ConsoleWindow.Constants.StatusLog;
			}
			return result;
		}

		private static string ContextString(LogEntry entry)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (ConsoleWindow.HasMode(entry.mode, ConsoleWindow.Mode.Error))
			{
				stringBuilder.Append("Error ");
			}
			else if (ConsoleWindow.HasMode(entry.mode, ConsoleWindow.Mode.Log))
			{
				stringBuilder.Append("Log ");
			}
			else
			{
				stringBuilder.Append("Assert ");
			}
			stringBuilder.Append("in file: ");
			stringBuilder.Append(entry.file);
			stringBuilder.Append(" at line: ");
			stringBuilder.Append(entry.line);
			if (entry.errorNum != 0)
			{
				stringBuilder.Append(" and errorNum: ");
				stringBuilder.Append(entry.errorNum);
			}
			return stringBuilder.ToString();
		}

		private static string GetFirstLine(string s)
		{
			int num = s.IndexOf("\n");
			return (num == -1) ? s : s.Substring(0, num);
		}

		private static string GetFirstTwoLines(string s)
		{
			int num = s.IndexOf("\n");
			string result;
			if (num != -1)
			{
				num = s.IndexOf("\n", num + 1);
				if (num != -1)
				{
					result = s.Substring(0, num);
					return result;
				}
			}
			result = s;
			return result;
		}

		private void SetActiveEntry(LogEntry entry)
		{
			if (entry != null)
			{
				this.m_ActiveText = entry.condition;
				if (this.m_ActiveInstanceID != entry.instanceID)
				{
					this.m_ActiveInstanceID = entry.instanceID;
					if (entry.instanceID != 0)
					{
						EditorGUIUtility.PingObject(entry.instanceID);
					}
				}
			}
			else
			{
				this.m_ActiveText = string.Empty;
				this.m_ActiveInstanceID = 0;
				this.m_ListView.row = -1;
			}
		}

		private static void ShowConsoleRow(int row)
		{
			ConsoleWindow.ShowConsoleWindow(false);
			if (ConsoleWindow.ms_ConsoleWindow)
			{
				ConsoleWindow.ms_ConsoleWindow.m_ListView.row = row;
				ConsoleWindow.ms_ConsoleWindow.m_ListView.selectionChanged = true;
				ConsoleWindow.ms_ConsoleWindow.Repaint();
			}
		}

		private void UpdateListView()
		{
			this.m_HasUpdatedGuiStyles = true;
			int rowHeight = this.RowHeight;
			this.m_ListView.rowHeight = rowHeight;
			this.m_ListView.row = -1;
			this.m_ListView.scrollPos.y = (float)(LogEntries.GetCount() * rowHeight);
		}

		private void OnGUI()
		{
			Event current = Event.current;
			ConsoleWindow.LoadIcons();
			if (!this.m_HasUpdatedGuiStyles)
			{
				this.m_LineHeight = Mathf.RoundToInt(ConsoleWindow.Constants.ErrorStyle.lineHeight);
				this.m_BorderHeight = ConsoleWindow.Constants.ErrorStyle.border.top + ConsoleWindow.Constants.ErrorStyle.border.bottom;
				this.UpdateListView();
			}
			GUILayout.BeginHorizontal(ConsoleWindow.Constants.Toolbar, new GUILayoutOption[0]);
			if (GUILayout.Button(ConsoleWindow.Constants.ClearLabel, ConsoleWindow.Constants.MiniButton, new GUILayoutOption[0]))
			{
				LogEntries.Clear();
				GUIUtility.keyboardControl = 0;
			}
			int count = LogEntries.GetCount();
			if (this.m_ListView.totalRows != count && this.m_ListView.totalRows > 0)
			{
				if (this.m_ListView.scrollPos.y >= (float)(this.m_ListView.rowHeight * this.m_ListView.totalRows - this.ms_LVHeight))
				{
					this.m_ListView.scrollPos.y = (float)(count * this.RowHeight - this.ms_LVHeight);
				}
			}
			EditorGUILayout.Space();
			bool flag = ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.Collapse);
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.Collapse, GUILayout.Toggle(flag, ConsoleWindow.Constants.CollapseLabel, ConsoleWindow.Constants.MiniButtonLeft, new GUILayoutOption[0]));
			bool flag2 = flag != ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.Collapse);
			if (flag2)
			{
				this.m_ListView.row = -1;
				this.m_ListView.scrollPos.y = (float)(LogEntries.GetCount() * this.RowHeight);
			}
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.ClearOnPlay, GUILayout.Toggle(ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.ClearOnPlay), ConsoleWindow.Constants.ClearOnPlayLabel, ConsoleWindow.Constants.MiniButtonMiddle, new GUILayoutOption[0]));
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.ErrorPause, GUILayout.Toggle(ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.ErrorPause), ConsoleWindow.Constants.ErrorPauseLabel, ConsoleWindow.Constants.MiniButtonRight, new GUILayoutOption[0]));
			this.m_AttachProfilerUI.OnGUILayout(this);
			EditorGUILayout.Space();
			if (this.m_DevBuild)
			{
				GUILayout.FlexibleSpace();
				ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.StopForAssert, GUILayout.Toggle(ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.StopForAssert), ConsoleWindow.Constants.StopForAssertLabel, ConsoleWindow.Constants.MiniButtonLeft, new GUILayoutOption[0]));
				ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.StopForError, GUILayout.Toggle(ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.StopForError), ConsoleWindow.Constants.StopForErrorLabel, ConsoleWindow.Constants.MiniButtonRight, new GUILayoutOption[0]));
			}
			GUILayout.FlexibleSpace();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			LogEntries.GetCountsByType(ref num, ref num2, ref num3);
			EditorGUI.BeginChangeCheck();
			bool val = GUILayout.Toggle(ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.LogLevelLog), new GUIContent((num3 > 999) ? "999+" : num3.ToString(), (num3 <= 0) ? ConsoleWindow.iconInfoMono : ConsoleWindow.iconInfoSmall), ConsoleWindow.Constants.MiniButtonRight, new GUILayoutOption[0]);
			bool val2 = GUILayout.Toggle(ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.LogLevelWarning), new GUIContent((num2 > 999) ? "999+" : num2.ToString(), (num2 <= 0) ? ConsoleWindow.iconWarnMono : ConsoleWindow.iconWarnSmall), ConsoleWindow.Constants.MiniButtonMiddle, new GUILayoutOption[0]);
			bool val3 = GUILayout.Toggle(ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.LogLevelError), new GUIContent((num > 999) ? "999+" : num.ToString(), (num <= 0) ? ConsoleWindow.iconErrorMono : ConsoleWindow.iconErrorSmall), ConsoleWindow.Constants.MiniButtonLeft, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck())
			{
				this.SetActiveEntry(null);
			}
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.LogLevelLog, val);
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.LogLevelWarning, val2);
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.LogLevelError, val3);
			GUILayout.EndHorizontal();
			this.m_ListView.totalRows = LogEntries.StartGettingEntries();
			SplitterGUILayout.BeginVerticalSplit(this.spl, new GUILayoutOption[0]);
			int rowHeight = this.RowHeight;
			EditorGUIUtility.SetIconSize(new Vector2((float)rowHeight, (float)rowHeight));
			GUIContent gUIContent = new GUIContent();
			int controlID = GUIUtility.GetControlID(FocusType.Native);
			try
			{
				bool flag3 = false;
				bool flag4 = ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.Collapse);
				IEnumerator enumerator = ListViewGUI.ListView(this.m_ListView, ConsoleWindow.Constants.Box, new GUILayoutOption[0]).GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						ListViewElement listViewElement = (ListViewElement)enumerator.Current;
						if (current.type == EventType.MouseDown && current.button == 0 && listViewElement.position.Contains(current.mousePosition))
						{
							if (current.clickCount == 2)
							{
								LogEntries.RowGotDoubleClicked(this.m_ListView.row);
							}
							flag3 = true;
						}
						if (current.type == EventType.Repaint)
						{
							int mode = 0;
							string text = null;
							LogEntries.GetLinesAndModeFromEntryInternal(listViewElement.row, ConsoleWindow.Constants.LogStyleLineCount, ref mode, ref text);
							GUIStyle gUIStyle = (listViewElement.row % 2 != 0) ? ConsoleWindow.Constants.EvenBackground : ConsoleWindow.Constants.OddBackground;
							gUIStyle.Draw(listViewElement.position, false, false, this.m_ListView.row == listViewElement.row, false);
							GUIStyle styleForErrorMode = ConsoleWindow.GetStyleForErrorMode(mode, true, ConsoleWindow.Constants.LogStyleLineCount == 1);
							styleForErrorMode.Draw(listViewElement.position, false, false, this.m_ListView.row == listViewElement.row, false);
							gUIContent.text = text;
							GUIStyle styleForErrorMode2 = ConsoleWindow.GetStyleForErrorMode(mode, false, ConsoleWindow.Constants.LogStyleLineCount == 1);
							styleForErrorMode2.Draw(listViewElement.position, gUIContent, controlID, this.m_ListView.row == listViewElement.row);
							if (flag4)
							{
								Rect position = listViewElement.position;
								gUIContent.text = LogEntries.GetEntryCount(listViewElement.row).ToString(CultureInfo.InvariantCulture);
								Vector2 vector = ConsoleWindow.Constants.CountBadge.CalcSize(gUIContent);
								position.xMin = position.xMax - vector.x;
								position.yMin += (position.yMax - position.yMin - vector.y) * 0.5f;
								position.x -= 5f;
								GUI.Label(position, gUIContent, ConsoleWindow.Constants.CountBadge);
							}
						}
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
				if (flag3)
				{
					if (this.m_ListView.scrollPos.y >= (float)(this.m_ListView.rowHeight * this.m_ListView.totalRows - this.ms_LVHeight))
					{
						this.m_ListView.scrollPos.y = (float)(this.m_ListView.rowHeight * this.m_ListView.totalRows - this.ms_LVHeight - 1);
					}
				}
				if (this.m_ListView.totalRows == 0 || this.m_ListView.row >= this.m_ListView.totalRows || this.m_ListView.row < 0)
				{
					if (this.m_ActiveText.Length != 0)
					{
						this.SetActiveEntry(null);
					}
				}
				else
				{
					LogEntry logEntry = new LogEntry();
					LogEntries.GetEntryInternal(this.m_ListView.row, logEntry);
					this.SetActiveEntry(logEntry);
					LogEntries.GetEntryInternal(this.m_ListView.row, logEntry);
					if (this.m_ListView.selectionChanged || !this.m_ActiveText.Equals(logEntry.condition))
					{
						this.SetActiveEntry(logEntry);
					}
				}
				if (GUIUtility.keyboardControl == this.m_ListView.ID && current.type == EventType.KeyDown && current.keyCode == KeyCode.Return && this.m_ListView.row != 0)
				{
					LogEntries.RowGotDoubleClicked(this.m_ListView.row);
					Event.current.Use();
				}
				if (current.type != EventType.Layout && ListViewGUI.ilvState.rectHeight != 1)
				{
					this.ms_LVHeight = ListViewGUI.ilvState.rectHeight;
				}
			}
			finally
			{
				LogEntries.EndGettingEntries();
				EditorGUIUtility.SetIconSize(Vector2.zero);
			}
			this.m_TextScroll = GUILayout.BeginScrollView(this.m_TextScroll, ConsoleWindow.Constants.Box);
			float minHeight = ConsoleWindow.Constants.MessageStyle.CalcHeight(GUIContent.Temp(this.m_ActiveText), base.position.width);
			EditorGUILayout.SelectableLabel(this.m_ActiveText, ConsoleWindow.Constants.MessageStyle, new GUILayoutOption[]
			{
				GUILayout.ExpandWidth(true),
				GUILayout.ExpandHeight(true),
				GUILayout.MinHeight(minHeight)
			});
			GUILayout.EndScrollView();
			SplitterGUILayout.EndVerticalSplit();
			if ((current.type == EventType.ValidateCommand || current.type == EventType.ExecuteCommand) && current.commandName == "Copy" && this.m_ActiveText != string.Empty)
			{
				if (current.type == EventType.ExecuteCommand)
				{
					EditorGUIUtility.systemCopyBuffer = this.m_ActiveText;
				}
				current.Use();
			}
		}

		public static bool GetConsoleErrorPause()
		{
			return ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.ErrorPause);
		}

		public static void SetConsoleErrorPause(bool enabled)
		{
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.ErrorPause, enabled);
		}

		public void ToggleLogStackTraces(object userData)
		{
			ConsoleWindow.StackTraceLogTypeData stackTraceLogTypeData = (ConsoleWindow.StackTraceLogTypeData)userData;
			PlayerSettings.SetStackTraceLogType(stackTraceLogTypeData.logType, stackTraceLogTypeData.stackTraceLogType);
		}

		public void ToggleLogStackTracesForAll(object userData)
		{
			IEnumerator enumerator = Enum.GetValues(typeof(LogType)).GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					LogType logType = (LogType)enumerator.Current;
					PlayerSettings.SetStackTraceLogType(logType, (StackTraceLogType)userData);
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
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
			if (Application.platform == RuntimePlatform.OSXEditor)
			{
				GUIContent arg_36_1 = EditorGUIUtility.TrTextContent("Open Player Log", null, null);
				bool arg_36_2 = false;
				if (ConsoleWindow.<>f__mg$cache0 == null)
				{
					ConsoleWindow.<>f__mg$cache0 = new GenericMenu.MenuFunction(InternalEditorUtility.OpenPlayerConsole);
				}
				menu.AddItem(arg_36_1, arg_36_2, ConsoleWindow.<>f__mg$cache0);
			}
			GUIContent arg_66_1 = EditorGUIUtility.TrTextContent("Open Editor Log", null, null);
			bool arg_66_2 = false;
			if (ConsoleWindow.<>f__mg$cache1 == null)
			{
				ConsoleWindow.<>f__mg$cache1 = new GenericMenu.MenuFunction(InternalEditorUtility.OpenEditorConsole);
			}
			menu.AddItem(arg_66_1, arg_66_2, ConsoleWindow.<>f__mg$cache1);
			menu.AddItem(EditorGUIUtility.TrTextContent("Show Timestamp", null, null), ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.ShowTimestamp), new GenericMenu.MenuFunction(this.SetTimestamp));
			for (int i = 1; i <= 10; i++)
			{
				string arg = (i != 1) ? "Lines" : "Line";
				menu.AddItem(new GUIContent(string.Format("Log Entry/{0} {1}", i, arg)), i == ConsoleWindow.Constants.LogStyleLineCount, new GenericMenu.MenuFunction2(this.SetLogLineCount), i);
			}
			this.AddStackTraceLoggingMenu(menu);
		}

		private void SetTimestamp()
		{
			ConsoleWindow.SetFlag(ConsoleWindow.ConsoleFlags.ShowTimestamp, !ConsoleWindow.HasFlag(ConsoleWindow.ConsoleFlags.ShowTimestamp));
		}

		private void SetLogLineCount(object obj)
		{
			int num = (int)obj;
			EditorPrefs.SetInt("ConsoleWindowLogLineCount", num);
			ConsoleWindow.Constants.LogStyleLineCount = num;
			this.UpdateListView();
		}

		private void AddStackTraceLoggingMenu(GenericMenu menu)
		{
			IEnumerator enumerator = Enum.GetValues(typeof(LogType)).GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					LogType logType = (LogType)enumerator.Current;
					IEnumerator enumerator2 = Enum.GetValues(typeof(StackTraceLogType)).GetEnumerator();
					try
					{
						while (enumerator2.MoveNext())
						{
							StackTraceLogType stackTraceLogType = (StackTraceLogType)enumerator2.Current;
							ConsoleWindow.StackTraceLogTypeData stackTraceLogTypeData;
							stackTraceLogTypeData.logType = logType;
							stackTraceLogTypeData.stackTraceLogType = stackTraceLogType;
							menu.AddItem(EditorGUIUtility.TrTextContent(string.Concat(new object[]
							{
								"Stack Trace Logging/",
								logType,
								"/",
								stackTraceLogType
							}), null, null), PlayerSettings.GetStackTraceLogType(logType) == stackTraceLogType, new GenericMenu.MenuFunction2(this.ToggleLogStackTraces), stackTraceLogTypeData);
						}
					}
					finally
					{
						IDisposable disposable;
						if ((disposable = (enumerator2 as IDisposable)) != null)
						{
							disposable.Dispose();
						}
					}
				}
			}
			finally
			{
				IDisposable disposable2;
				if ((disposable2 = (enumerator as IDisposable)) != null)
				{
					disposable2.Dispose();
				}
			}
			int num = (int)PlayerSettings.GetStackTraceLogType(LogType.Log);
			IEnumerator enumerator3 = Enum.GetValues(typeof(LogType)).GetEnumerator();
			try
			{
				while (enumerator3.MoveNext())
				{
					LogType logType2 = (LogType)enumerator3.Current;
					if (PlayerSettings.GetStackTraceLogType(logType2) != (StackTraceLogType)num)
					{
						num = -1;
						break;
					}
				}
			}
			finally
			{
				IDisposable disposable3;
				if ((disposable3 = (enumerator3 as IDisposable)) != null)
				{
					disposable3.Dispose();
				}
			}
			IEnumerator enumerator4 = Enum.GetValues(typeof(StackTraceLogType)).GetEnumerator();
			try
			{
				while (enumerator4.MoveNext())
				{
					StackTraceLogType stackTraceLogType2 = (StackTraceLogType)enumerator4.Current;
					menu.AddItem(EditorGUIUtility.TrTextContent("Stack Trace Logging/All/" + stackTraceLogType2, null, null), num == (int)stackTraceLogType2, new GenericMenu.MenuFunction2(this.ToggleLogStackTracesForAll), stackTraceLogType2);
				}
			}
			finally
			{
				IDisposable disposable4;
				if ((disposable4 = (enumerator4 as IDisposable)) != null)
				{
					disposable4.Dispose();
				}
			}
		}

		private static void SendEntryDoubleClicked(LogEntry entry)
		{
			if (ConsoleWindow.entryWithManagedCallbackDoubleClicked != null)
			{
				ConsoleWindow.entryWithManagedCallbackDoubleClicked(entry);
			}
		}
	}
}
