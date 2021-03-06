using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
	public sealed class ObjectNames
	{
		[GeneratedByOldBindingsGenerator]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string NicifyVariableName(string name);

		[GeneratedByOldBindingsGenerator]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string GetClassName(UnityEngine.Object obj);

		internal static string GetTypeName(UnityEngine.Object obj)
		{
			string result;
			if (obj == null)
			{
				result = "Object";
			}
			else
			{
				string text = AssetDatabase.GetAssetPath(obj).ToLower();
				if (text.EndsWith(".unity"))
				{
					result = "Scene";
				}
				else if (text.EndsWith(".guiskin"))
				{
					result = "GUI Skin";
				}
				else if (Directory.Exists(AssetDatabase.GetAssetPath(obj)))
				{
					result = "Folder";
				}
				else if (obj.GetType() == typeof(UnityEngine.Object))
				{
					result = Path.GetExtension(text) + " File";
				}
				else
				{
					result = ObjectNames.GetClassName(obj);
				}
			}
			return result;
		}

		[GeneratedByOldBindingsGenerator]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string GetDragAndDropTitle(UnityEngine.Object obj);

		[GeneratedByOldBindingsGenerator]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void SetNameSmart(UnityEngine.Object obj, string name);

		[GeneratedByOldBindingsGenerator]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void SetNameSmartWithInstanceID(int instanceID, string name);

		[Obsolete("Please use NicifyVariableName instead")]
		public static string MangleVariableName(string name)
		{
			return ObjectNames.NicifyVariableName(name);
		}

		[Obsolete("Please use GetInspectorTitle instead")]
		public static string GetPropertyEditorTitle(UnityEngine.Object obj)
		{
			return ObjectNames.GetInspectorTitle(obj);
		}

		[GeneratedByOldBindingsGenerator]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string GetUniqueName(string[] existingNames, string name);

		private static string GetObjectTypeName(UnityEngine.Object o)
		{
			string result;
			if (o == null)
			{
				result = "Nothing Selected";
			}
			else if (o is GameObject)
			{
				result = o.name;
			}
			else if (o is TagManager)
			{
				result = "Tags & Layers";
			}
			else if (o is Component)
			{
				MonoBehaviour monoBehaviour = o as MonoBehaviour;
				if (monoBehaviour)
				{
					string scriptClassName = monoBehaviour.GetScriptClassName();
					if (scriptClassName == "InvalidStateMachineBehaviour")
					{
						result = monoBehaviour.name + " (Script)";
					}
					else
					{
						result = scriptClassName + " (Script)";
					}
				}
				else
				{
					MeshFilter meshFilter = o as MeshFilter;
					if (meshFilter)
					{
						Mesh sharedMesh = meshFilter.sharedMesh;
						result = ((!sharedMesh) ? "[none]" : sharedMesh.name) + " (MeshFilter)";
					}
					else
					{
						result = o.GetType().Name;
					}
				}
			}
			else if (o is AssetImporter)
			{
				MonoImporter monoImporter = o as MonoImporter;
				if (monoImporter)
				{
					MonoScript script = monoImporter.GetScript();
					result = "Default References (" + ((!script) ? string.Empty : script.name) + ")";
				}
				else
				{
					SubstanceImporter substanceImporter = o as SubstanceImporter;
					if (substanceImporter)
					{
						MonoScript substanceArchive = substanceImporter.GetSubstanceArchive();
						if (substanceArchive)
						{
							result = substanceArchive.name + " (Substance Archive)";
							return result;
						}
					}
					result = o.GetType().Name;
				}
			}
			else
			{
				result = o.name + " (" + o.GetType().Name + ")";
			}
			return result;
		}

		public static string GetInspectorTitle(UnityEngine.Object obj)
		{
			string result;
			if (obj == null && obj != null && (obj is MonoBehaviour || obj is ScriptableObject))
			{
				result = " (Script)";
			}
			else if (obj == null)
			{
				result = "Nothing Selected";
			}
			else
			{
				string text = ObjectNames.NicifyVariableName(ObjectNames.GetObjectTypeName(obj));
				if (Attribute.IsDefined(obj.GetType(), typeof(ObsoleteAttribute)))
				{
					text += " (Deprecated)";
				}
				result = text;
			}
			return result;
		}
	}
}
