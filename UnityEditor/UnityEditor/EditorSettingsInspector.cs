using System;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEditor.Hardware;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	[CustomEditor(typeof(EditorSettings))]
	internal class EditorSettingsInspector : ProjectSettingsBaseEditor
	{
		private struct PopupElement
		{
			public readonly string id;

			public readonly bool requiresTeamLicense;

			public readonly GUIContent content;

			public bool Enabled
			{
				get
				{
					return !this.requiresTeamLicense || InternalEditorUtility.HasTeamLicense();
				}
			}

			public PopupElement(string content)
			{
				this = new EditorSettingsInspector.PopupElement(content, false);
			}

			public PopupElement(string content, bool requiresTeamLicense)
			{
				this.id = content;
				this.content = new GUIContent(content);
				this.requiresTeamLicense = requiresTeamLicense;
			}
		}

		private EditorSettingsInspector.PopupElement[] vcDefaultPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement(ExternalVersionControl.Disabled),
			new EditorSettingsInspector.PopupElement(ExternalVersionControl.Generic)
		};

		private EditorSettingsInspector.PopupElement[] vcPopupList = null;

		private EditorSettingsInspector.PopupElement[] serializationPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Mixed"),
			new EditorSettingsInspector.PopupElement("Force Binary"),
			new EditorSettingsInspector.PopupElement("Force Text")
		};

		private EditorSettingsInspector.PopupElement[] behaviorPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("3D"),
			new EditorSettingsInspector.PopupElement("2D")
		};

		private EditorSettingsInspector.PopupElement[] spritePackerPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Disabled"),
			new EditorSettingsInspector.PopupElement("Enabled For Builds(Legacy Sprite Packer)"),
			new EditorSettingsInspector.PopupElement("Always Enabled(Legacy Sprite Packer)"),
			new EditorSettingsInspector.PopupElement("Enabled For Builds"),
			new EditorSettingsInspector.PopupElement("Always Enabled")
		};

		private EditorSettingsInspector.PopupElement[] lineEndingsPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("OS Native"),
			new EditorSettingsInspector.PopupElement("Unix"),
			new EditorSettingsInspector.PopupElement("Windows")
		};

		private EditorSettingsInspector.PopupElement[] spritePackerPaddingPowerPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("1"),
			new EditorSettingsInspector.PopupElement("2"),
			new EditorSettingsInspector.PopupElement("3")
		};

		private EditorSettingsInspector.PopupElement[] remoteDevicePopupList;

		private DevDevice[] remoteDeviceList;

		private EditorSettingsInspector.PopupElement[] remoteCompressionList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("JPEG"),
			new EditorSettingsInspector.PopupElement("PNG")
		};

		private EditorSettingsInspector.PopupElement[] remoteResolutionList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Downsize"),
			new EditorSettingsInspector.PopupElement("Normal")
		};

		private EditorSettingsInspector.PopupElement[] remoteJoystickSourceList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Remote"),
			new EditorSettingsInspector.PopupElement("Local")
		};

		private string[] logLevelPopupList = new string[]
		{
			"Verbose",
			"Info",
			"Notice",
			"Fatal"
		};

		private string[] semanticMergePopupList = new string[]
		{
			"Off",
			"Premerge",
			"Ask"
		};

		private EditorSettingsInspector.PopupElement[] etcTextureCompressorPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Legacy"),
			new EditorSettingsInspector.PopupElement("Default"),
			new EditorSettingsInspector.PopupElement("Custom")
		};

		private EditorSettingsInspector.PopupElement[] etcTextureFastCompressorPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("etcpak"),
			new EditorSettingsInspector.PopupElement("ETCPACK Fast")
		};

		private EditorSettingsInspector.PopupElement[] etcTextureNormalCompressorPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("etcpak"),
			new EditorSettingsInspector.PopupElement("ETCPACK Fast"),
			new EditorSettingsInspector.PopupElement("Etc2Comp Fast"),
			new EditorSettingsInspector.PopupElement("Etc2Comp Best")
		};

		private EditorSettingsInspector.PopupElement[] etcTextureBestCompressorPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Etc2Comp Fast"),
			new EditorSettingsInspector.PopupElement("Etc2Comp Best"),
			new EditorSettingsInspector.PopupElement("ETCPACK Best")
		};

		public void OnEnable()
		{
			Plugin[] availablePlugins = Plugin.availablePlugins;
			List<EditorSettingsInspector.PopupElement> list = new List<EditorSettingsInspector.PopupElement>(this.vcDefaultPopupList);
			Plugin[] array = availablePlugins;
			for (int i = 0; i < array.Length; i++)
			{
				Plugin plugin = array[i];
				list.Add(new EditorSettingsInspector.PopupElement(plugin.name, true));
			}
			this.vcPopupList = list.ToArray();
			DevDeviceList.Changed += new DevDeviceList.OnChangedHandler(this.OnDeviceListChanged);
			this.BuildRemoteDeviceList();
		}

		public void OnDisable()
		{
			DevDeviceList.Changed -= new DevDeviceList.OnChangedHandler(this.OnDeviceListChanged);
		}

		private void OnDeviceListChanged()
		{
			this.BuildRemoteDeviceList();
		}

		private void BuildRemoteDeviceList()
		{
			List<DevDevice> list = new List<DevDevice>();
			List<EditorSettingsInspector.PopupElement> list2 = new List<EditorSettingsInspector.PopupElement>();
			list.Add(DevDevice.none);
			list2.Add(new EditorSettingsInspector.PopupElement("None"));
			list.Add(new DevDevice("Any Android Device", "Any Android Device", "virtual", "Android", DevDeviceState.Connected, DevDeviceFeatures.RemoteConnection));
			list2.Add(new EditorSettingsInspector.PopupElement("Any Android Device"));
			DevDevice[] devices = DevDeviceList.GetDevices();
			for (int i = 0; i < devices.Length; i++)
			{
				DevDevice item = devices[i];
				bool flag = (item.features & DevDeviceFeatures.RemoteConnection) != DevDeviceFeatures.None;
				if (item.isConnected && flag)
				{
					list.Add(item);
					list2.Add(new EditorSettingsInspector.PopupElement(item.name));
				}
			}
			this.remoteDeviceList = list.ToArray();
			this.remoteDevicePopupList = list2.ToArray();
		}

		public override void OnInspectorGUI()
		{
			bool enabled = GUI.enabled;
			this.ShowUnityRemoteGUI(enabled);
			GUILayout.Space(10f);
			bool flag = Collab.instance.IsCollabEnabledForCurrentProject();
			using (new EditorGUI.DisabledScope(!flag))
			{
				GUI.enabled = !flag;
				GUILayout.Label("Version Control", EditorStyles.boldLabel, new GUILayoutOption[0]);
				ExternalVersionControl d = EditorSettings.externalVersionControl;
				this.CreatePopupMenuVersionControl("Mode", this.vcPopupList, d, new GenericMenu.MenuFunction2(this.SetVersionControlSystem));
				GUI.enabled = (enabled && !flag);
			}
			if (flag)
			{
				EditorGUILayout.HelpBox("Version Control not available when using Collaboration feature.", MessageType.Warning);
			}
			if (this.VersionControlSystemHasGUI())
			{
				GUI.enabled = true;
				bool flag2 = false;
				if (!(EditorSettings.externalVersionControl == ExternalVersionControl.Generic) && !(EditorSettings.externalVersionControl == ExternalVersionControl.Disabled))
				{
					ConfigField[] activeConfigFields = Provider.GetActiveConfigFields();
					flag2 = true;
					ConfigField[] array = activeConfigFields;
					for (int i = 0; i < array.Length; i++)
					{
						ConfigField configField = array[i];
						string configValue = EditorUserSettings.GetConfigValue(configField.name);
						string text;
						if (configField.isPassword)
						{
							text = EditorGUILayout.PasswordField(configField.label, configValue, new GUILayoutOption[0]);
							if (text != configValue)
							{
								EditorUserSettings.SetPrivateConfigValue(configField.name, text);
							}
						}
						else
						{
							text = EditorGUILayout.TextField(configField.label, configValue, new GUILayoutOption[0]);
							if (text != configValue)
							{
								EditorUserSettings.SetConfigValue(configField.name, text);
							}
						}
						if (configField.isRequired && string.IsNullOrEmpty(text))
						{
							flag2 = false;
						}
					}
				}
				string logLevel = EditorUserSettings.GetConfigValue("vcSharedLogLevel");
				int num = Array.FindIndex<string>(this.logLevelPopupList, (string item) => item.ToLower() == logLevel);
				if (num == -1)
				{
					logLevel = "notice";
					num = Array.FindIndex<string>(this.logLevelPopupList, (string item) => item.ToLower() == logLevel);
					if (num == -1)
					{
						num = 0;
					}
					logLevel = this.logLevelPopupList[num];
					EditorUserSettings.SetConfigValue("vcSharedLogLevel", logLevel);
				}
				int num2 = EditorGUILayout.Popup("Log Level", num, this.logLevelPopupList, new GUILayoutOption[0]);
				if (num2 != num)
				{
					EditorUserSettings.SetConfigValue("vcSharedLogLevel", this.logLevelPopupList[num2].ToLower());
				}
				GUI.enabled = enabled;
				string label = "Connected";
				if (Provider.onlineState == OnlineState.Updating)
				{
					label = "Connecting...";
				}
				else if (Provider.onlineState == OnlineState.Offline)
				{
					label = "Disconnected";
				}
				EditorGUILayout.LabelField("Status", label, new GUILayoutOption[0]);
				if (Provider.onlineState != OnlineState.Online && !string.IsNullOrEmpty(Provider.offlineReason))
				{
					GUI.enabled = false;
					GUILayout.TextArea(Provider.offlineReason, new GUILayoutOption[0]);
					GUI.enabled = enabled;
				}
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				GUI.enabled = (flag2 && Provider.onlineState != OnlineState.Updating);
				if (GUILayout.Button("Connect", EditorStyles.miniButton, new GUILayoutOption[0]))
				{
					Provider.UpdateSettings();
				}
				GUILayout.EndHorizontal();
				EditorUserSettings.AutomaticAdd = EditorGUILayout.Toggle("Automatic add", EditorUserSettings.AutomaticAdd, new GUILayoutOption[0]);
				if (Provider.requiresNetwork)
				{
					bool flag3 = EditorGUILayout.Toggle("Work Offline", EditorUserSettings.WorkOffline, new GUILayoutOption[0]);
					if (flag3 != EditorUserSettings.WorkOffline)
					{
						if (flag3 && !EditorUtility.DisplayDialog("Confirm working offline", "Working offline and making changes to your assets means that you will have to manually integrate changes back into version control using your standard version control client before you stop working offline in Unity. Make sure you know what you are doing.", "Work offline", "Cancel"))
						{
							flag3 = false;
						}
						EditorUserSettings.WorkOffline = flag3;
						EditorApplication.RequestRepaintAllViews();
					}
					EditorUserSettings.allowAsyncStatusUpdate = EditorGUILayout.Toggle("Allow Async Update", EditorUserSettings.allowAsyncStatusUpdate, new GUILayoutOption[0]);
				}
				if (Provider.hasCheckoutSupport)
				{
					EditorUserSettings.showFailedCheckout = EditorGUILayout.Toggle("Show failed checkouts", EditorUserSettings.showFailedCheckout, new GUILayoutOption[0]);
				}
				GUI.enabled = enabled;
				EditorUserSettings.semanticMergeMode = (SemanticMergeMode)EditorGUILayout.Popup("Smart merge", (int)EditorUserSettings.semanticMergeMode, this.semanticMergePopupList, new GUILayoutOption[0]);
				this.DrawOverlayDescriptions();
			}
			GUILayout.Space(10f);
			int selectedIndex = (int)EditorSettings.serializationMode;
			using (new EditorGUI.DisabledScope(!flag))
			{
				GUI.enabled = !flag;
				GUILayout.Label("Asset Serialization", EditorStyles.boldLabel, new GUILayoutOption[0]);
				GUI.enabled = (enabled && !flag);
				this.CreatePopupMenu("Mode", this.serializationPopupList, selectedIndex, new GenericMenu.MenuFunction2(this.SetAssetSerializationMode));
			}
			if (flag)
			{
				EditorGUILayout.HelpBox("Asset Serialization is forced to Text when using Collaboration feature.", MessageType.Warning);
			}
			GUILayout.Space(10f);
			GUI.enabled = true;
			GUILayout.Label("Default Behavior Mode", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = enabled;
			selectedIndex = Mathf.Clamp((int)EditorSettings.defaultBehaviorMode, 0, this.behaviorPopupList.Length - 1);
			this.CreatePopupMenu("Mode", this.behaviorPopupList, selectedIndex, new GenericMenu.MenuFunction2(this.SetDefaultBehaviorMode));
			GUILayout.Space(10f);
			GUI.enabled = true;
			GUILayout.Label("Sprite Packer", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = enabled;
			selectedIndex = Mathf.Clamp((int)EditorSettings.spritePackerMode, 0, this.spritePackerPopupList.Length - 1);
			this.CreatePopupMenu("Mode", this.spritePackerPopupList, selectedIndex, new GenericMenu.MenuFunction2(this.SetSpritePackerMode));
			if (EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOn || EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnly)
			{
				selectedIndex = Mathf.Clamp(EditorSettings.spritePackerPaddingPower - 1, 0, 2);
				this.CreatePopupMenu("Padding Power (Legacy Sprite Packer)", this.spritePackerPaddingPowerPopupList, selectedIndex, new GenericMenu.MenuFunction2(this.SetSpritePackerPaddingPower));
			}
			this.DoProjectGenerationSettings();
			this.DoEtcTextureCompressionSettings();
			this.DoInternalSettings();
			this.DoLineEndingsSettings();
		}

		private void DoProjectGenerationSettings()
		{
			GUILayout.Space(10f);
			GUILayout.Label("C# Project Generation", EditorStyles.boldLabel, new GUILayoutOption[0]);
			string text = EditorSettings.Internal_ProjectGenerationUserExtensions;
			string text2 = EditorGUILayout.TextField("Additional extensions to include", text, new GUILayoutOption[0]);
			if (text2 != text)
			{
				EditorSettings.Internal_ProjectGenerationUserExtensions = text2;
			}
			text = EditorSettings.projectGenerationRootNamespace;
			text2 = EditorGUILayout.TextField("Root namespace", text, new GUILayoutOption[0]);
			if (text2 != text)
			{
				EditorSettings.projectGenerationRootNamespace = text2;
			}
		}

		private void DoInternalSettings()
		{
			if (EditorPrefs.GetBool("DeveloperMode", false))
			{
				GUILayout.Space(10f);
				GUILayout.Label("Internal Settings", EditorStyles.boldLabel, new GUILayoutOption[0]);
				string text = "-testable";
				EditorGUI.BeginChangeCheck();
				bool flag = EditorSettings.Internal_UserGeneratedProjectSuffix == text;
				flag = EditorGUILayout.Toggle("Internals visible in user scripts", flag, new GUILayoutOption[0]);
				if (EditorGUI.EndChangeCheck())
				{
					EditorSettings.Internal_UserGeneratedProjectSuffix = ((!flag) ? "" : text);
				}
				if (flag)
				{
					EditorGUILayout.HelpBox("If you want this to be set for other people, remember to manually add ProjectSettings/EditorSettings.asset to the repository", MessageType.Info);
				}
			}
		}

		private void DoEtcTextureCompressionSettings()
		{
			GUILayout.Space(10f);
			GUILayout.Label("ETC Texture Compressor", EditorStyles.boldLabel, new GUILayoutOption[0]);
			int num = Mathf.Clamp(EditorSettings.etcTextureCompressorBehavior, 0, this.etcTextureCompressorPopupList.Length - 1);
			this.CreatePopupMenu("Behavior", this.etcTextureCompressorPopupList, num, new GenericMenu.MenuFunction2(this.SetEtcTextureCompressorBehavior));
			EditorGUI.indentLevel++;
			EditorGUI.BeginDisabledGroup(num < 2);
			num = Mathf.Clamp(EditorSettings.etcTextureFastCompressor, 0, this.etcTextureFastCompressorPopupList.Length - 1);
			this.CreatePopupMenu("Fast", this.etcTextureFastCompressorPopupList, num, new GenericMenu.MenuFunction2(this.SetEtcTextureFastCompressor));
			num = Mathf.Clamp(EditorSettings.etcTextureNormalCompressor, 0, this.etcTextureNormalCompressorPopupList.Length - 1);
			this.CreatePopupMenu("Normal", this.etcTextureNormalCompressorPopupList, num, new GenericMenu.MenuFunction2(this.SetEtcTextureNormalCompressor));
			num = Mathf.Clamp(EditorSettings.etcTextureBestCompressor, 0, this.etcTextureBestCompressorPopupList.Length - 1);
			this.CreatePopupMenu("Best", this.etcTextureBestCompressorPopupList, num, new GenericMenu.MenuFunction2(this.SetEtcTextureBestCompressor));
			EditorGUI.EndDisabledGroup();
			EditorGUI.indentLevel--;
		}

		private void DoLineEndingsSettings()
		{
			GUILayout.Space(10f);
			GUILayout.Label("Line Endings For New Scripts", EditorStyles.boldLabel, new GUILayoutOption[0]);
			int lineEndingsForNewScripts = (int)EditorSettings.lineEndingsForNewScripts;
			this.CreatePopupMenu("Mode", this.lineEndingsPopupList, lineEndingsForNewScripts, new GenericMenu.MenuFunction2(this.SetLineEndingsForNewScripts));
		}

		private static int GetIndexById(DevDevice[] elements, string id, int defaultIndex)
		{
			int result;
			for (int i = 0; i < elements.Length; i++)
			{
				if (elements[i].id == id)
				{
					result = i;
					return result;
				}
			}
			result = defaultIndex;
			return result;
		}

		private static int GetIndexById(EditorSettingsInspector.PopupElement[] elements, string id, int defaultIndex)
		{
			int result;
			for (int i = 0; i < elements.Length; i++)
			{
				if (elements[i].id == id)
				{
					result = i;
					return result;
				}
			}
			result = defaultIndex;
			return result;
		}

		private void ShowUnityRemoteGUI(bool editorEnabled)
		{
			GUI.enabled = true;
			GUILayout.Label("Unity Remote", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = editorEnabled;
			string unityRemoteDevice = EditorSettings.unityRemoteDevice;
			int indexById = EditorSettingsInspector.GetIndexById(this.remoteDeviceList, unityRemoteDevice, 0);
			GUIContent content = new GUIContent(this.remoteDevicePopupList[indexById].content);
			Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, EditorGUIUtility.TrTextContent("Device", null, null));
			if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.remoteDevicePopupList, indexById, new GenericMenu.MenuFunction2(this.SetUnityRemoteDevice));
			}
			int indexById2 = EditorSettingsInspector.GetIndexById(this.remoteCompressionList, EditorSettings.unityRemoteCompression, 0);
			content = new GUIContent(this.remoteCompressionList[indexById2].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, EditorGUIUtility.TrTextContent("Compression", null, null));
			if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.remoteCompressionList, indexById2, new GenericMenu.MenuFunction2(this.SetUnityRemoteCompression));
			}
			int indexById3 = EditorSettingsInspector.GetIndexById(this.remoteResolutionList, EditorSettings.unityRemoteResolution, 0);
			content = new GUIContent(this.remoteResolutionList[indexById3].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, EditorGUIUtility.TrTextContent("Resolution", null, null));
			if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.remoteResolutionList, indexById3, new GenericMenu.MenuFunction2(this.SetUnityRemoteResolution));
			}
			int indexById4 = EditorSettingsInspector.GetIndexById(this.remoteJoystickSourceList, EditorSettings.unityRemoteJoystickSource, 0);
			content = new GUIContent(this.remoteJoystickSourceList[indexById4].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, EditorGUIUtility.TrTextContent("Joystick Source", null, null));
			if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.remoteJoystickSourceList, indexById4, new GenericMenu.MenuFunction2(this.SetUnityRemoteJoystickSource));
			}
		}

		private void DrawOverlayDescriptions()
		{
			Texture2D overlayAtlas = Provider.overlayAtlas;
			if (!(overlayAtlas == null))
			{
				GUILayout.Space(10f);
				GUILayout.Label("Overlay legends", EditorStyles.boldLabel, new GUILayoutOption[0]);
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.BeginVertical(new GUILayoutOption[0]);
				this.DrawOverlayDescription(Asset.States.Local);
				this.DrawOverlayDescription(Asset.States.OutOfSync);
				this.DrawOverlayDescription(Asset.States.CheckedOutLocal);
				this.DrawOverlayDescription(Asset.States.CheckedOutRemote);
				this.DrawOverlayDescription(Asset.States.DeletedLocal);
				this.DrawOverlayDescription(Asset.States.DeletedRemote);
				GUILayout.EndVertical();
				GUILayout.BeginVertical(new GUILayoutOption[0]);
				this.DrawOverlayDescription(Asset.States.AddedLocal);
				this.DrawOverlayDescription(Asset.States.AddedRemote);
				this.DrawOverlayDescription(Asset.States.Conflicted);
				this.DrawOverlayDescription(Asset.States.LockedLocal);
				this.DrawOverlayDescription(Asset.States.LockedRemote);
				this.DrawOverlayDescription(Asset.States.Updating);
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
		}

		private void DrawOverlayDescription(Asset.States state)
		{
			Rect atlasRectForState = Provider.GetAtlasRectForState((int)state);
			if (atlasRectForState.width != 0f)
			{
				Texture2D overlayAtlas = Provider.overlayAtlas;
				if (!(overlayAtlas == null))
				{
					GUILayout.Label("    " + Asset.StateToString(state), EditorStyles.miniLabel, new GUILayoutOption[0]);
					Rect lastRect = GUILayoutUtility.GetLastRect();
					lastRect.width = 16f;
					GUI.DrawTextureWithTexCoords(lastRect, overlayAtlas, atlasRectForState);
				}
			}
		}

		private void CreatePopupMenuVersionControl(string title, EditorSettingsInspector.PopupElement[] elements, string selectedValue, GenericMenu.MenuFunction2 func)
		{
			int num = Array.FindIndex<EditorSettingsInspector.PopupElement>(elements, (EditorSettingsInspector.PopupElement typeElem) => typeElem.id == selectedValue);
			GUIContent content = new GUIContent(elements[num].content);
			this.CreatePopupMenu(title, content, elements, num, func);
		}

		private void CreatePopupMenu(string title, EditorSettingsInspector.PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
		{
			this.CreatePopupMenu(title, elements[selectedIndex].content, elements, selectedIndex, func);
		}

		private void CreatePopupMenu(string title, GUIContent content, EditorSettingsInspector.PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
		{
			Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent(title));
			if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, elements, selectedIndex, func);
			}
		}

		private void DoPopup(Rect popupRect, EditorSettingsInspector.PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
		{
			GenericMenu genericMenu = new GenericMenu();
			for (int i = 0; i < elements.Length; i++)
			{
				EditorSettingsInspector.PopupElement popupElement = elements[i];
				if (popupElement.Enabled)
				{
					genericMenu.AddItem(popupElement.content, i == selectedIndex, func, i);
				}
				else
				{
					genericMenu.AddDisabledItem(popupElement.content);
				}
			}
			genericMenu.DropDown(popupRect);
		}

		private bool VersionControlSystemHasGUI()
		{
			bool result;
			if (!Collab.instance.IsCollabEnabledForCurrentProject())
			{
				ExternalVersionControl d = EditorSettings.externalVersionControl;
				result = (d != ExternalVersionControl.Disabled && d != ExternalVersionControl.AutoDetect && d != ExternalVersionControl.Generic);
			}
			else
			{
				result = false;
			}
			return result;
		}

		private void SetVersionControlSystem(object data)
		{
			int num = (int)data;
			if (num >= 0 || num < this.vcPopupList.Length)
			{
				EditorSettingsInspector.PopupElement popupElement = this.vcPopupList[num];
				string externalVersionControl = EditorSettings.externalVersionControl;
				EditorSettings.externalVersionControl = popupElement.id;
				Provider.UpdateSettings();
				AssetDatabase.Refresh();
				if (externalVersionControl != popupElement.id)
				{
					if (popupElement.content.text == ExternalVersionControl.Disabled || popupElement.content.text == ExternalVersionControl.Generic)
					{
						WindowPending.CloseAllWindows();
					}
				}
			}
		}

		private void SetAssetSerializationMode(object data)
		{
			int serializationMode = (int)data;
			EditorSettings.serializationMode = (SerializationMode)serializationMode;
		}

		private void SetUnityRemoteDevice(object data)
		{
			EditorSettings.unityRemoteDevice = this.remoteDeviceList[(int)data].id;
		}

		private void SetUnityRemoteCompression(object data)
		{
			EditorSettings.unityRemoteCompression = this.remoteCompressionList[(int)data].id;
		}

		private void SetUnityRemoteResolution(object data)
		{
			EditorSettings.unityRemoteResolution = this.remoteResolutionList[(int)data].id;
		}

		private void SetUnityRemoteJoystickSource(object data)
		{
			EditorSettings.unityRemoteJoystickSource = this.remoteJoystickSourceList[(int)data].id;
		}

		private void SetDefaultBehaviorMode(object data)
		{
			int defaultBehaviorMode = (int)data;
			EditorSettings.defaultBehaviorMode = (EditorBehaviorMode)defaultBehaviorMode;
		}

		private void SetSpritePackerMode(object data)
		{
			int spritePackerMode = (int)data;
			EditorSettings.spritePackerMode = (SpritePackerMode)spritePackerMode;
		}

		private void SetSpritePackerPaddingPower(object data)
		{
			int num = (int)data;
			EditorSettings.spritePackerPaddingPower = num + 1;
		}

		private void SetEtcTextureCompressorBehavior(object data)
		{
			int num = (int)data;
			if (EditorSettings.etcTextureCompressorBehavior != num)
			{
				EditorSettings.etcTextureCompressorBehavior = num;
				if (num == 0)
				{
					EditorSettings.SetEtcTextureCompressorLegacyBehavior();
				}
				else
				{
					EditorSettings.SetEtcTextureCompressorDefaultBehavior();
				}
			}
		}

		private void SetEtcTextureFastCompressor(object data)
		{
			EditorSettings.etcTextureFastCompressor = (int)data;
		}

		private void SetEtcTextureNormalCompressor(object data)
		{
			EditorSettings.etcTextureNormalCompressor = (int)data;
		}

		private void SetEtcTextureBestCompressor(object data)
		{
			EditorSettings.etcTextureBestCompressor = (int)data;
		}

		private void SetLineEndingsForNewScripts(object data)
		{
			int lineEndingsForNewScripts = (int)data;
			EditorSettings.lineEndingsForNewScripts = (LineEndingsMode)lineEndingsForNewScripts;
		}
	}
}
