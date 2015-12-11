﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEditor;
using System.IO;
using AsepriteAnimations.Boomlagoon.JSON;
using UnityEditor.Animations;

namespace AsepriteAnimations
{
	public class AsepriteImporter : EditorWindow
	{
		// ================================================================================
		//  const
		// --------------------------------------------------------------------------------

		private const string ASEPRITE_STANDARD_PATH_WINDOWS = @"C:\Program Files (x86)\Aseprite\Aseprite.exe";
		private const string ASEPRITE_STANDARD_PATH_MACOSX = @"/Applications/Aseprite.app/Contents/MacOS/aseprite";
		private const string PREFS_PREFIX = "IMPORT_ASEPRITE_";

		// ================================================================================
		//  user values
		// --------------------------------------------------------------------------------

		string _asepritePath = "";

		// sprite import values
		private float _spritePixelsPerUnit = 100f;
		private SpriteAlignment _spriteAlignment = SpriteAlignment.BottomCenter;
		private float _spriteAlignmentCustomX = 0;
		private float _spriteAlignmentCustomY = 0;

		private RuntimeAnimatorController _baseController = null;

		private bool _saveSpritesToSubfolder = true;
		private bool _saveAnimationsToSubfolder = true;

		private List<string> _animationNamesThatDoNotLoop = new List<string>() { "death" };

		// ================================================================================
		//  private
		// --------------------------------------------------------------------------------

		private GUIStyle _dropBoxStyle;
		private string _nonLoopingAnimationEnterValue = "";
		private Vector2 _scrollPos = Vector2.zero;

		private bool _hasApplication = false;

		private bool canImportAnimations
		{
			get
			{
				return _hasApplication;
			}
		}

		// ================================================================================
		//  menu entry
		// --------------------------------------------------------------------------------

		[MenuItem("Window/Aseprite Importer")]
		public static void ImportAnimationsMenu()
		{
			EditorWindow.GetWindow(typeof(AsepriteImporter), false, "Aseprite");
        }

		// ================================================================================
		//  unity methods
		// --------------------------------------------------------------------------------

		public void OnEnable()
		{
			LoadUserConfig();
		}

		public void OnGUI()
		{
			if (_dropBoxStyle == null)
				GetBoxStyle();

			_scrollPos = GUILayout.BeginScrollView(_scrollPos);

			EditorGUILayout.Space();
			ShowAnimationsGUI();

			GUILayout.Space(25f);

			ShowAnimatorControllerGUI();

			GUILayout.Space(25f);

			ShowAnimatorOverrideControllerGUI();

			GUILayout.Space(25f);

			ShowUserConfig();

			GUILayout.EndScrollView();
		}

		// ================================================================================
		//  save and load user values
		// --------------------------------------------------------------------------------

		private void LoadUserConfig()
		{
			if (EditorPrefs.HasKey(PREFS_PREFIX + "asepritePath"))
			{
				_asepritePath = EditorPrefs.GetString(PREFS_PREFIX + "asepritePath");
            }
			else
			{
				if (Application.platform == RuntimePlatform.WindowsEditor)
				{
					_asepritePath = ASEPRITE_STANDARD_PATH_WINDOWS;				
                }
				else
				{
					_asepritePath = ASEPRITE_STANDARD_PATH_MACOSX;
				}

				if (!File.Exists(_asepritePath))
					_asepritePath = "";
			}

			if (EditorPrefs.HasKey(PREFS_PREFIX + "spritePixelsPerUnit"))
			{
				_spritePixelsPerUnit = EditorPrefs.GetFloat(PREFS_PREFIX + "spritePixelsPerUnit");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spriteAlignment"))
			{
				_spriteAlignment = (SpriteAlignment)EditorPrefs.GetInt(PREFS_PREFIX + "spriteAlignment");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spriteAlignmentCustomX"))
			{
				_spriteAlignmentCustomX = EditorPrefs.GetFloat(PREFS_PREFIX + "spriteAlignmentCustomX");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spriteAlignmentCustomY"))
			{
				_spriteAlignmentCustomY = EditorPrefs.GetFloat(PREFS_PREFIX + "spriteAlignmentCustomY");
			}

			if (EditorPrefs.HasKey(PREFS_PREFIX + "saveSpritesToSubfolder"))
			{
				_saveSpritesToSubfolder = EditorPrefs.GetBool(PREFS_PREFIX + "saveSpritesToSubfolder");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "saveAnimationsToSubfolder"))
			{
				_saveAnimationsToSubfolder = EditorPrefs.GetBool(PREFS_PREFIX + "saveAnimationsToSubfolder");
			}

			if (EditorPrefs.HasKey(PREFS_PREFIX + "baseControllerPath"))
			{
				string baseControllerPath = EditorPrefs.GetString(PREFS_PREFIX + "baseControllerPath");
				if (!string.IsNullOrEmpty(baseControllerPath))
				{
					_baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(baseControllerPath);
				}
			}

			if (EditorPrefs.HasKey(PREFS_PREFIX + "nonLoopCount"))
			{
				_animationNamesThatDoNotLoop = new List<string>();
				int count = EditorPrefs.GetInt(PREFS_PREFIX + "nonLoopCount");

				for (int i = 0; i < count; i++)
				{
					if (EditorPrefs.HasKey(PREFS_PREFIX + "nonLoopCount" + i.ToString()))
					{
						_animationNamesThatDoNotLoop.Add(EditorPrefs.GetString(PREFS_PREFIX + "nonLoopCount" + i.ToString()));
					}
				}
			}

			CheckIfApplicationIsValid();
        }

		private void SaveUserConfig()
		{
			EditorPrefs.SetString(PREFS_PREFIX + "asepritePath", _asepritePath);

			EditorPrefs.SetFloat(PREFS_PREFIX + "spritePixelsPerUnit", _spritePixelsPerUnit);
			EditorPrefs.SetInt(PREFS_PREFIX + "spriteAlignment", (int)_spriteAlignment);
			EditorPrefs.SetFloat(PREFS_PREFIX + "spriteAlignmentCustomX", _spriteAlignmentCustomX);
			EditorPrefs.SetFloat(PREFS_PREFIX + "spriteAlignmentCustomY", _spriteAlignmentCustomY);

			EditorPrefs.SetBool(PREFS_PREFIX + "saveSpritesToSubfolder", _saveSpritesToSubfolder);
			EditorPrefs.SetBool(PREFS_PREFIX + "saveAnimationsToSubfolder", _saveAnimationsToSubfolder);

			if (_baseController != null)
			{
				EditorPrefs.SetString(PREFS_PREFIX + "baseControllerPath", AssetDatabase.GetAssetPath(_baseController));
			}
			else
			{
				EditorPrefs.SetString(PREFS_PREFIX + "baseControllerPath", "");
			}

			EditorPrefs.SetInt(PREFS_PREFIX + "nonLoopCount", _animationNamesThatDoNotLoop.Count);
			for (int i = 0; i < _animationNamesThatDoNotLoop.Count; i++)
			{
				EditorPrefs.SetString(PREFS_PREFIX + "nonLoopCount" + i.ToString(), _animationNamesThatDoNotLoop[i]);
			}
		}

		// ================================================================================
		//  GUI methods
		// --------------------------------------------------------------------------------

		private void GetBoxStyle()
		{
			_dropBoxStyle = new GUIStyle(EditorStyles.helpBox);
			_dropBoxStyle.alignment = TextAnchor.MiddleCenter;
		}

		private void ShowUserConfig()
		{
			ShowHeadline("Config");

			/*
				Aseprite Application
			*/

			GUILayout.BeginHorizontal();
			GUILayout.Label("Aseprite Application Path");
			string newPath = GUILayout.TextField(_asepritePath);
			if (newPath != _asepritePath)
			{
				_asepritePath = newPath;
				SaveUserConfig();
				CheckIfApplicationIsValid();
			}
            GUILayout.EndHorizontal();

			/*
				sprite values
			*/

			SpriteAlignment newAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Sprite Alignment",_spriteAlignment);
			if (newAlignment != _spriteAlignment)
			{
				_spriteAlignment = newAlignment;
				SaveUserConfig();
			}

			if (_spriteAlignment == SpriteAlignment.Custom)
			{
				float newSpriteAlignmentCustomX = EditorGUILayout.Slider("x", _spriteAlignmentCustomX, 0, 1f);
				float newSpriteAlignmentCustomY = EditorGUILayout.Slider("y", _spriteAlignmentCustomY, 0, 1f);

				if (newSpriteAlignmentCustomX != _spriteAlignmentCustomX || newSpriteAlignmentCustomY != _spriteAlignmentCustomY)
				{
					_spriteAlignmentCustomX = newSpriteAlignmentCustomX;
					_spriteAlignmentCustomY = newSpriteAlignmentCustomY;
					SaveUserConfig();
				}
			}

			float newPixelsPerUnit = EditorGUILayout.FloatField("Sprite Pixels per Unit", _spritePixelsPerUnit);
			if (newPixelsPerUnit != _spritePixelsPerUnit)
			{
				_spritePixelsPerUnit = newPixelsPerUnit;
				SaveUserConfig();
			}

			EditorGUILayout.BeginHorizontal();
			bool saveSpritesToSubfolder = EditorGUILayout.Toggle("Sprites to Subfolder", _saveSpritesToSubfolder);
			if (saveSpritesToSubfolder != _saveSpritesToSubfolder)
			{
				_saveSpritesToSubfolder = saveSpritesToSubfolder;
				SaveUserConfig();
			}

			bool saveAninmationsToSubfolder = EditorGUILayout.Toggle("Animations to Subfolder", _saveAnimationsToSubfolder);
			if (saveAninmationsToSubfolder != _saveAnimationsToSubfolder)
			{
				_saveAnimationsToSubfolder = saveAninmationsToSubfolder;
				SaveUserConfig();
			}
			EditorGUILayout.EndHorizontal();

			/*
				animations that do not loop
			*/

			GUILayout.Space(25f);
			ShowHeadline("Non-looping Animations");

			for (int i = 0; i < _animationNamesThatDoNotLoop.Count; i++)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(_animationNamesThatDoNotLoop[i]);
				bool doDelete = GUILayout.Button("Delete");			
				GUILayout.EndHorizontal();
				if (doDelete)
				{
					_animationNamesThatDoNotLoop.RemoveAt(i);
					SaveUserConfig();
					break;
				}
			}

			EditorGUILayout.Space();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Add ");
			_nonLoopingAnimationEnterValue = EditorGUILayout.TextField(_nonLoopingAnimationEnterValue);
			if (GUILayout.Button("Enter"))
			{
				if (!string.IsNullOrEmpty(_nonLoopingAnimationEnterValue) && !_animationNamesThatDoNotLoop.Contains(_nonLoopingAnimationEnterValue))
				{
					_animationNamesThatDoNotLoop.Add(_nonLoopingAnimationEnterValue);
					_nonLoopingAnimationEnterValue = "";
					SaveUserConfig();
				}
			}
			GUILayout.EndHorizontal();
		}

		private void ShowAnimationsGUI()
		{
			ShowHeadline("Animations");

			DefaultAsset droppedAsset = ShowDropButton<DefaultAsset>(canImportAnimations);
			if (droppedAsset != null)
			{
				CreateAnimationsForAseFile(droppedAsset);
				AssetDatabase.Refresh();
			}
		}

		private void ShowAnimatorControllerGUI()
		{
			ShowHeadline("Animator Controller + Animations");

			DefaultAsset droppedAsset = ShowDropButton<DefaultAsset>(canImportAnimations);
			if (droppedAsset != null)
			{
				var animationInfo = CreateAnimationsForAseFile(droppedAsset);

				if (animationInfo != null)
				{
					CreateAnimatorController(animationInfo);
				}

				AssetDatabase.Refresh();
			}
		}

		private void ShowAnimatorOverrideControllerGUI()
		{
			ShowHeadline("Animator Override Controller + Animations");

			RuntimeAnimatorController newBaseController = EditorGUILayout.ObjectField("Based on Controller:", _baseController, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;
			if (newBaseController != _baseController)
			{
				_baseController = newBaseController;
				SaveUserConfig();
			}

			DefaultAsset droppedAsset = ShowDropButton<DefaultAsset>(canImportAnimations && _baseController != null);
			if (droppedAsset != null)
			{
				if (_baseController == null)
				{
					Debug.LogWarning("Please assign an Animator Controller first");
					return;
				}

				var animationInfo = CreateAnimationsForAseFile(droppedAsset);

				if (animationInfo != null)
				{
					CreateAnimatorOverrideController(animationInfo);
				}

				AssetDatabase.Refresh();
			}
		}

		private void ShowHeadline(string headline)
		{
			EditorGUILayout.LabelField(headline, EditorStyles.boldLabel, GUILayout.Height(20f));
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private void CheckIfApplicationIsValid()
		{
			_hasApplication = File.Exists(_asepritePath);
		}

		private AsepriteAnimationInfo CreateAnimationsForAseFile(DefaultAsset droppedAsset)
		{
			string path = AssetDatabase.GetAssetPath(droppedAsset);
			string name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(droppedAsset));

			string lastPart = "/" + name + ".ase";

			// check if this is an ASE file (HACK!)
			if (!path.Contains(lastPart))
				return null;

			path = path.Replace(lastPart, "");

			if (CreateSpriteAtlasAndMetaFile(path, name))
			{
				AssetDatabase.Refresh();
				return ImportJSONAndCreateAnimations(path, name);
			}

			return null;
        }

		/// <summary>
		/// calls the Aseprite application which then should output a png with all sprites and a corresponding JSON
		/// </summary>
		/// <returns></returns>
		private bool CreateSpriteAtlasAndMetaFile(string path, string name)
		{
			string parameters = "'" + name + ".ase' --data '" + name + ".json' --sheet '" + name + ".png' --sheet-pack --list-tags --format json-array";
			bool success = CallAsepriteCLI(path, parameters) == 0;

			// move png and json file to subfolder
			if (success && _saveSpritesToSubfolder)
			{
				// create subdirectory
				if (!Directory.Exists(path + "/Sprites"))
					Directory.CreateDirectory(path + "/Sprites");

				string target = path + "/Sprites/" + name + ".json";
				if (File.Exists(target))
					File.Delete(target);
                File.Move(path + "/" + name + ".json", target);

				target = path + "/Sprites/" + name + ".png";
				if (File.Exists(target))
					File.Delete(target);
				File.Move(path + "/" + name + ".png", target);
			}

			return success;
		}

		private int CallAsepriteCLI(string path, string buildOptions)
		{
			string workingDirectory = Application.dataPath.Replace("Assets", "") + path;

			// determine application name depending on platform
			string applicationName = _asepritePath;
			if (Application.platform == RuntimePlatform.OSXEditor)
				applicationName = ASEPRITE_STANDARD_PATH_MACOSX;

            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
			start.Arguments = "-b " + buildOptions;
			start.FileName = applicationName;
            start.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			start.CreateNoWindow = true;
			start.UseShellExecute = false;
			start.WorkingDirectory = workingDirectory;

			// Run the external process & wait for it to finish
			using (System.Diagnostics.Process proc = System.Diagnostics.Process.Start(start))
			{
				proc.WaitForExit();
				// Retrieve the app's exit code
				return proc.ExitCode;
			}
		}

		private void CreateAnimatorController(AsepriteAnimationInfo animations)
		{
			AnimatorController controller;

			// check if controller already exists; use this to not loose any references to this in other assets
			string pathForOverrideController = animations.basePath + "/" + animations.name + ".controller";
			controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForOverrideController);

			if (controller == null)
			{
				// create a new controller and place every animation as a state on the first layer
				controller = AnimatorController.CreateAnimatorControllerAtPath(animations.basePath + "/" + animations.name + ".controller");
				controller.AddLayer("Default");

				foreach (var animation in animations.animations)
				{
					AnimatorState state = controller.layers[0].stateMachine.AddState(animation.name);
					state.motion = animation.animationClip;
				}
			}
			else
			{
				// look at all states on the first layer and replace clip if state has the same name
				var childStates = controller.layers[0].stateMachine.states;
				foreach (var childState in childStates)
				{
					AnimationClip clip = animations.GetClip(childState.state.name);
					if (clip != null)
						childState.state.motion = clip;
				}
			}

			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();
		}

		private void CreateAnimatorOverrideController(AsepriteAnimationInfo animations)
		{
			if (_baseController != null)
			{
				AnimatorOverrideController overrideController;

				// check if override controller already exists; use this to not loose any references to this in other assets
				string pathForOverrideController = animations.basePath + "/" + animations.name + ".overrideController";
				overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathForOverrideController);

				if (overrideController == null)
				{
					overrideController = new AnimatorOverrideController();
					AssetDatabase.CreateAsset(overrideController, pathForOverrideController);
				}

				overrideController.runtimeAnimatorController = _baseController;

				// set override clips
				var clipPairs = overrideController.clips;
				for (int i = 0; i < clipPairs.Length; i++)
				{
					string animationName = clipPairs[i].originalClip.name;
					AnimationClip clip = animations.GetClip(animationName);
					clipPairs[i].overrideClip = clip;
				}
				overrideController.clips = clipPairs;

				EditorUtility.SetDirty(overrideController);
			}
			else
			{
				Debug.LogWarning("No Animator Controller found as a base for the Override Controller");
			}
		}

		private AsepriteAnimationInfo ImportJSONAndCreateAnimations(string basePath, string name)
		{
			string imagePath = basePath;
			if (_saveSpritesToSubfolder)
				imagePath += "/Sprites";

			string imageAssetFilename = imagePath + "/" + name + ".png";
			string textAssetFilename = imagePath + "/" + name + ".json";
			TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textAssetFilename);

			if (textAsset != null)
			{
				// parse the JSON file
				JSONObject jsonObject = JSONObject.Parse(textAsset.ToString());
				AsepriteAnimationInfo animations = AsepriteAnimationInfo.GetAnimationInfo(jsonObject);
				animations.basePath = basePath;
				animations.name = name;
				animations.nonLoopingAnimations = _animationNamesThatDoNotLoop;
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(textAsset)); // delete JSON file afterwards

				CreateSprites(imageAssetFilename, animations);
				
				if (_saveAnimationsToSubfolder)
				{
					string path = basePath + "/Animations";
					if (!Directory.Exists(path))
					{
						Directory.CreateDirectory(path);
					}

					CreateAnimations(imageAssetFilename, path, animations);
				}
				else
				{
					CreateAnimations(imageAssetFilename, basePath, animations);

				}

				return animations;
			}
			else
			{
				Debug.LogWarning("Problem with JSON file: " + textAssetFilename);
			}

			return null;
		}

		private void CreateAnimations(string imageAssetFilename, string animationsPath, AsepriteAnimationInfo animationInfo)
		{
			string masterName = Path.GetFileNameWithoutExtension(imageAssetFilename);

			var assets = AssetDatabase.LoadAllAssetsAtPath(imageAssetFilename);
			List<Sprite> sprites = new List<Sprite>();
			foreach (var item in assets)
			{
				if (item is Sprite)
					sprites.Add(item as Sprite);
			}

			foreach (var animation in animationInfo.animations)
			{
				animationInfo.CreateAnimation(animationsPath, masterName, animation, sprites);
			}
		}

		private void CreateSprites(string imageFile, AsepriteAnimationInfo animations)
		{
			TextureImporter importer = AssetImporter.GetAtPath(imageFile) as TextureImporter;

			// adjust values for pixel art
			importer.spriteImportMode = SpriteImportMode.Multiple;
			importer.spritePixelsPerUnit = _spritePixelsPerUnit;
			importer.mipmapEnabled = false;
			importer.filterMode = FilterMode.Point;
			importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			importer.maxTextureSize = animations.maxTextureSize;

			// create sub sprites for this file
			importer.spritesheet = animations.GetSpriteSheet(_spriteAlignment, _spriteAlignmentCustomX, _spriteAlignmentCustomY);

			EditorUtility.SetDirty(importer);
			importer.SaveAndReimport();

			AssetDatabase.ImportAsset(imageFile, ImportAssetOptions.ForceUpdate);
		}

		// ================================================================================
		//  OnGUI helper
		// --------------------------------------------------------------------------------

		private T ShowDropButton<T>(bool isEnabled) where T : UnityEngine.Object
		{
			T returnValue = null;

			Rect drop_area = GUILayoutUtility.GetRect(0.0f, 80.0f, GUILayout.ExpandWidth(true));

			GUI.enabled = isEnabled;
			GUI.Box(drop_area, "Drop Aseprite file here", _dropBoxStyle);
			GUI.enabled = true;

			if (!isEnabled)
				return null;

			Event evt = Event.current;
			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:

					if (!drop_area.Contains(evt.mousePosition)
						|| !DraggedObjectsContainType<T>())
						return null;

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();

						foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
						{
							if (dragged_object is T)
							{
								returnValue = dragged_object as T;
							}
						}
					}

					evt.Use();

					break;
			}

			return returnValue;
		}

		private bool DraggedObjectsContainType<T>() where T : UnityEngine.Object
		{
			foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
			{
				if (dragged_object is T)
				{
					return true;
				}
			}

			return false;
		}
	}
}
