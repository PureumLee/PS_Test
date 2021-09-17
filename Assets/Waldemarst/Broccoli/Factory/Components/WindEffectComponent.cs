using System.Collections.Generic;

using UnityEngine;

using Broccoli.Pipe;
using Broccoli.Model;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Factory;

namespace Broccoli.Component
{
	/// <summary>
	/// Wind effect component.
	/// </summary>
	public class WindEffectComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The wind effect element.
		/// </summary>
		WindEffectElement windEffectElement = null;
		/// <summary>
		/// The wind meta builder.
		/// </summary>
		WindMetaBuilder windMetaBuilder = new WindMetaBuilder ();
		/// <summary>
		/// SpeedTree8 compatible wind meta builder.
		/// </summary>
		STWindMetaBuilder stWindMetaBuilder = new STWindMetaBuilder ();
		/// <summary>
		/// The UVs (channel 0) on each meshId.
		/// </summary>
		Dictionary<int, List<Vector4>> meshIdToUV = new Dictionary<int, List<Vector4>> ();
		/// <summary>
		/// The UV2s (channel 1) on each meshId.
		/// </summary>
		Dictionary<int, List<Vector4>> meshIdToUV2 = new Dictionary<int, List<Vector4>> ();
		/// <summary>
		/// The UV2s used on the merged mesh, placeholder for preview updating operations.
		/// </summary>
		List<Vector4> mergedMeshUV2s = new List<Vector4> ();
		/// <summary>
		/// The colors on each meshId.
		/// </summary>
		Dictionary<int, Color[]> meshIdToColor = new Dictionary<int, Color[]> ();
		/// <summary>
		/// The colors used on the merged mesh, placeholder for preview updating operations.
		/// </summary>
		Color[] mergedMeshColors = new Color[0];
		/// <summary>
		/// UV2 per sprout.
		/// </summary>
		Dictionary<int, Vector4> sproutUV2 = new Dictionary<int, Vector4> ();
		/// <summary>
		/// Color per sprout.
		/// </summary>
		Dictionary<int, Color> sproutColor = new Dictionary<int, Color> ();
		/// <summary>
		/// Use SpeedTree 8 vertex mapping.
		/// </summary>
		bool mapST = true;
		#endregion

		#region Configuration
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.None;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear ()
		{
			base.Clear ();
			windEffectElement = null;
			windMetaBuilder.Clear ();
			stWindMetaBuilder.Clear ();
			sproutUV2.Clear ();
			sproutColor.Clear ();
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
				windEffectElement = pipelineElement as WindEffectElement;
				mapST = MaterialManager.leavesShaderType != MaterialManager.LeavesShaderType.TreeCreatorOrSimilar;

				BranchMeshGeneratorElement branchMeshGeneratorElement = 
				(BranchMeshGeneratorElement) windEffectElement.GetUpstreamElement (PipelineElement.ClassType.BranchMeshGenerator); 
			
				if (branchMeshGeneratorElement != null && branchMeshGeneratorElement.isActive) {
					// Prepare branch vertex information for traversing the tree.
					BranchMeshGeneratorComponent branchMeshGeneratorComponent = 
						(BranchMeshGeneratorComponent) treeFactory.componentManager.GetFactoryComponent (
							branchMeshGeneratorElement);
					if (mapST) {
						stWindMetaBuilder.windSpread = windEffectElement.windSpread;
						stWindMetaBuilder.windAmplitude = windEffectElement.windAmplitude;
						stWindMetaBuilder.sproutTurbulence = windEffectElement.sproutTurbulence;
						stWindMetaBuilder.sproutSway = windEffectElement.sproutSway;
						stWindMetaBuilder.weightCurve = windEffectElement.windFactorCurve;
						stWindMetaBuilder.useMultiPhaseOnTrunk = windEffectElement.useMultiPhaseOnTrunk;
						stWindMetaBuilder.isST7 = MaterialManager.leavesShaderType == MaterialManager.LeavesShaderType.SpeedTree7OrSimilar;
						stWindMetaBuilder.applyToRoots = windEffectElement.applyToRoots;
						stWindMetaBuilder.AnalyzeTree (tree, branchMeshGeneratorComponent.branchSkins);
					} else {
						windMetaBuilder.windSpread = windEffectElement.windSpread;
						windMetaBuilder.windAmplitude = windEffectElement.windAmplitude;
						windMetaBuilder.weightCurve = windEffectElement.windFactorCurve;
						windMetaBuilder.useMultiPhaseOnTrunk = windEffectElement.useMultiPhaseOnTrunk;
						windMetaBuilder.applyToRoots = windEffectElement.applyToRoots;
						windMetaBuilder.AnalyzeTree (tree, branchMeshGeneratorComponent.branchSkins);
					}
					SetWindData (treeFactory);
					if (mapST) {
						stWindMetaBuilder.Clear ();
					} else {
						windMetaBuilder.Clear ();
					}
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Process a special command or subprocess on this component.
		/// </summary>
		/// <param name="cmd">Cmd.</param>
		/// <param name="treeFactory">Tree factory.</param>
		public override void ProcessComponentOnly (int cmd, TreeFactory treeFactory) {
			if (pipelineElement != null && tree != null) {
				windEffectElement = pipelineElement as WindEffectElement;
				mapST = MaterialManager.leavesShaderType != MaterialManager.LeavesShaderType.TreeCreatorOrSimilar;

				BranchMeshGeneratorElement branchMeshGeneratorElement = 
				(BranchMeshGeneratorElement) windEffectElement.GetUpstreamElement (PipelineElement.ClassType.BranchMeshGenerator); 
			
				if (branchMeshGeneratorElement != null && branchMeshGeneratorElement.isActive) {
					// Prepare branch vertex information for traversing the tree.
					BranchMeshGeneratorComponent branchMeshGeneratorComponent = 
						(BranchMeshGeneratorComponent) treeFactory.componentManager.GetFactoryComponent (
							branchMeshGeneratorElement);
					mapST = MaterialManager.leavesShaderType != MaterialManager.LeavesShaderType.TreeCreatorOrSimilar;
					if (mapST) {
						stWindMetaBuilder.windSpread = windEffectElement.windSpread;
						stWindMetaBuilder.windAmplitude = windEffectElement.windAmplitude;
						stWindMetaBuilder.weightCurve = windEffectElement.windFactorCurve;
						stWindMetaBuilder.useMultiPhaseOnTrunk = windEffectElement.useMultiPhaseOnTrunk;
						stWindMetaBuilder.isST7 = MaterialManager.leavesShaderType == MaterialManager.LeavesShaderType.SpeedTree7OrSimilar;
						stWindMetaBuilder.AnalyzeTree (tree, branchMeshGeneratorComponent.branchSkins);
					} else {
						windMetaBuilder.windSpread = windEffectElement.windSpread;
						windMetaBuilder.windAmplitude = windEffectElement.windAmplitude;
						windMetaBuilder.useMultiPhaseOnTrunk = windEffectElement.useMultiPhaseOnTrunk;
						windMetaBuilder.AnalyzeTree (tree, branchMeshGeneratorComponent.branchSkins);
					}
					SetWindData (treeFactory, true);
					if (mapST) {
						stWindMetaBuilder.Clear ();
					} else {
						windMetaBuilder.Clear ();
					}
				}
			}
		}
		/// <summary>
		/// Sets the wind data.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		void SetWindData (TreeFactory treeFactory, bool updatePreviewTree = false) {
			// Clean merged mesh UV2s and colors if we are going to update the whole mesh.
			if (updatePreviewTree) {
				mergedMeshUV2s.Clear ();
				mergedMeshColors  = new Color[0];
			}

			/// <summary>
			/// 
			/// </summary>
			/// <value></value>
			if (mapST) {
				// Set wind on branch mesh
				int branchMeshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
				Mesh mesh = treeFactory.meshManager.GetMesh (branchMeshId);
				stWindMetaBuilder.SetBranchesWindData (treeFactory.previewTree, branchMeshId, mesh);

				// Set wind on each sprouts mesh
				Dictionary<int, MeshManager.MeshData> meshDatas = 
					treeFactory.meshManager.GetMeshesDataOfType (MeshManager.MeshData.Type.Sprout);
				var meshDatasEnumerator = meshDatas.GetEnumerator ();
				int sproutMeshId;
				while (meshDatasEnumerator.MoveNext ()) {
					sproutMeshId = meshDatasEnumerator.Current.Key;
					mesh = treeFactory.meshManager.GetMesh (sproutMeshId);
					if (treeFactory.meshManager.GetMesh (sproutMeshId) != null && treeFactory.meshManager.HasMeshParts (sproutMeshId)) {
						List<MeshManager.MeshPart> meshParts = treeFactory.meshManager.GetMeshParts (sproutMeshId);
						stWindMetaBuilder.SetSproutsWindData (treeFactory.previewTree, sproutMeshId, mesh, meshParts);
					}
				}
			} else {
				SetBranchesWindWeight (treeFactory.previewTree, treeFactory, updatePreviewTree);
				SetSproutWindWeight (treeFactory.previewTree, treeFactory, updatePreviewTree);
			}
			

			if (updatePreviewTree) {
				MeshFilter meshFilter = tree.obj.GetComponent<MeshFilter> ();
				meshFilter.sharedMesh.SetUVs (1, mergedMeshUV2s);
				meshFilter.sharedMesh.colors = mergedMeshColors;
			}

			sproutUV2.Clear ();
			sproutColor.Clear ();
		}
		/// <summary>
		/// Sets the tree UV2 values.
		/// </summary>
		/// <param name="tree">Tree.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		void SetBranchesWindWeight (BroccoTree tree, TreeFactory treeFactory, bool updatePreviewTree = false) {
			int branchMeshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
			Mesh mesh = treeFactory.meshManager.GetMesh (branchMeshId);
			if (mesh != null) {
				if (!meshIdToUV2.ContainsKey (branchMeshId)) {
					meshIdToUV2.Add (branchMeshId, new List<Vector4>());
				}
				if (!meshIdToUV.ContainsKey (branchMeshId)) { // TODO: Only if ST8
					meshIdToUV.Add (branchMeshId, new List<Vector4>());
				}
				mesh.GetUVs (0, meshIdToUV [branchMeshId]); // TODO: Only if ST8
				mesh.GetUVs (1, meshIdToUV2 [branchMeshId]);
				if (!meshIdToColor.ContainsKey (branchMeshId)) {
					meshIdToColor.Add (branchMeshId, new Color[0]);
				}
				meshIdToColor[branchMeshId] = mesh.colors;
				List<Vector4> _uvs = meshIdToUV[branchMeshId]; // TODO: Only if ST8
				List<Vector4> _uv2s = meshIdToUV2[branchMeshId];

				for (int i = 0; i < tree.branches.Count; i++) {
					SetSingleBranchWindWeight (ref _uv2s, meshIdToColor[branchMeshId], tree.branches [i]);
				}

				mesh.SetUVs (0, meshIdToUV[branchMeshId]); // TODO: Only if ST8
				mesh.SetUVs (1, meshIdToUV2[branchMeshId]);
				mesh.colors = meshIdToColor [branchMeshId];

				if (updatePreviewTree) {
					MeshFilter meshFilter = tree.obj.GetComponent<MeshFilter> ();
					if (mergedMeshUV2s.Count == 0 || mergedMeshColors.Length == 0) {
						meshFilter.sharedMesh.GetUVs (1, mergedMeshUV2s);
						mergedMeshColors = meshFilter.sharedMesh.colors;
					}
					// Get mesh vertex offset.
					int vertexOffset = treeFactory.meshManager.GetMergedMeshVertexOffset (branchMeshId);

					// Update UV2.
					mergedMeshUV2s.RemoveRange (vertexOffset, meshIdToUV2[branchMeshId].Count);
					mergedMeshUV2s.InsertRange (vertexOffset, meshIdToUV2[branchMeshId]);
					
					// Update Color.
					for (int j = 0; j < meshIdToColor[branchMeshId].Length; j++) {
						mergedMeshColors [vertexOffset + j] = meshIdToColor[branchMeshId] [j];
					}
				}
			}
		}
		/// <summary>
		/// Sets the branch wind weight.
		/// </summary>
		/// <param name="uv2s">UV2 array.</param>
		/// <param name="colors">Colors.</param>
		/// <param name="branch">Branch.</param>s
		void SetSingleBranchWindWeight (ref List<Vector4> localUV2s, 
			Color[] colors,
			BroccoTree.Branch branch) 
		{
			float currentPosition = -1;
			Vector4 windWeightUV2 = Vector4.zero;
			Color windWeightColor = Color.black;
			float green = 0;

			BranchMeshBuilder.BranchSkin branchSkin = windMetaBuilder.GetBranchSkin (branch.id);
			if (branchSkin != null) {
				int startIndex, vertexCount, segmentIndex;
				branchSkin.GetVertexStartAndCount (branch.id, out startIndex, out vertexCount);
				// For each vertex on this branch.
				for (int i = startIndex; i < startIndex + vertexCount; i++) {
					segmentIndex = branchSkin.vertexInfos [i].segmentIndex;
					if (currentPosition != branchSkin.positions [segmentIndex]) {
						currentPosition = branchSkin.positions [segmentIndex];
						windWeightUV2 = windMetaBuilder.GetUV2 (branch, currentPosition);
						windWeightColor = windMetaBuilder.GetColor (branch, currentPosition);
					}
					localUV2s [branchSkin.vertexOffset + i] = windWeightUV2;
					if (colors.Length > 0) colors [branchSkin.vertexOffset + i] = windWeightColor;
				}

				// For each sprout on the branch.
				for (int j = 0; j < branch.sprouts.Count; j++) {
						if (!sproutUV2.ContainsKey (branch.sprouts [j].helperSproutId)) {
							// Get the UV2.
							sproutUV2.Add (branch.sprouts [j].helperSproutId, 
								windMetaBuilder.GetUV2 (branch, branch.sprouts [j].position));
							// Get the Color.
							windWeightColor = windMetaBuilder.GetColor (branch, branch.sprouts [j].position);
							// Randomize the green color channel.
						green = Mathf.Clamp01 (Random.Range (windEffectElement.sproutTurbulence / 1.5f, windEffectElement.sproutTurbulence * 2f));
						windWeightColor.g = green;
						sproutColor.Add (branch.sprouts [j].helperSproutId, windWeightColor);
					}
				}
			}

			for (int i = 0; i < branch.branches.Count; i++) {
				SetSingleBranchWindWeight (ref localUV2s, colors, branch.branches[i]);
			}
		}
		/// <summary>
		/// Sets the branch wind weight.
		/// </summary>
		/// <param name="uv2s">UV2 array.</param>
		/// <param name="colors">Colors.</param>
		/// <param name="branch">Branch.</param>
		/// <param name="branchVertexInfos">Branch vertex infos.</param>
		void SetSingleBranchSTWindWeight (
			ref List<Vector4> localUVs, 
			ref List<Vector4> localUV2s, 
			Color[] colors,
			BroccoTree.Branch branch, 
			List<BranchMeshBuilder.VertexInfo> branchVertexInfos) 
		{
			/*
			float currentPosition = -1;
			Vector4 windWeightUV = Vector4.zero;
			Vector4 windWeightUV2 = Vector4.zero;
			Color windWeightColor = Color.black;
			float green = 0;

			// For each vertex on this branch.
			for (int i = 0; i < branchVertexInfos.Count; i++) {
				if (currentPosition != branchVertexInfos[i].position) {
					windWeightUV2 = stWindMetaBuilder.GetUV2 (branch, branchVertexInfos [i].position);
					windWeightColor = stWindMetaBuilder.GetColor (branch, branchVertexInfos [i].position);
					currentPosition = branchVertexInfos[i].position;
				}
				windWeightUV = stWindMetaBuilder.GetUV (branch, branchVertexInfos [i].position, localUVs[branchVertexInfos[i].vertexIndex]);
				localUVs [branchVertexInfos[i].vertexIndex] = windWeightUV;
				localUV2s [branchVertexInfos[i].vertexIndex] = windWeightUV2;
				colors [branchVertexInfos[i].vertexIndex] = windWeightColor;
			}

			// For each sprout on the branch.
			for (int j = 0; j < branch.sprouts.Count; j++) {
					if (!sproutUV2.ContainsKey (branch.sprouts [j].helperSproutId)) {
						// Get the UV2.
						sproutUV2.Add (branch.sprouts [j].helperSproutId, 
							stWindMetaBuilder.GetUV2 (branch, branch.sprouts [j].position));
						// Get the Color.
						windWeightColor = stWindMetaBuilder.GetColor (branch, branch.sprouts [j].position);
						// Randomize the green color channel.
					green = Mathf.Clamp01 (Random.Range (windEffectElement.sproutTurbulence / 1.5f, windEffectElement.sproutTurbulence * 2f));
					windWeightColor.g = green;
					sproutColor.Add (branch.sprouts [j].helperSproutId, windWeightColor);
				}
			}

			for (int i = 0; i < branch.branches.Count; i++) {
				if (vertexInfos.ContainsKey (branch.branches[i].id)) {
					SetSingleBranchSTWindWeight (ref localUVs, ref localUV2s, colors, 
						branch.branches[i], vertexInfos [branch.branches[i].id]);
				}
			}
			*/
		}
		/// <summary>
		/// Sets the sprout UV2 values.
		/// </summary>
		/// <param name="tree">Tree.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		void SetSproutWindWeight (BroccoTree tree, TreeFactory treeFactory, bool updatePreviewTree = false) {
			Dictionary<int, MeshManager.MeshData> meshDatas = 
				treeFactory.meshManager.GetMeshesDataOfType (MeshManager.MeshData.Type.Sprout);
			var meshDatasEnumerator = meshDatas.GetEnumerator ();
			int meshId;
			Mesh mesh;
			MeshFilter meshFilter = null;

			if (updatePreviewTree && mergedMeshUV2s.Count == 0) {
				meshFilter = tree.obj.GetComponent<MeshFilter> ();
				meshFilter.sharedMesh.GetUVs (1, mergedMeshUV2s);
				mergedMeshColors = meshFilter.sharedMesh.colors;
			}

			// Iterate through every ground sprout mesh.
			while (meshDatasEnumerator.MoveNext ()) {
				meshId = meshDatasEnumerator.Current.Key;
				if (treeFactory.meshManager.GetMesh (meshId) != null && treeFactory.meshManager.HasMeshParts (meshId)) {
					mesh = treeFactory.meshManager.GetMesh (meshId);
					// Set list with the original UV2s on the mesh.
					if (!meshIdToUV2.ContainsKey (meshId)) {
						meshIdToUV2.Add (meshId, new List<Vector4>());
					}
					mesh.GetUVs (1, meshIdToUV2 [meshId]);
					// Set array with the original colors on the mesh.
					if (!meshIdToColor.ContainsKey (meshId)) {
						meshIdToColor.Add (meshId, new Color[0]);
					}
					meshIdToColor[meshId] = mesh.colors;

					// Iterate through every individual sprout.
					Vector4 originalUV2s;
					List<MeshManager.MeshPart> meshParts = treeFactory.meshManager.GetMeshParts (meshId);
					for (int i = 0; i < meshParts.Count; i++) {
						meshParts[i].helperMeshId = meshId;
						float swayRandom = Random.Range (0.8f, 1.2f) * windEffectElement.sproutSway;
						if (sproutUV2.ContainsKey (meshParts[i].sproutId)) {
							for (int j = 0; j < meshParts[i].length; j++) {
								originalUV2s = meshIdToUV2 [meshId] [j + meshParts[i].startIndex];
								meshIdToUV2 [meshId] [j + meshParts[i].startIndex] = new Vector4 (
									sproutUV2 [meshParts[i].sproutId].x, 
									sproutUV2 [meshParts[i].sproutId].y + 
										(originalUV2s.z * windEffectElement.sproutSway / Random.Range(10f, 15f)),
									originalUV2s.z, 
									originalUV2s.w);
								meshIdToColor [meshId] [j + meshParts[i].startIndex] = sproutColor [meshParts[i].sproutId];
								meshIdToColor [meshId] [j + meshParts[i].startIndex].g *= originalUV2s.w;
								
								// Sprout swaying.
								meshIdToColor [meshId] [j + meshParts[i].startIndex].r = Mathf.Clamp01 ( 
									meshIdToColor [meshId] [j + meshParts[i].startIndex].r -
									(originalUV2s.z * swayRandom / 2.5f));

							}
						}
					}

					mesh.SetUVs (1, meshIdToUV2[meshId]);
					mesh.colors = meshIdToColor[meshId];

					if (updatePreviewTree) {
						int vertexOffset = treeFactory.meshManager.GetMergedMeshVertexOffset (meshId);
						mergedMeshUV2s.RemoveRange (vertexOffset, meshIdToUV2[meshId].Count);
						mergedMeshUV2s.InsertRange (vertexOffset, meshIdToUV2[meshId]);
						//mergedMeshColors = meshFilter.sharedMesh.colors;
						for (int j = 0; j < meshIdToColor[meshId].Length; j++) {
							mergedMeshColors [vertexOffset + j] = meshIdToColor[meshId] [j];
						}
					}
				}
			}
		}
		/// <summary>
		/// Sets the sprout UV2 values.
		/// </summary>
		/// <param name="tree">Tree.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		/*
		void SetSproutWindWeight (BroccoTree tree, TreeFactory treeFactory, bool updatePreviewTree = false) {
			Dictionary<int, MeshManager.MeshData> meshDatas = 
				treeFactory.meshManager.GetMeshesDataOfType (MeshManager.MeshData.Type.Sprout);
			var meshDatasEnumerator = meshDatas.GetEnumerator ();
			int meshId;
			Mesh mesh;
			MeshFilter meshFilter = null;

			if (updatePreviewTree) {
				meshFilter = tree.obj.GetComponent<MeshFilter> ();
				meshFilter.sharedMesh.GetUVs (1, mergedUV2s);
				mergedColors = meshFilter.sharedMesh.colors;
			}

			// Iterate through every ground sprout mesh.
			while (meshDatasEnumerator.MoveNext ()) {
				meshId = meshDatasEnumerator.Current.Key;
				if (treeFactory.meshManager.GetMesh (meshId) != null && treeFactory.meshManager.HasMeshParts (meshId)) {
					mesh = treeFactory.meshManager.GetMesh (meshId);
					if (!uv2s.ContainsKey (meshId)) {
						uv2s.Add (meshId, new List<Vector4>());
					}
					mesh.GetUVs (1, uv2s [meshId]);
					if (!colors.ContainsKey (meshId)) {
						colors.Add (meshId, new Color[0]);
					}
					colors[meshId] = mesh.colors;

					// Iterate through every individual sprout.
					Vector4 originalUV2s;
					List<MeshManager.MeshPart> meshParts = treeFactory.meshManager.GetMeshParts (meshId);
					for (int i = 0; i < meshParts.Count; i++) {
						meshParts[i].helperMeshId = meshId;
						if (sproutUV2.ContainsKey (meshParts[i].sproutId)) {
							for (int j = 0; j < meshParts[i].length; j++) {
								originalUV2s = uv2s [meshId] [j + meshParts[i].startIndex];
								uv2s [meshId] [j + meshParts[i].startIndex] = new Vector4 (sproutUV2 [meshParts[i].sproutId].x, 
									sproutUV2 [meshParts[i].sproutId].y, originalUV2s.z, originalUV2s.w);
								colors [meshId] [j + meshParts[i].startIndex] = sproutColor [meshParts[i].sproutId];
								colors [meshId] [j + meshParts[i].startIndex].g *= originalUV2s.w;
							}
						}
					}

					mesh.SetUVs (1, uv2s[meshId]);
					mesh.colors = colors[meshId];

					if (updatePreviewTree) {
						int vertexOffset = treeFactory.meshManager.GetMergedMeshVertexOffset (meshId);
						mergedUV2s.RemoveRange (vertexOffset, uv2s[meshId].Count);
						mergedUV2s.InsertRange (vertexOffset, uv2s[meshId]);
						mergedColors = meshFilter.sharedMesh.colors;
						for (int j = 0; j < colors[meshId].Length; j++) {
							mergedColors [vertexOffset + j] = colors[meshId] [j];
						}
					}
				}
			}

			if (updatePreviewTree) {
				meshFilter.sharedMesh.SetUVs (1, mergedUV2s);
				meshFilter.sharedMesh.colors = mergedColors;
			}
		}
		*/
	}
	#endregion
}