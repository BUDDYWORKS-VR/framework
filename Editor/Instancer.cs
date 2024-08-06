using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/*
MIT License

Copyright (c) 2022 VRLabs LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace BUDDYWORKS.AvatarFramework.Instancer
{
	public class Instancer : MonoBehaviour
	{
		public static string Instance(string packageName, string installFilePath, string[] excludeRegexs)
		{
			string targetFolder = EditorUtility.OpenFolderPanel("Select Directory To Copy Assets To", "Assets/", "");

			if (targetFolder == "" || targetFolder == null)
			{
				Debug.LogError("No folder selected, please select a folder to copy the assets to.");
				return null;
			}

			if (!targetFolder.Contains(Application.dataPath))
			{
				Debug.LogError("Selected folder is not in the Assets folder, please select a folder in the Assets directory.");
				return null;
			}

			targetFolder = PrepareTargetFolderPath(targetFolder, packageName);
			
			string sourceFolder = GetSourceFolder(installFilePath);

			string[] localAssetPaths = GetLocalAssetPaths(sourceFolder, excludeRegexs);

			CreateDirectories(localAssetPaths, targetFolder);

			CopyFiles(localAssetPaths, sourceFolder, targetFolder);

			AssetDatabase.Refresh();

			FixReferences(localAssetPaths, sourceFolder, targetFolder);
			
			AssetDatabase.Refresh();
			
			return targetFolder;
		}
		
		public static void Install(string packageName, string installFilePath, string[] excludeRegexs, Action<string> callBack)
		{
			string instancePath = Instance(packageName, installFilePath, excludeRegexs);
			callBack(instancePath);
		}

		static string PrepareTargetFolderPath(string folderPath, string packageName)
		{
			folderPath = "Assets" + folderPath.Remove(0, Application.dataPath.Length) + "/" + packageName;

			if (Directory.Exists(folderPath))
			{
				int i = 1;
				while (Directory.Exists(folderPath + i.ToString()))
				{
					i++;
				}
				
				folderPath += i;
			}

			Directory.CreateDirectory(folderPath);
			AssetDatabase.ImportAsset(folderPath);
			return folderPath;
		}
		
		static string GetSourceFolder(string installFilePath)
		{
			string sourceFolder = installFilePath;

			while (!File.Exists(sourceFolder + "/package.json"))
			{
				sourceFolder = Path.GetDirectoryName(sourceFolder);
			}

			return sourceFolder.Replace("\\", "/");
		}

		static string[] GetLocalAssetPaths(string sourceFolder, string[] excludeRegexs)
		{
			string[] assetPaths = AssetDatabase.FindAssets("", new [] { sourceFolder }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

			string[] filteredLocalAssetPaths = assetPaths
				.Select(path => path.Remove(0,sourceFolder.Length))
				.Where(path => excludeRegexs.All(regex => !Regex.Match(path, regex).Success))
				.ToArray();

			return filteredLocalAssetPaths;
		}
		
		static void CreateDirectories(string[] filePaths, string targetFolder)
		{
			try
			{
				AssetDatabase.StartAssetEditing();
				foreach (string path in filePaths)
				{
					string targetPath = Path.GetDirectoryName(targetFolder + path);
					if (!Directory.Exists(targetPath))
					{
						Directory.CreateDirectory(targetPath);
						AssetDatabase.ImportAsset(targetPath);
					}
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}
		}

		static void CopyFiles(string[] filePaths, string sourceFolder, string targetFolder)
		{
			try
			{
				AssetDatabase.StartAssetEditing();
				foreach (string path in filePaths)
				{
					if (!Directory.Exists(sourceFolder + path))
					{
						AssetDatabase.CopyAsset(sourceFolder + path, targetFolder + path);
					}
				}
			}
			finally{
				AssetDatabase.StopAssetEditing();
			}
		}

		static void FixReferences(string[] localAssetPaths, string sourceFolder, string targetFolder)
		{
			foreach (string localAssetPath in localAssetPaths)
			{
				string targetAssetPath = targetFolder + localAssetPath;
				UnityEngine.Object[] targetAssets = AssetDatabase.LoadAllAssetsAtPath(targetAssetPath);
				foreach (var targetAsset in targetAssets)
				{
					SerializedObject serializedObject = new SerializedObject(targetAsset);
					SerializedProperty property = serializedObject.GetIterator();
					do
					{
						if (property.propertyType == SerializedPropertyType.ObjectReference)
						{
							// Debug.Log($"O{property.name}, {property.displayName}, {property.objectReferenceValue}");
							if (property.objectReferenceValue != null)
							{
								property.objectReferenceValue = GetTargetVersion(sourceFolder, targetFolder, property.objectReferenceValue);
							}
						}

						if (property.propertyType == SerializedPropertyType.ExposedReference)
						{
							// Debug.Log($"E{property.name}, {property.displayName}, {property.exposedReferenceValue}");
							if (property.exposedReferenceValue != null)
							{
								property.exposedReferenceValue = GetTargetVersion(sourceFolder, targetFolder, property.exposedReferenceValue);
							}
						}
					} while (property.Next(true));
				
					serializedObject.ApplyModifiedProperties();	
				}
			}
		}

		private static Object GetTargetVersion(string sourceFolder, string targetFolder, Object target)
		{
			string targetPath = AssetDatabase.GetAssetPath(target);
			if (targetPath.StartsWith(sourceFolder))
			{
				string newTargetPath = targetFolder + targetPath.Remove(0, sourceFolder.Length); 
				return AssetDatabase.LoadAllAssetsAtPath(newTargetPath).Where(obj => obj.GetType() == target.GetType()).FirstOrDefault(x => x.name == target.name);
			}

			return target;
		}
	}
}
