using UnityEngine;

using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Factory;
using Broccoli.Manager;

namespace Broccoli.Component
{
	/// <summary>
	/// Branch mapper component.
	/// </summary>
	public class ProceduralBranchMapperComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The bark texture mapper element.
		/// </summary>
		ProceduralBranchMapperElement proceduralBranchMapperElement = null;
		/// <summary>
		/// Branch mesh builder.
		/// </summary>
		//BranchMeshBuilder branchMeshBuilder = null;
		/// <summary>
		/// The stylized mesh meta builder.
		/// </summary>
		StylizedMetaBuilder stylizedMetaBuilder = null;
		/// <summary>
		/// Component command.
		/// </summary>
		public enum ComponentCommand
		{
			BuildMaterials,
			SetUVs
		}
		#endregion

		#region Configuration
		/// <summary>
		/// Prepares the parameters to process with this component.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="processControl">Process control.</param>
		protected override void PrepareParams (TreeFactory treeFactory,
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl processControl = null) 
		{
			base.PrepareParams (treeFactory, useCache, useLocalCache, processControl);
			//branchMeshBuilder = BranchMeshBuilder.GetInstance ();
			/*
			if (processControl.pass == 1 && !treeFactory.treeFactoryPreferences.prefabStrictLowPoly) {
				if (branchMeshGeneratorElement.maxPolygonSides > 6) {
					meshBuilder.maxPolygonSides = 6;
				} else {
					meshBuilder.maxPolygonSides = branchMeshGeneratorElement.maxPolygonSides;
				}
				if (branchMeshGeneratorElement.minPolygonSides > 3) {
					meshBuilder.minPolygonSides = 3;
				} else {
					meshBuilder.minPolygonSides = branchMeshGeneratorElement.minPolygonSides;
				}
			}
			*/
		}
		/// <summary>
		/// Gets the process prefab weight.
		/// </summary>
		/// <returns>The process prefab weight.</returns>
		/// <param name="treeFactory">Tree factory.</param>
		public override int GetProcessPrefabWeight (TreeFactory treeFactory) {
			int weight = 0;
			/*
			if (proceduralBranchMapperElement.mainTexture != null)
				weight += 15;
			if (proceduralBranchMapperElement.normalTexture != null)
				weight += 15;
			*/
			return weight;
		}
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.Material;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear () {
			base.Clear ();
			//proceduralBranchMapperElement = null;
			//stylizedMetaBuilder = null;
		}
		#endregion

		#region Processing
		/// <summary>
		/// Process the tree according to the pipeline element.
		/// </summary>
		/// <param name="treeFactory">Parent tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="ProcessControl">Process control.</param>
		public override bool Process (TreeFactory treeFactory, 
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl ProcessControl = null) {
			if (pipelineElement != null && tree != null) {
				//proceduralBranchMapperElement = pipelineElement as ProceduralBranchMapperElement;
				BuildMaterials (treeFactory);
				AssignUVs (treeFactory);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Removes the product of this component on the factory processing.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void Unprocess (TreeFactory treeFactory) {
			treeFactory.meshManager.DeregisterMeshByType (MeshManager.MeshData.Type.Branch);
			treeFactory.materialManager.DeregisterMaterialByType (MeshManager.MeshData.Type.Branch);
		}
		/// <summary>
		/// Process a special command or subprocess on this component.
		/// </summary>
		/// <param name="cmd">Cmd.</param>
		/// <param name="treeFactory">Tree factory.</param>
		public override void ProcessComponentOnly (int cmd, TreeFactory treeFactory) {
			if (pipelineElement != null && tree != null) {
				if (cmd == (int)ComponentCommand.BuildMaterials) {
					BuildMaterials (treeFactory, true);
				} else {
					AssignUVs (treeFactory, true);
				}
			}
		}
		/// <summary>
		/// Processes called only on the prefab creation.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void OnProcessPrefab (TreeFactory treeFactory) {
			#if UNITY_EDITOR
			if (proceduralBranchMapperElement.materialMode == ProceduralBranchMapperElement.MaterialMode.Custom) {
				Material material;
				if (treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled ||
					treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
					AssetManager.MaterialParams materialParams;
					if (treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
						int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
						material = treeFactory.materialManager.GetTreeCreatorMaterial (meshId, false);
						materialParams = new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Native);
						material.name = "Optimized Bark Material";
					} else {
						material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Branch, true);
						materialParams = new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Custom);
					}
					if (treeFactory.treeFactoryPreferences.prefabCopyCustomMaterialBarkTexturesEnabled) {
						materialParams.copyTextures = true;
						materialParams.copyTexturesName = "bark";
					}
					treeFactory.assetManager.AddMaterial (material, 
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					treeFactory.assetManager.AddMaterialParams (materialParams,
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
				} else {
					material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Branch, false);
					AssetManager.MaterialParams materialParams;
					materialParams = new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Custom);
					treeFactory.assetManager.AddMaterial (material, 
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					treeFactory.assetManager.AddMaterialParams (materialParams,
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
				}
			} else {
				Material material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Branch, true);
				if (material != null) {
					material.name = "Optimized Bark Material";
					treeFactory.assetManager.AddMaterial (material, 
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					if (treeFactory.treeFactoryPreferences.prefabCreateAtlas) {
						AssetManager.MaterialParams materialParams = 
							new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Native, false);
						if (treeFactory.treeFactoryPreferences.prefabCopyCustomMaterialBarkTexturesEnabled) {
							materialParams.copyTextures = true;
							materialParams.copyTexturesName = "bark";
						}
						treeFactory.assetManager.AddMaterialParams (materialParams,
							treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					}
				}
			}
			#endif
		}
		/// <summary>
		/// Builds the materials.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		private void BuildMaterials (TreeFactory treeFactory, bool updatePreviewTree = false) {
			if (proceduralBranchMapperElement.mappingMode == ProceduralBranchMapperElement.MappingMode.Gradient) {
				// GRADIENT MODE
			} else if (proceduralBranchMapperElement.mappingMode == ProceduralBranchMapperElement.MappingMode.Grid) {
				// GRID MODE

				// Get material
				int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
				Material material;
				if (treeFactory.materialManager.HasMaterial (meshId) && 
					!treeFactory.materialManager.IsCustomMaterial (meshId) &&
					treeFactory.materialManager.GetMaterial (meshId) != null) {
					material = treeFactory.materialManager.GetMaterial (meshId);
				} else {
					material = treeFactory.materialManager.GetOwnedMaterial (meshId, treeFactory.materialManager.GetBarkShader ().name);
				}

				// Set texture to the material
				if (proceduralBranchMapperElement.gridTextureMode == ProceduralBranchMapperElement.GridTextureMode.File) {
					// TEXTURE FROM FILE
					/* Available shader options
					_Color ("Main Color", Color) = (1,1,1,1)
					_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
					_BumpSpecMap ("Normalmap (GA) Spec (R)", 2D) = "bump" {}
					_TranslucencyMap ("Trans (RGB) Gloss(A)", 2D) = "white" {}
					*/
					Texture2D mainTexture = proceduralBranchMapperElement.gridTextureFile;
					material.SetTexture ("_MainTex", mainTexture);
				} else {
					// PROCEDURAL TEXTURE
				}
				material.SetTexture ("_TranslucencyMap", MaterialManager.GetTranslucencyTex ());
				material.name = "Bark";
			}

			//treeFactory.materialManager.DeregisterMaterial (MeshManager.MeshData.Type.Bark);
			
			if (updatePreviewTree) {
				MeshRenderer renderer = tree.obj.GetComponent<MeshRenderer> ();
				Material[] materials = renderer.sharedMaterials;
				for (int j = 0; j < treeFactory.meshManager.GetMeshesCount (); j++) {
					int meshId = treeFactory.meshManager.GetMergedMeshId (j);
					if (treeFactory.materialManager.GetMaterial (meshId)) {
						if (treeFactory.materialManager.IsCustomMaterial (meshId) &&
						    treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
							bool isSproutMesh = treeFactory.meshManager.IsSproutMesh (meshId);
							materials [j] = treeFactory.materialManager.GetTreeCreatorMaterial (meshId, isSproutMesh);
						} else {
							materials [j] = treeFactory.materialManager.GetMaterial (meshId, true);
						}
					}
				}
				renderer.sharedMaterials = materials;
			}
		}
		/// <summary>
		/// Assigns the UVs.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		private void AssignUVs (TreeFactory treeFactory, bool updatePreviewTree = false) {
			if (stylizedMetaBuilder == null) {
				stylizedMetaBuilder = new StylizedMetaBuilder ();
			}
			BranchMeshGeneratorElement branchMeshGeneratorElement;
			branchMeshGeneratorElement = 
				(BranchMeshGeneratorElement) proceduralBranchMapperElement.GetUpstreamElement (
					PipelineElement.ClassType.BranchMeshGenerator);
			
			if (branchMeshGeneratorElement != null &&
			    treeFactory.meshManager.HasMesh (MeshManager.MeshData.Type.Branch)) {
				Vector2[] originalUVs = new Vector2[0];
				if (updatePreviewTree) {
					MeshFilter meshFilter = tree.obj.GetComponent<MeshFilter> ();
					originalUVs = meshFilter.sharedMesh.uv;
				}

				/*
				BranchMeshGeneratorComponent branchMeshGeneratorComponent = 
					(BranchMeshGeneratorComponent)treeFactory.componentManager.GetFactoryComponent (branchMeshGeneratorElement);
				stylizedMetaBuilder.displacementDeltaX = proceduralBranchMapperElement.mappingXDisplacement;
				stylizedMetaBuilder.displacementDeltaY = proceduralBranchMapperElement.mappingYDisplacement;
				stylizedMetaBuilder.isGirthSensitive = proceduralBranchMapperElement.isGirthSensitive;

				stylizedMetaBuilder.applyMappingOffsetFromParent = proceduralBranchMapperElement.applyMappingOffsetFromParent;
				*/
				/*
				Vector2[] uvs = stylizedMetaBuilder.SetMeshUVs (treeFactory.meshManager.GetMesh (MeshManager.MeshData.Type.Bark),
					treeFactory.previewTree,
					branchMeshGeneratorComponent.vertexInfos,
					branchMeshGeneratorComponent.triangleInfos);
					*/
				Vector2[] uvs = new Vector2[treeFactory.meshManager.GetMesh (MeshManager.MeshData.Type.Branch).vertexCount];
				// REFACTOR

				if (updatePreviewTree) {
					int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
					int vertexOffset = treeFactory.meshManager.GetMergedMeshVertexOffset (meshId);
					for (int j = 0; j < uvs.Length; j++) {
						originalUVs [vertexOffset + j] = uvs [j];
					}
					MeshFilter meshFilter = tree.obj.GetComponent<MeshFilter> ();
					meshFilter.sharedMesh.uv = originalUVs;
				}
			}
		}
		#endregion
	}
}