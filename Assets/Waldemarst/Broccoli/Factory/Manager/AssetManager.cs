﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Utils;
using Broccoli.Serialization;
using Broccoli.Factory;

/// <summary>
/// Managment classes for asset components of the tree.
/// </summary>
namespace Broccoli.Manager
{
	/// <summary>
	/// Manager to create prefab from the processed trees,
	/// to optimize meshes, materials and create atlases.
	/// </summary>
	public class AssetManager {
		#region MaterialParams Class
		/// <summary>
		/// Parameters applied to the materials on the prefab.
		/// </summary>
		public class MaterialParams {
			/// <summary>
			/// Shader type.
			/// </summary>
			public enum ShaderType {
				Native,
				Custom
			}
			/// <summary>
			/// The type of the shader.
			/// </summary>
			public ShaderType shaderType = ShaderType.Native;
			/// <summary>
			/// The material textures can be used on the atlas creation process.
			/// </summary>
			public bool useInAtlas = false;
			/// <summary>
			/// If used in atlas the textures need cropping.
			/// </summary>
			public bool needsCrop = false;
			/// <summary>
			/// If true the main texture and the normal texture gets copied to the prefab folder.
			/// </summary>
			public bool copyTextures = false;
			/// <summary>
			/// Name to use when copying textures from a material.
			/// </summary>
			public string copyTexturesName = "";
			/// <summary>
			/// Initializes a new instance of the <see cref="Broccoli.Factory.AssetManager+MaterialParams"/> class.
			/// </summary>
			/// <param name="shaderType">Shader type.</param>
			/// <param name="useInAtlas">If set to <c>true</c> the textures can be used in an atlas.</param>
			/// <param name="needsCrop">If set to <c>true</c> and textures are used in an atlas, then the textures need cropping.</param>
			public MaterialParams (ShaderType shaderType, bool useInAtlas = false, bool needsCrop = false) {
				this.shaderType = shaderType;
				this.useInAtlas = useInAtlas;
				this.needsCrop = needsCrop;
			}
		}
		#endregion

		#region Mesh Class
		/// <summary>
		/// Mesh data on every pass made to process the tree (LOD level).
		/// Contains the pass and the submeshes belonging to the whole mesh.
		/// </summary>
		class AssetMesh {
			/// <summary>
			/// Number of pass the mesh belongs to.
			/// </summary>
			public int pass = 0;
			/// <summary>
			/// Submeshes to be included on the prefab. The submesh index is used as key.
			/// </summary>
			public Dictionary<int, List<Mesh>> submeshes = new Dictionary<int, List<Mesh>> ();
			/// <summary>
			/// Clear this instance.
			/// </summary>
			public void Clear () {
				var submeshesEnumerator = submeshes.GetEnumerator ();
				while (submeshesEnumerator.MoveNext ()) {
					int submeshIndex = submeshesEnumerator.Current.Key;
					if (submeshes.ContainsKey (submeshIndex)) {
						submeshes [submeshIndex].Clear ();
					}
				}
				submeshes.Clear ();
			}
		}
		#endregion

		#region Delegates and Events
		/// <summary>
		/// Delegate to call functions related to a prefab LOD gameo bject.
		/// </summary>
		/// <param name="lodGameObject"></param>
		public delegate void OnLODEvent (GameObject lodGameObject);
		/// <summary>
		/// To be called when a LOD GameObject is ready and before it gets added to the prefab.
		/// </summary>
		public OnLODEvent onLODReady;
		#endregion

		#region Vars
		#if UNITY_EDITOR
		/// <summary>
		/// Mesh information container for the asset.
		/// The index is the number of processing pass that generated the mesh.
		/// </summary>
		Dictionary<int, AssetMesh> assetMeshes = new Dictionary<int, AssetMesh> ();
		/// <summary>
		/// Relationship between group ids and their submeshes.
		/// </summary>
		public Dictionary<int, List<int>> groupIdToSubmeshIndex = new Dictionary<int, List<int>> ();
		/// <summary>
		/// Relationship between submesh index and map areas assigned to them.
		/// </summary>
		public Dictionary<int, SproutMap.SproutMapArea> submeshToArea = new Dictionary<int, SproutMap.SproutMapArea> ();
		/// <summary>
		/// Materials to be included on the prefab.
		/// The index of the submesh receiving the material is used as key.
		/// </summary>
		Dictionary<int, List<Material>> materials = new Dictionary<int, List<Material>> ();
		/// <summary>
		/// The material parameters.
		/// </summary>
		Dictionary<int, MaterialParams> materialParameters = new Dictionary<int, MaterialParams> ();
		/// <summary>
		/// True if the prefab folder has been prepared to use native materials.
		/// </summary>
		bool nativeMaterialSet = false;
		/// <summary>
		/// True if the prefab folder has been prepared to use native normal on materials.
		/// </summary>
		bool nativeMaterialNormalSet = false;
		/// <summary>
		/// Colliders to add to the prefab.
		/// </summary>
		public CapsuleCollider[] colliders = new CapsuleCollider[0];
		/// <summary>
		/// The preview target game object.
		/// </summary>
		GameObject previewTargetGameObject;
		/// <summary>
		/// Prefab object.
		/// </summary>
		UnityEngine.Object prefab;
		/// <summary>
		/// The game object to create the prefab from.
		/// </summary>
		GameObject prefabGameObject;
		/// <summary>
		/// Prefab is valid flag.
		/// </summary>
		bool prefabIsValid = false;
		/*
		/// <summary>
		/// The prefab prefix.
		/// </summary>
		public string prefabPrefix = "BroccoTree_";
		*/
		/// <summary>
		/// If true an offset is applied to the prefab mesh vertices.
		/// </summary>
		public bool applyVerticesOffset = false;
		/// <summary>
		/// Offset to apply to the prefab mesh vertices.
		/// </summary>
		public Vector3 verticesOffset = Vector3.zero;
		#endif
		/*
		/// <summary>
		/// The folder to save the prefab.
		/// </summary>
		public string prefabFolder = "Assets";
		*/
		/// <summary>
		/// The prefab path.
		/// </summary>
		public string prefabPath = "";
		/// <summary>
		/// The name of the prefab.
		/// </summary>
		public string prefabName = "";
		#endregion

		#region Singleton
		/// <summary>
		/// Asset manager singleton.
		/// </summary>
		static AssetManager _assetManager = null;
		/// <summary>
		/// Gets the singleton instance of the asset manager..
		/// </summary>
		/// <returns>The instance.</returns>
		public static AssetManager GetInstance () {
			if (_assetManager == null) {
				_assetManager = new AssetManager ();
			}
			return _assetManager;
		}
		#endregion

		#region Prefab Operations
		/// <summary>
		/// Begins with the creation process for the prefab.
		/// Makes sure the destination path is writable, creates the empty prefab and
		/// the prefab container object.
		/// </summary>
		/// <param name="previewTarget">The preview tree game object.</param>
		public void BeginWithCreatePrefab (GameObject previewTarget) {
			#if UNITY_EDITOR
			Clear ();
			previewTargetGameObject = previewTarget;
			prefabGameObject = new GameObject ();
			if (!AssetDatabase.IsValidFolder (GlobalSettings.prefabSavePath)) {
				throw new UnityException ("Broccoli Tree Creator: Path to create/edit the tree prefab is no valid (" + GlobalSettings.prefabSavePath + ")");
			} else {
				int i = 0;
				do {
					prefabName = GlobalSettings.prefabSavePrefix + (i==0?"":"" + i);
					prefabPath = Path.Combine (GlobalSettings.prefabSavePath, prefabName) + ".prefab";
					prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object> (prefabPath);
					i++;
				} while (prefab != null); //TODO: check without loading at path.
				if (prefab == null) {
					//prefab = PrefabUtility.CreateEmptyPrefab (prefabPath);
					#if UNITY_2018_3_OR_NEWER
					prefab = PrefabUtility.SaveAsPrefabAsset (prefabGameObject, prefabPath);
					#else
					prefab = PrefabUtility.CreatePrefab (prefabPath, prefabGameObject);
					#endif
					prefabIsValid = true;
				}
			}
			#endif
		}
		/// <summary>
		/// Ends the with the creation process for the prefab commiting the result to a prefab object.
		/// </summary>
		/// <param name="generateBillboard">True to generate a billboard asset.</param>
		/// <param name="isTreeCreator">True if the tree uses Unity Tree Creator shaders.</param>
		/// <returns><c>true</c>, if with commit was successful, <c>false</c> otherwise.</returns>
		public bool EndWithCommit (bool generateBillboard, bool isTreeCreator) {
			bool result = SavePrefab (generateBillboard, isTreeCreator);
			#if UNITY_EDITOR
			previewTargetGameObject = null;
			Object.DestroyImmediate (prefabGameObject);
			//AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (prefab));
			EditorUtility.UnloadUnusedAssetsImmediate ();
			#endif
			return result;
		}
		/// <summary>
		/// Adds a list of submeshes to be included on the prefab.
		/// </summary>
		/// <returns><c>true</c>, if submeshes were added, <c>false</c> otherwise.</returns>
		/// <param name="submeshesToAdd">Submeshes to add.</param>
		/// <param name="pass">Number of processing pass that generated the mesh.</param>
		public bool AddSubmeshes (Mesh[] submeshesToAdd, int pass = 1) {
			#if UNITY_EDITOR
			if (prefabIsValid) {
				if (!assetMeshes.ContainsKey (pass)) {
					AssetMesh assetMesh = new AssetMesh ();
					assetMesh.pass = pass;
					assetMeshes.Add (pass, assetMesh);
				}
				for (int i = 0; i < submeshesToAdd.Length; i++) {
					if (!assetMeshes[pass].submeshes.ContainsKey (i)) {
						assetMeshes[pass].submeshes [i] = new List<Mesh> ();
					}
					assetMeshes[pass].submeshes [i].Add (Object.Instantiate(submeshesToAdd[i]));
				}
				return true;
			}
			#endif
			return false;
		}
		/// <summary>
		/// Adds and binds a material to a submesh based on its index.
		/// </summary>
		/// <returns><c>true</c>, if material was added, <c>false</c> otherwise.</returns>
		/// <param name="material">Material.</param>
		/// <param name="submeshIndex">Submesh index.</param>
		/// <param name="groupId">Group identifier if the submesh belong to one.</param>
		/// <param name="area">Map area if the material belong to one.</param>
		public bool AddMaterial (Material material, int submeshIndex, int groupId = 0, SproutMap.SproutMapArea area = null) {
			#if UNITY_EDITOR
			if (prefabIsValid) {
				if (materials.ContainsKey (submeshIndex)) {
					materials [submeshIndex].Clear ();
				} else {
					materials [submeshIndex] = new List<Material> ();
				}
				if (groupIdToSubmeshIndex.ContainsKey (submeshIndex)) {
					groupIdToSubmeshIndex.Remove (submeshIndex);
				}
				//materials [submeshIndex].Add (Object.Instantiate<Material> (material));
				materials [submeshIndex].Add (material);
				if (groupId > 0) {
					if (!groupIdToSubmeshIndex.ContainsKey (groupId)) {
						groupIdToSubmeshIndex [groupId] = new List<int> ();
					}
					groupIdToSubmeshIndex [groupId].Add (submeshIndex);
				}
				if (area != null) {
					if (!submeshToArea.ContainsKey (submeshIndex)) {
						submeshToArea.Add (submeshIndex, area);
					}
				}
				return true;
			}
			#endif
			return false;
		}
		/// <summary>
		/// Adds the material parameters.
		/// </summary>
		/// <param name="materialParams">Material parameters.</param>
		/// <param name="submeshIndex">Submesh index.</param>
		public void AddMaterialParams (MaterialParams materialParams, int submeshIndex) {
			#if UNITY_EDITOR
			if (materialParams != null) {
				if (materialParameters.ContainsKey (submeshIndex)) {
					materialParameters.Remove (submeshIndex);
				}
				materialParameters.Add (submeshIndex, materialParams);
			}
			#endif
		}
		/// <summary>
		/// Clear this instance and prepares it for a new prefab creation process.
		/// </summary>
		public void Clear () {
			#if UNITY_EDITOR
			Object.DestroyImmediate (prefabGameObject);
			previewTargetGameObject = null;
			prefabGameObject = null;
			prefab = null;
			prefabName = "";
			prefabPath = "";
			prefabIsValid = false;
			applyVerticesOffset = false;
			verticesOffset = Vector3.zero;
			var assetMeshesEnumerator = assetMeshes.GetEnumerator ();
			while (assetMeshesEnumerator.MoveNext ()) {
				assetMeshesEnumerator.Current.Value.Clear ();
			}
			assetMeshes.Clear ();
			var groupIdToSubmeshIndexEnumerator = groupIdToSubmeshIndex.GetEnumerator ();
			int groupId;
			while (groupIdToSubmeshIndexEnumerator.MoveNext ()) {
				groupId = groupIdToSubmeshIndexEnumerator.Current.Key;
				if (groupIdToSubmeshIndex.ContainsKey (groupId)) {
					groupIdToSubmeshIndex [groupId].Clear ();
				}
			}
			groupIdToSubmeshIndex.Clear ();
			var materialsEnumerator = materials.GetEnumerator ();
			int materialIndex;
			while (materialsEnumerator.MoveNext ()) {
				materialIndex = materialsEnumerator.Current.Key;
				if (materials.ContainsKey (materialIndex)) {
					materials [materialIndex].Clear ();
				}
			}
			submeshToArea.Clear ();
			materials.Clear ();
			materialParameters.Clear ();
			nativeMaterialSet = false;
			nativeMaterialNormalSet = false;
			#endif
		}
		/// <summary>
		/// Gets the mesh for the prefab according to the pass level.
		/// </summary>
		/// <returns>The pass mesh.</returns>
		/// <param name="meshFilter">Mesh filter.</param>
		/// <param name="pass">Process ass.</param>
		Mesh GetMesh (MeshFilter meshFilter, int pass = 1) {
			Mesh mergingMesh = new Mesh ();
			#if UNITY_EDITOR
			if (assetMeshes.ContainsKey (pass)) {
				List<Mesh> meshes = new List<Mesh> ();
				var submeshesEnumerator = assetMeshes [pass].submeshes.GetEnumerator ();
				int meshId;
				while (submeshesEnumerator.MoveNext ()) {
					meshId = submeshesEnumerator.Current.Key;
					if (assetMeshes[pass].submeshes [meshId].Count == 1) {
						meshes.Add (assetMeshes[pass].submeshes [meshId] [0]);
					} else {
						meshes.Add (MergeMeshes (meshFilter, assetMeshes[pass].submeshes [meshId], true));
					}
				}
				mergingMesh.subMeshCount = meshes.Count;
				mergingMesh = MergeMeshes (meshFilter, meshes);
				mergingMesh.name = "Mesh";
				// Apply offset if required.
				if (applyVerticesOffset && verticesOffset != Vector3.zero) {
					Vector3[] vertices = mergingMesh.vertices;
					for (int i = vertices.Length - 1; i >= 0; i--) {
						vertices [i] = vertices [i] - verticesOffset;
					}
					mergingMesh.vertices = vertices;
					mergingMesh.RecalculateBounds ();
				}
			}
			#endif
			return mergingMesh;
		}
		/// <summary>
		/// Merges a list of submeshes into one mesh.
		/// </summary>
		/// <returns>A single mesh.</returns>
		/// <param name="submeshesToMerge">Submeshes to merge.</param>
		/// <param name="fullMerge">If set to <c>true</c> the submeshes are not included as indexed submeshes on the final mesh.</param>
		Mesh MergeMeshes (MeshFilter meshFilter, List<Mesh> submeshesToMerge, bool fullMerge = false) {
			Mesh mergingMesh = new Mesh ();
			#if UNITY_EDITOR
			mergingMesh.subMeshCount = submeshesToMerge.Count;
			CombineInstance[] combine = new CombineInstance[submeshesToMerge.Count];
			for (int i = 0; i < submeshesToMerge.Count; i++) {
				combine [i].mesh = submeshesToMerge[i];
				combine [i].transform = meshFilter.transform.localToWorldMatrix;
				combine [i].subMeshIndex = 0;
			}
			mergingMesh.CombineMeshes (combine, fullMerge, false);
			mergingMesh.name = "Mesh";
			#endif
			return mergingMesh;
		}
		/// <summary>
		/// Saves the prefab.
		/// </summary>
		/// <param name="generateBillboard">True to generate a billboard asset.</param>
		/// <param name="isTreeCreator">True if the tree uses Unity Tree Creator shaders.</param>
		/// <returns><c>true</c>, if prefab was saved, <c>false</c> otherwise.</returns>
		bool SavePrefab (bool generateBillboard, bool isTreeCreator) {
			#if UNITY_EDITOR
			if (prefabIsValid) {

				// No LODS
				if (assetMeshes.Count == 1 && !generateBillboard) {
					int pass = 1;
					if (!assetMeshes.ContainsKey (pass)) {
						pass = 2;
					}
					Mesh mainMesh = CreateMeshGameObject (prefabGameObject, pass);
					MeshRenderer meshRenderer = prefabGameObject.GetComponent<MeshRenderer> ();
					meshRenderer.sharedMaterials = new Material[mainMesh.subMeshCount];
					Material[] sharedMaterialsCopy = meshRenderer.sharedMaterials;

					// Add materials.
					int i = 0; 
					var enumerator = materials.GetEnumerator ();
					while (enumerator.MoveNext ()) {
						var materialPair = enumerator.Current;
						if (string.IsNullOrEmpty (AssetDatabase.GetAssetPath (materialPair.Value [0]))) {
							materialPair.Value [0] = Object.Instantiate<Material> (materialPair.Value [0]);
							materialPair.Value [0].name = materialPair.Value [0].name.Replace ("(Clone)", "");
							AssetDatabase.AddObjectToAsset (materialPair.Value [0], prefab);
						}
						if (materialParameters.ContainsKey (materialPair.Key)) {
							switch (materialParameters [materialPair.Key].shaderType) {
							case MaterialParams.ShaderType.Native:
								SetNativeMaterial (materialPair.Value [0], isTreeCreator);
								break;
							case MaterialParams.ShaderType.Custom:
								// TODO: Not implemented yet.
								break;
							}
						}
						sharedMaterialsCopy [i] = materialPair.Value [0];
						i++;
					}
					meshRenderer.sharedMaterials = sharedMaterialsCopy;

					// Add tree controller.
					if (GlobalSettings.prefabAddController) {
						Broccoli.Controller.BroccoTreeController treeController = prefabGameObject.AddComponent<Broccoli.Controller.BroccoTreeController> ();
						treeController.shaderType = (Broccoli.Controller.BroccoTreeController.ShaderType)MaterialManager.leavesShaderType;
						treeController.version = Broccoli.Base.BroccoliExtensionInfo.GetVersion ();
						treeController.windScale = TreeFactory.GetActiveInstance ().treeFactoryPreferences.factoryScale;

						// Set Wind
						WindEffectElement windEffectElement= (WindEffectElement)TreeFactory.GetActiveInstance ().localPipeline.GetElement (PipelineElement.ClassType.WindEffect, true);
						if (windEffectElement) {
							treeController.localWindAmplitude = windEffectElement.windAmplitude;
							treeController.sproutTurbulance = windEffectElement.sproutTurbulence;
							treeController.sproutSway = windEffectElement.sproutSway;
						}
					}

					// Add appendable components.
					List<ComponentReference> components = 
						TreeFactory.GetActiveInstance ().treeFactoryPreferences.appendableComponents;
					for (int j = 0; j < components.Count; j++) {
						if (components[j] != null && components[j].script.SystemType != null) {
							components[j].AddTo (prefabGameObject);
						}
					}
					if (onLODReady != null) onLODReady.Invoke (prefabGameObject);

					#if UNITY_2018_3_OR_NEWER
					prefab = PrefabUtility.SaveAsPrefabAsset (prefabGameObject, prefabPath);
					#else
					PrefabUtility.ReplacePrefab (prefabGameObject, prefab);
					#endif
				} else {
					// LODs
					LODGroup lodGroup = prefabGameObject.AddComponent<LODGroup> ();
					lodGroup.animateCrossFading = true;
					lodGroup.fadeMode = LODFadeMode.CrossFade;
					LOD[] lods = new LOD[assetMeshes.Count + 1];
					bool materialAddedToAsset = false;

					int i = 0;

					// Create LODs
					for (int pass = assetMeshes.Count; pass >= 1; pass--) {
						GameObject lodGameObject = new GameObject ();
						Mesh lodMesh = CreateMeshGameObject (lodGameObject, pass);
						lodMesh.name = "LOD_" + i;
						lodGameObject.transform.parent = prefabGameObject.transform;
						lodGameObject.name = "LOD_" + i;

						MeshRenderer meshRenderer = lodGameObject.GetComponent<MeshRenderer> ();
						meshRenderer.sharedMaterials = new Material [lodMesh.subMeshCount];

						// Add Tree Controller.
						if (GlobalSettings.prefabAddController) {
							Broccoli.Controller.BroccoTreeController treeController = lodGameObject.AddComponent<Broccoli.Controller.BroccoTreeController> ();
							treeController.shaderType = (Broccoli.Controller.BroccoTreeController.ShaderType)MaterialManager.leavesShaderType;
							treeController.version = Broccoli.Base.BroccoliExtensionInfo.GetVersion ();
							treeController.windScale = TreeFactory.GetActiveInstance ().treeFactoryPreferences.factoryScale;
							
							// Set Wind
							WindEffectElement windEffectElement= (WindEffectElement)TreeFactory.GetActiveInstance ().localPipeline.GetElement (PipelineElement.ClassType.WindEffect, true);
							if (windEffectElement) {
								treeController.localWindAmplitude = windEffectElement.windAmplitude;
								treeController.sproutTurbulance = windEffectElement.sproutTurbulence;
								treeController.sproutSway = windEffectElement.sproutSway;
							}
						}

						// Add appendable components.
						List<ComponentReference> components = 
							TreeFactory.GetActiveInstance ().treeFactoryPreferences.appendableComponents;
						for (int k = 0; k < components.Count; k++) {
							if (components[k] != null) {
								components[k].AddTo (lodGameObject);
							}
						}

						// Add materials.
						int j = 0; 
						var materialsEnumerator = materials.GetEnumerator ();
						while (materialsEnumerator.MoveNext ()) {
							var materialPair = materialsEnumerator.Current;
							if (!materialAddedToAsset &&
								string.IsNullOrEmpty (AssetDatabase.GetAssetPath (materialPair.Value [0]))) {
								materialPair.Value [0] = Object.Instantiate<Material> (materialPair.Value [0]);
								materialPair.Value [0].name = materialPair.Value [0].name.Replace ("(Clone)", "");
								AssetDatabase.AddObjectToAsset (materialPair.Value [0], prefab);
							}
							Material[] sharedMaterialsCopy = meshRenderer.sharedMaterials;
							if (materialParameters.ContainsKey (materialPair.Key)) {
								switch (materialParameters [materialPair.Key].shaderType) {
								case MaterialParams.ShaderType.Native:
									SetNativeMaterial (materialPair.Value [0], isTreeCreator);
									break;
								case MaterialParams.ShaderType.Custom:
									// TODO: Not implemented yet.
									break;
								}
							}
							sharedMaterialsCopy [j] = materialPair.Value [0];
							meshRenderer.sharedMaterials = sharedMaterialsCopy;
							j++;
						}
						materialAddedToAsset = true;
						Renderer[] renderers = new Renderer[1];
						renderers[0] = lodGameObject.GetComponent<Renderer> ();
						if (assetMeshes.Count == 2) {
							lods [i] = new LOD (0.6f - (0.4f * i), renderers);
						} else {
							lods [i] = new LOD (0.5f - (0.5f * i), renderers);
						}
						if (onLODReady != null) onLODReady.Invoke (lodGameObject);
						i++;
					}

					// Create billboard.
					BillboardBuilder billboardBuilder = BillboardBuilder.GetInstance ();
					if (generateBillboard) {
						int textureSize = TreeFactory.GetAtlasSize (TreeFactory.GetActiveInstance ().treeFactoryPreferences.billboardTextureSize);
						billboardBuilder.textureSize = new Vector2 (textureSize, textureSize);
						billboardBuilder.billboardTexturePath = 
							GetPrefabTextureFolder () + "/" + GlobalSettings.prefabTexturesPrefix + "billboard.png";
						billboardBuilder.billboardNormalTexturePath = 
							GetPrefabTextureFolder () + "/" + GlobalSettings.prefabTexturesPrefix + "billboard_normal.png";
						GameObject billboardGameObject = 
							billboardBuilder.GenerateBillboardAsset (previewTargetGameObject);
						billboardGameObject.transform.parent = prefabGameObject.transform;
						billboardGameObject.name = "LOD_" + i;
						BillboardAsset billboardAsset = billboardBuilder.GetBillboardAsset (true);
						Material billboardMaterial = billboardBuilder.GetBillboardMaterial (true);
						billboardMaterial.name = "Billboard Material";
						billboardAsset.name = "Billboard Asset";
						billboardAsset.material = billboardMaterial;
						billboardGameObject.GetComponent<BillboardRenderer> ().billboard = billboardAsset;
						billboardGameObject.transform.position = new Vector3(0, billboardBuilder.meshTargetYOffset, 0);
						if (onLODReady != null) onLODReady.Invoke (billboardGameObject);
						AssetDatabase.AddObjectToAsset (billboardMaterial, prefab);
						AssetDatabase.AddObjectToAsset (billboardAsset, prefab);
						Renderer[] billboardRenderers = new Renderer[1];
						billboardRenderers[0] = billboardGameObject.GetComponent<Renderer> ();
						if (assetMeshes.Count == 2) {
							lods [i] = new LOD (0.6f - (0.4f * i), billboardRenderers);
						} else {
							lods [i] = new LOD (0.5f - (0.5f * i * 2), billboardRenderers);
						}
					}

					lodGroup.SetLODs (lods);

					// Add collision objects.
					for (int j = 0; j < colliders.Length; j++) {
						CapsuleCollider collider = prefabGameObject.AddComponent<CapsuleCollider> ();
						collider.name = "Collider_" + j;
						collider.center = colliders[j].center;
						collider.direction = colliders[j].direction;
						collider.height = colliders[j].height;
						collider.radius = colliders[j].radius;
					}

					#if UNITY_2018_3_OR_NEWER
					prefab = PrefabUtility.SaveAsPrefabAsset (prefabGameObject, prefabPath);
					#else
					PrefabUtility.ReplacePrefab (prefabGameObject, prefab);
					#endif
					billboardBuilder.Clear ();
				}
					
				return true;
			}
			#endif
			return false;
		}
		Mesh CreateMeshGameObject (GameObject gameObject, int pass = 1) {
			Mesh mainMesh = null;
			#if UNITY_EDITOR
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter> ();
			gameObject.AddComponent<MeshRenderer> ();
			mainMesh = GetMesh (meshFilter, pass);
			AssetDatabase.AddObjectToAsset (mainMesh, prefab);
			meshFilter.sharedMesh = mainMesh;
			#endif
			return mainMesh;
		}
		/// <summary>
		/// Sets the properties of a native material.
		/// </summary>
		/// <param name="nativeMaterial">Native material.</param>
		/// <param name="isTreeCreator">True if the tree uses Unity Tree Creator shaders.</param>
		void SetNativeMaterial (Material nativeMaterial, bool isTreeCreator) {
			#if UNITY_EDITOR
			// TODO: move special case processing to object using an interface.
			string texturePath = GetPrefabTextureFolder ();
			string shadowTexPath = texturePath + "/shadow.png";
			string translucencyTexPath = texturePath + "/translucency_gloss.png";
			if (!nativeMaterialSet) {
				if (isTreeCreator) {
					AssetDatabase.CopyAsset (MaterialManager.GetShadowTexPath (), shadowTexPath);
					AssetDatabase.CopyAsset (MaterialManager.GetTranslucencyTexPath (), translucencyTexPath);
					AssetDatabase.ImportAsset (shadowTexPath);
					AssetDatabase.ImportAsset (translucencyTexPath);
				}
				nativeMaterialSet = true;
			}
			if (nativeMaterial.HasProperty ("_ShadowTex")) {
				Texture2D shadowTex = AssetDatabase.LoadAssetAtPath<Texture2D> (shadowTexPath);
				nativeMaterial.SetTexture ("_ShadowTex", shadowTex);
			}
			if (nativeMaterial.HasProperty ("_TranslucencyMap")) {
				Texture2D translucencyTex = AssetDatabase.LoadAssetAtPath<Texture2D> (translucencyTexPath);
				nativeMaterial.SetTexture ("_TranslucencyMap", translucencyTex);
			}
			if (nativeMaterial.HasProperty ("_BumpSpecMap") && nativeMaterial.GetTexture ("_BumpSpecMap") == null) {
				string normalSpecularTexPath = texturePath + "/normal_specular.png";
				if (!nativeMaterialNormalSet) {
					AssetDatabase.CopyAsset (MaterialManager.GetNormalSpecularTexPath (), normalSpecularTexPath);
					AssetDatabase.ImportAsset (normalSpecularTexPath);
					nativeMaterialNormalSet = true;
				}
				Texture2D normalSpecularTex = AssetDatabase.LoadAssetAtPath<Texture2D> (normalSpecularTexPath);
				nativeMaterial.SetTexture ("_BumpSpecMap", normalSpecularTex);
			}
			#endif
		}
		#endregion

		#region Prefab Optimization
		/// <summary>
		/// Optimizes the submeshes (mergin) and materials based on their group id.
		/// </summary>
		public void OptimizeOnGroups () {
			#if UNITY_EDITOR
			Dictionary<string, List<int>> textureToSubmeshIndex = new Dictionary<string, List<int>> ();
			string textureName;

			// Traverse groups with submeshes
			var groupIdToSubmeshIndexEnumerator = groupIdToSubmeshIndex.GetEnumerator ();
			int groupId;
			while (groupIdToSubmeshIndexEnumerator.MoveNext ()) {
				groupId = groupIdToSubmeshIndexEnumerator.Current.Key;
				var textureToSubmeshIndexEnumerator = textureToSubmeshIndex.GetEnumerator ();
				while (textureToSubmeshIndexEnumerator.MoveNext ()) {
					textureToSubmeshIndexEnumerator.Current.Value.Clear ();
				}
				textureToSubmeshIndex.Clear ();

				int submeshId;
				for (int i = 0; i < groupIdToSubmeshIndex [groupId].Count; i++) {
					submeshId = groupIdToSubmeshIndex [groupId] [i];
					if (materials.ContainsKey (submeshId)) {
						// TODO: use texture instance instead
						if (materials [submeshId] [0].HasProperty ("_MainTex")) {
							textureName = materials [submeshId] [0].mainTexture.GetInstanceID () + "";
							if (!textureToSubmeshIndex.ContainsKey (textureName)) {
								textureToSubmeshIndex [textureName] = new List<int> ();
							}
							textureToSubmeshIndex [textureName].Add (submeshId);
						}
					}
				}

				// Add submeshes to merge on the same list.
				var assetMeshesEnumerator = assetMeshes.GetEnumerator ();
				int meshPass;
				bool setMaterialOnPass = false;
				while (assetMeshesEnumerator.MoveNext ()) {
					meshPass = assetMeshesEnumerator.Current.Key;
					textureToSubmeshIndexEnumerator = textureToSubmeshIndex.GetEnumerator ();
					while (textureToSubmeshIndexEnumerator.MoveNext ()) {
						var textureToSubmeshIndexPair = textureToSubmeshIndexEnumerator.Current;
						int containerSubmeshIndex = -1;
						int submeshToMergeIndex;
						for (int i = 0; i < textureToSubmeshIndexPair.Value.Count; i++) {
							submeshToMergeIndex = textureToSubmeshIndexPair.Value [i];
							if (containerSubmeshIndex < 0) {
								containerSubmeshIndex = submeshToMergeIndex;
							} else {
								if (assetMeshes [meshPass].submeshes.ContainsKey (containerSubmeshIndex)) {
									assetMeshes [meshPass].submeshes [containerSubmeshIndex].Add (
										assetMeshes [meshPass].submeshes [submeshToMergeIndex] [0]);
									if (assetMeshes [meshPass].submeshes.ContainsKey (submeshToMergeIndex)) {
										assetMeshes [meshPass].submeshes [submeshToMergeIndex].Clear ();
										assetMeshes [meshPass].submeshes.Remove (submeshToMergeIndex);
									}
								}
								if (materials.ContainsKey (containerSubmeshIndex) && !setMaterialOnPass) {
									materials [containerSubmeshIndex].Add (materials [submeshToMergeIndex] [0]);
									if (materials.ContainsKey (submeshToMergeIndex)) {
										materials [submeshToMergeIndex].Clear ();
										materials.Remove (submeshToMergeIndex);
									}
								}
							}
						}
					}
					setMaterialOnPass = true;
				}
			}

			// Cleaning
			var enumerator = textureToSubmeshIndex.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				enumerator.Current.Value.Clear ();
			}
			textureToSubmeshIndex.Clear ();
			#endif
		}
		/// <summary>
		/// Optimizes texture materials by creating an atlas.
		/// </summary>
		/// <param name="atlasMaximumSize">Atlas maximum size.</param>
		public void OptimizeForAtlas (int atlasMaximumSize = 512) {
			#if UNITY_EDITOR
			TextureManager textureManager = TextureManager.GetInstance ();
			textureManager.Clear ();
			Dictionary <int, int> rectToSubmeshIndex = new Dictionary<int, int> ();
			int i = 0;

			var materialsEnumerator = materials.GetEnumerator ();
			int submeshIndex;
			while (materialsEnumerator.MoveNext ()) {
				submeshIndex = materialsEnumerator.Current.Key;
				if (materialParameters.ContainsKey (submeshIndex)) {
					MaterialParams materialParams = materialParameters [submeshIndex];
					if (materialParams.useInAtlas) {
						Material material = materials [submeshIndex][0];
						if (materialParams.needsCrop && submeshToArea [submeshIndex] != null) {
							SproutMap.SproutMapArea sproutArea = submeshToArea [submeshIndex];
							sproutArea.Normalize ();
							Texture2D texture = TextureUtil.CropTextureRelative (textureManager.GetCopy (sproutArea.texture),
								                    sproutArea.x, 
								                    sproutArea.y, 
								                    sproutArea.width, 
								                    sproutArea.height);
							textureManager.AddTexture (submeshIndex.ToString (), texture);
							textureManager.RegisterTextureToAtlas (submeshIndex.ToString());
							RegisterNormalTextureToAtlas (submeshIndex, material, texture.width, texture.height);
							rectToSubmeshIndex.Add (i, submeshIndex);
							i++;
						} else {
							Texture2D mainTexture = textureManager.GetMainTexture (material);
							if (mainTexture != null) {
								textureManager.AddTexture (submeshIndex.ToString (), mainTexture);
								textureManager.RegisterTextureToAtlas (submeshIndex.ToString ());
								RegisterNormalTextureToAtlas (submeshIndex, material, mainTexture.width, mainTexture.height);
								rectToSubmeshIndex.Add (i, submeshIndex);
								i++;
							}
						}
					}
				}
			}

			// Create atlas.
			if (textureManager.GetTextureCount() > 0) {
				// Create atlas.
				string folderPath = GetPrefabTextureFolder ();
				textureManager.SaveAtlasesToAssets (folderPath, atlasMaximumSize);
				Texture2D atlasTexture = 
					UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D> (textureManager.GetAtlasAssetPath ());
				Texture2D normalAtlasTexture = 
					UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D> (textureManager.GetAtlasAssetPath ("normal_atlas"));
				Rect[] submeshesRect = textureManager.GetAtlasRects ();

				// Set materials.
				if (!string.IsNullOrEmpty (textureManager.GetAtlasAssetPath ())) {
					var rectToSubmeshIndexEnumerator = rectToSubmeshIndex.GetEnumerator ();
					int rectIndex;
					while (rectToSubmeshIndexEnumerator.MoveNext ()) {
						rectIndex = rectToSubmeshIndexEnumerator.Current.Key;
						submeshIndex = rectToSubmeshIndex [rectIndex];
						// Set main texture to atlas.
						materials [submeshIndex] [0].SetTexture ("_MainTex", atlasTexture);
						// Set normal texture to atlas.
						materials [submeshIndex] [0].SetTexture ("_BumpSpecMap", normalAtlasTexture);
						var assetMeshesEnumerator = assetMeshes.GetEnumerator ();
						int meshPass;
						while (assetMeshesEnumerator.MoveNext ()) {
							meshPass = assetMeshesEnumerator.Current.Key;
							if (submeshToArea.ContainsKey (submeshIndex)) {
								UpdateUVs (assetMeshes[meshPass].submeshes [submeshIndex] [0], 
									submeshesRect [rectIndex], 
									submeshToArea [submeshIndex]);
							} else if (submeshIndex >= 0) {
								UpdateUVs (assetMeshes[meshPass].submeshes [submeshIndex] [0], 
									submeshesRect [rectIndex]);
							}
						}
					}
				}
			}

			// Copy required textures.
			var materialParametersEnumerator = materialParameters.GetEnumerator ();
			while (materialParametersEnumerator.MoveNext ()) {
				submeshIndex = materialParametersEnumerator.Current.Key;
				if (materialParameters [submeshIndex].copyTextures && 
					!string.IsNullOrEmpty (materialParameters [submeshIndex].copyTexturesName)) {
					string folderPath = GetPrefabTextureFolder ();
					string texturePath = folderPath + "/" + materialParameters [submeshIndex].copyTexturesName + ".png";
					Material material = materials [submeshIndex] [0];
					Texture2D mainTex = textureManager.GetMainTexture (material, false);
					if (mainTex != null) {
						AssetDatabase.CopyAsset (AssetDatabase.GetAssetPath (mainTex), texturePath);
						AssetDatabase.ImportAsset (texturePath);
						material.SetTexture("_MainTex",  AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath));
						// Set normal texture.
						Texture2D normalTex = textureManager.GetNormalTexture (material, false);
						texturePath = folderPath + "/" + materialParameters [submeshIndex].copyTexturesName + "_normal.png";
						if (normalTex != null) {
							AssetDatabase.CopyAsset (AssetDatabase.GetAssetPath (normalTex), texturePath);
							AssetDatabase.ImportAsset (texturePath);
							if (material.HasProperty ("_BumpSpecMap")) {
								material.SetTexture("_BumpSpecMap",  AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath));
							} else if (material.HasProperty ("_BumpMap")) {
								material.SetTexture("_BumpMap",  AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath));
							}
						}
					}
				}
			}

			rectToSubmeshIndex.Clear ();
			textureManager.Clear ();
			#endif
		}
		/// <summary>
		/// Registers a normal texture to atlas.
		/// </summary>
		/// <param name="submeshIndex">Submesh index.</param>
		/// <param name="baseMaterial">Base material.</param>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		private void RegisterNormalTextureToAtlas (int submeshIndex, Material baseMaterial, int width, int height) {
			#if UNITY_EDITOR
			if (baseMaterial != null && width > 0 && height > 0) {
				TextureManager textureManager = TextureManager.GetInstance ();
				Texture2D normalTexture = textureManager.GetNormalTexture (baseMaterial, true);
				if (normalTexture == null) {
					normalTexture = MaterialManager.GetNormalSpecularTex (true);
				}
				if (normalTexture != null) {
					TextureUtil.BilinearScale (normalTexture, width, height);
					textureManager.AddTexture (submeshIndex.ToString () + "n", normalTexture);
					textureManager.RegisterTextureToAtlas (submeshIndex.ToString () + "n", "normal_atlas");
				}
			}
			#endif
		}
		/// <summary>
		/// Gets the prefab texture folder.
		/// </summary>
		/// <returns>The prefab texture folder.</returns>
		public string GetPrefabTextureFolder () {
			return TextureManager.GetInstance ().GetOrCreateFolder (GetPrefabFolder (), GetPrefabName () + "_Textures");
		}
		/// <summary>
		/// UV channel update process for new texture areas after atlas optimization.
		/// </summary>
		/// <param name="mesh">Mesh to update UVs.</param>
		/// <param name="rect">Rect resulting from atlas creation.</param>
		private void UpdateUVs (Mesh mesh, Rect rect) {
			UpdateUVs (mesh, rect, 0f, 0f, 1f, 1f);
		}
		/// <summary>
		/// UV channel update process for new texture areas after atlas optimization.
		/// </summary>
		/// <param name="mesh">Mesh to update UVs.</param>
		/// <param name="rect">Rect resulting from atlas creation.</param>
		/// <param name="sproutArea">Sprout mapping area.</param>
		private void UpdateUVs (Mesh mesh, Rect rect, SproutMap.SproutMapArea sproutArea) {
			UpdateUVs (mesh, rect, sproutArea.x, sproutArea.y, sproutArea.width, sproutArea.height);
		}
		/// <summary>
		/// UV channel update process for new texture areas after atlas optimization.
		/// </summary>
		/// <param name="mesh">Mesh to update UVs.</param>
		/// <param name="rect">Rect resulting from atlas creation.</param>
		/// <param name="originalX">Current x offset used on the UVs.</param>
		/// <param name="originalY">Current y offset used on the UVs.</param>
		/// <param name="originalWidth">Current width used on the UVs.</param>
		/// <param name="originalHeight">Current height used on the UVs.</param>
		private void UpdateUVs (Mesh mesh, Rect rect, float originalX, float originalY, float originalWidth, float originalHeight) {
			float widthRel, heightRel;
			widthRel = rect.width / originalWidth;
			heightRel = rect.height / originalHeight;
			List<Vector4> uvs = new List<Vector4> ();
			mesh.GetUVs (0, uvs);
			for (int i = 0; i < uvs.Count; i++) {
				uvs[i] = new Vector4 (rect.x + (widthRel * (uvs[i].x - originalX)),
					rect.y + (heightRel * (uvs[i].y - originalY)), uvs[i].z, uvs[i].w);
			}
			mesh.SetUVs (0, uvs);
		}
		#endregion

		#region Data
		/// <summary>
		/// Gets the temp filename.
		/// </summary>
		/// <returns>The temp filename.</returns>
		/// <param name="referenceObj">Reference object.</param>
		public string GetTempFilename (Object referenceObj) {
			return ("Temp/UnityTempFile" + referenceObj.GetInstanceID());
		}
		/// <summary>
		/// Gets the prefab folder.
		/// </summary>
		/// <returns>The prefab folder.</returns>
		public string GetPrefabFolder () {
			return GlobalSettings.prefabSavePath;
		}
		/// <summary>
		/// Gets the name of the prefab.
		/// </summary>
		/// <returns>The prefab name.</returns>
		public string GetPrefabName () {
			return prefabName;
		}
		#endregion
	}
}