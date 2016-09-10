﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace DragonBone
{
	/// <summary>
	/// Armature editor.
	/// author:  bingheliefeng
	/// </summary>
	public class ArmatureEditor : ScriptableWizard {
		[System.Serializable]
		public class Atlas{
			public Texture2D texture;
			public TextAsset atlasText;
		}

		[Header("Settting")]
		public float zoffset = 0.003f;
		public bool useUnitySprite = false; //Is used Unity Sprite2D
		public bool isSingleSprite = false;//is atlas textrue

		[Header("Anim File")]
		public TextAsset animTextAsset;
		public bool createAvatar = false;//generate Avatar and Avatar Mask

		[Header("TextureAtlas")]
		public Texture2D altasTexture;
		public TextAsset altasTextAsset;
		public Atlas[] otherTextures;

		private DragonBoneData.ArmatureData _armatureData;
		public DragonBoneData.ArmatureData armatureData{ 
			get{return _armatureData;} 
			set{_armatureData=value;}
		}

		private Transform _armature;
		public Transform armature{ 
			get{ return _armature;}
			set{_armature = value;}
		}

		public Atlas GetAtlasByTextureName(string textureName){
			if(atlasKV.ContainsKey(textureName)){
				return atlasKV[textureName];
			}
			return null;
		}

		public Dictionary<string,Transform> bonesKV = new Dictionary<string, Transform>();
		public Dictionary<string,DragonBoneData.BoneData> bonesDataKV = new Dictionary<string, DragonBoneData.BoneData>();
		public Dictionary<string,Transform> slotsKV = new Dictionary<string, Transform>();
		public Dictionary<string,DragonBoneData.SlotData> slotsDataKV = new Dictionary<string, DragonBoneData.SlotData>();
		public Dictionary<string,Sprite> spriteKV = new Dictionary<string, Sprite>();//single sprite

		public Dictionary<string , Atlas> atlasKV = new Dictionary<string, Atlas>();
		public Dictionary<string,Matrix2D> bonePoseKV = new Dictionary<string, Matrix2D>() ; //bonePose , key is bone name
		public Dictionary<string,bool> ffdKV = new Dictionary<string, bool>();//skinnedMesh animation or ffd animation, key is skin name/texture name

		[HideInInspector]
		public List<Transform> bones = new List<Transform>();

		[MenuItem("DragonBone/DragonBone Panel (All Function)")]
		static void CreateWizard () {
			ScriptableWizard.DisplayWizard<ArmatureEditor>("Create DragonBone", "Create");
		}

		[MenuItem("DragonBone/DragonBone (SpriteFrame)")]
		static void CreateDragbonBoneByDir_SpriteFrame()
		{
			CreateDragonBoneByDir(false);
		}
		[MenuItem("DragonBone/DragonBone (UnitySprite)")]
		static void CreateDragbonBoneByDir_UnitySprite()
		{
			CreateDragonBoneByDir(true);
		}

		static void CreateDragonBoneByDir(bool useUnitySprite){
			if(Selection.activeObject is DefaultAsset)
			{
				string dirPath = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);
				if(Directory.Exists(dirPath)){
					string animJsonPath=null,texturePath=null,textureJsonPath=null;
					foreach (string path in Directory.GetFiles(dirPath))  
					{  
						if(path.IndexOf("texture.json")>-1 && path.LastIndexOf(".meta")==-1 && System.IO.Path.GetExtension(path) == ".json"){
							textureJsonPath = path;
							continue;
						}
						if(path.IndexOf("texture")>-1 && path.LastIndexOf(".meta")==-1 && System.IO.Path.GetExtension(path) == ".png" ){
							texturePath = path;
							continue;
						}
						if (path.IndexOf("texture.json")==-1 && System.IO.Path.GetExtension(path) == ".json" && path.LastIndexOf(".meta")==-1)  
						{  
							animJsonPath = path;
						}
					} 
					if(!string.IsNullOrEmpty(animJsonPath) && !string.IsNullOrEmpty(texturePath) && !string.IsNullOrEmpty(textureJsonPath)){
						ArmatureEditor instance  = ScriptableObject.CreateInstance<ArmatureEditor>();
						instance.useUnitySprite = useUnitySprite;
						instance.altasTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textureJsonPath);
						instance.altasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
						instance.animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(animJsonPath);
						if(instance.altasTexture&&instance.altasTextAsset&&instance.animTextAsset){
							instance.OnWizardCreate();
						}
						DestroyImmediate(instance);
					}
					else if(useUnitySprite && !string.IsNullOrEmpty(animJsonPath))
					{
						string spritesPath = null;
						foreach (string path in Directory.GetDirectories(dirPath))  
						{  
							if(path.LastIndexOf("texture")>-1){
								spritesPath = path;
								break;
							}
						}
						if(!string.IsNullOrEmpty(spritesPath)){

							Dictionary<string,Sprite> spriteKV = new Dictionary<string, Sprite>();
							foreach (string path in Directory.GetFiles(spritesPath))  
							{  
								if(path.LastIndexOf(".png")>-1 && path.LastIndexOf(".meta")==-1 ){
									Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
									spriteKV[sprite.name]=sprite;
								}
							}
							if(spriteKV.Count>0){
								ArmatureEditor instance  = ScriptableObject.CreateInstance<ArmatureEditor>();
								instance.useUnitySprite = true;
								instance.isSingleSprite = true;
								instance.spriteKV = spriteKV;
								instance.animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(animJsonPath);
								instance.OnWizardCreate();
								DestroyImmediate(instance);
							}
						}
					}
				}
			}
		}

		public void SetAtlasTextureImporter(string atlasPath){
			TextureImporter textureImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
			textureImporter.maxTextureSize = 2048;
			AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
		}


		public void OnWizardCreate(){
			if(isSingleSprite){
				useUnitySprite = true;
			}
			else{
				if(animTextAsset==null || altasTexture==null || altasTextAsset==null){
					return;
				}
			}

			if(useUnitySprite && isSingleSprite && spriteKV.Count==0){
				string spritesPath = AssetDatabase.GetAssetPath(animTextAsset);
				spritesPath = spritesPath.Substring(0,spritesPath.LastIndexOf('.'));
				spritesPath = spritesPath.Substring(0,spritesPath.LastIndexOf('/'))+"/texture";
				if(Directory.Exists(spritesPath))
				{
					foreach (string path in Directory.GetFiles(spritesPath))  
					{  
						if(path.LastIndexOf(".png")>-1 && path.LastIndexOf(".meta")==-1 ){
							Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
							spriteKV[sprite.name]=sprite;
						}
					}
				}
				else
				{
					return;
				}
			}
			if(altasTexture && altasTextAsset){
				SetAtlasTextureImporter(AssetDatabase.GetAssetPath(altasTexture));
				JsonParse.ParseTextureAtlas(this,altasTexture,altasTextAsset);
			}
			if(otherTextures!=null){
				foreach(Atlas atlas in otherTextures){
					SetAtlasTextureImporter(AssetDatabase.GetAssetPath(atlas.texture));
					JsonParse.ParseTextureAtlas(this,atlas.texture,atlas.atlasText);
				}
			}
			JsonParse.ParseAnimJsonData(this);
		}

		void OnWizardUpdate() {
			helpString = "Just only need anim file if does not use the atlas.";
		}

		//init
		public void InitShow(){
			ShowArmature.AddBones(this);
			ShowArmature.AddSlot(this);
			ShowArmature.ShowBones(this);
			ShowArmature.ShowSlots(this);
			ShowArmature.ShowSkins(this);
			ShowArmature.SetIKs(this);
			AnimFile.CreateAnimFile(this);
			DragonBoneArmature dba = _armature.GetComponent<DragonBoneArmature>();
			if(dba && (dba.updateMeshs == null || dba.updateMeshs.Length==0) 
				&& (dba.updateFrames==null || dba.updateFrames.Length==0)){
				Object.DestroyImmediate(dba);
			}

			Renderer[] renders = _armature.GetComponentsInChildren<Renderer>();
			foreach(Renderer r in renders){
				if(!r.enabled){
					r.enabled = true;
					r.gameObject.SetActive(false);
				}
				else if(r.GetComponent<SpriteMesh>()!=null)
				{
					for(int i=0;i<r.transform.childCount;++i){
						r.transform.GetChild(i).gameObject.SetActive(false);
					}
				}
				else if(r.GetComponent<SpriteFrame>()){
					//optimize memory
					SpriteFrame sf = r.GetComponent<SpriteFrame>();
					SpriteFrame.TextureFrame tf = sf.frame;
					sf.frames=new SpriteFrame.TextureFrame[]{tf};
				}
			}
			string path = AssetDatabase.GetAssetPath(animTextAsset);
			path = path.Substring(0,path.LastIndexOf('/'))+"/"+_armature.name+".prefab";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if(!prefab){
				PrefabUtility.CreatePrefab(path,_armature.gameObject,ReplacePrefabOptions.ConnectToPrefab);
			}else{
				PrefabUtility.ReplacePrefab( _armature.gameObject,prefab,ReplacePrefabOptions.ConnectToPrefab);
			}
		}
	}
}