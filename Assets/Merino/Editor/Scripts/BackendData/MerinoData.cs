using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace Merino
{
	public class MerinoData : ScriptableObject
	{
		#region Singleton Behaviour

		private static MerinoData instance;
		public static MerinoData Instance
		{
			get
			{
				if (instance != null)
				{
					return instance;
				}
				
				// attempt to get instance from disk.
				var possibleTempData = AssetDatabase.LoadAssetAtPath<MerinoData>(MerinoPrefs.tempDataPath);
				if (possibleTempData != null && possibleTempData.treeElements != null)
				{
					instance = possibleTempData;
					return instance;
				}
				
				// no instance exists, create a new instance.
				instance = CreateInstance<MerinoData>();
				AssetDatabase.CreateAsset(instance, MerinoPrefs.tempDataPath);
				AssetDatabase.SaveAssets();
				return instance;
			}
		}

		#endregion
	
		[SerializeField] private int editedID = -1;
		internal static int EditedID
		{
			get { return Instance.editedID; }
			set { Instance.editedID = value; }
		}
		
		[SerializeField] private double timestamp;
		internal static double Timestamp
		{
			get { return Instance.timestamp; }
			set { Instance.timestamp = value; }
		}
		
		[HideInInspector] [SerializeField] private TreeViewState viewState = new TreeViewState();
		internal static TreeViewState ViewState
		{
			get { return Instance.viewState; }
			set { Instance.viewState = value; }
		}
		
		[SerializeField] private List<TextAsset> currentFiles = new List<TextAsset>();
		internal static List<TextAsset> CurrentFiles
		{
			get { return Instance.currentFiles; }
			set { Instance.currentFiles = value; }
		}
		
		[SerializeField] private List<TextAsset> dirtyFiles = new List<TextAsset>();
		internal static List<TextAsset> DirtyFiles
		{
			get { return Instance.dirtyFiles; }
			set { Instance.dirtyFiles = value; }
		}

		[SerializeField] private Dictionary<TextAsset, int> fileToNodeID = new Dictionary<TextAsset, int>();
		internal static Dictionary<TextAsset, int> FileToNodeID
		{
			get { return Instance.fileToNodeID; }
			set { Instance.fileToNodeID = value; }
		}

		[SerializeField] private List<MerinoTreeElement> treeElements = new List<MerinoTreeElement>();
		internal static List<MerinoTreeElement> TreeElements
		{
			get { return Instance.treeElements; }
			set { Instance.treeElements = value; }
		}

		public static TextAsset GetDefaultData()
		{
			var defaultData = Resources.Load<TextAsset>(MerinoPrefs.newFileTemplatePath);
			if (defaultData == null)
			{
				MerinoDebug.Log(LoggingLevel.Warning, "Merino couldn't find the new file template at Resources/" + MerinoPrefs.newFileTemplatePath + ". Double-check the file exists there, or you can override this path in EditorPrefs.");
				return null;
			}
			return defaultData;
		}
		
		static bool IsProbablyYarnFile(TextAsset textAsset)
		{
			if ( AssetDatabase.GetAssetPath(textAsset).EndsWith(".yarn.txt") && textAsset.text.Contains("---") && textAsset.text.Contains("===") && textAsset.text.Contains("title:") )
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static TextAsset LoadYarnFileAtFullPath(string path, bool isRelativePath=false)
		{
			var newFile = AssetDatabase.LoadAssetAtPath<TextAsset>( isRelativePath ? path : "Assets" + path.Substring(Application.dataPath.Length) );
			if (MerinoData.CurrentFiles.Contains(newFile) == false)
			{
				MerinoData.CurrentFiles.Add(newFile);
			}
			else
			{
				MerinoDebug.Log(LoggingLevel.Warning, "Merino: file at " + path + " is already loaded!");
			}

			return newFile;
		}
		
		public static TextAsset[] LoadYarnFilesAtPath(string path)
		{
			// use Unity's AssetDatabase search function to find TextAssets, then convert GUIDs to files, and add unique items to currentFiles
			var guids = !string.IsNullOrEmpty(path) ? AssetDatabase.FindAssets("t:TextAsset", new string[] {path} ) : AssetDatabase.FindAssets("t:TextAsset");
			var files = guids.Select(AssetDatabase.GUIDToAssetPath )
				.Where( x => x.Contains("Editor")==false )
				.Select( AssetDatabase.LoadAssetAtPath<TextAsset>)
				.Where( IsProbablyYarnFile )
				.ToArray();
			foreach (var file in files)
			{
				if (MerinoData.CurrentFiles.Contains(file)==false )
				{
					MerinoData.CurrentFiles.Add(file);
				}
			}

			if (files.Length == 0)
			{
				EditorUtility.DisplayDialog("Merino: no Yarn.txt files found", "No valid Yarn.txt files were found at the path " + path, "Close");
			}

			return files;
		}

		#region Util Methods

		internal static MerinoTreeElement GetNode(int id)
		{
			return Instance.treeElements.Find(x => x.depth != -1 && x.id == id);
		}

		internal static MerinoTreeElement GetNode(string name)
		{
			return Instance.treeElements.Find(x => x.depth != -1 && x.name == name);
		}

		#endregion
	}
}
