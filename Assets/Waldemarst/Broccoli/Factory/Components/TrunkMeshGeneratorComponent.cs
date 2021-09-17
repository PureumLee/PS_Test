using Broccoli.Factory;

namespace Broccoli.Component
{
	/// <summary>
	/// Trunk mesh generator component.
	/// </summary>
	public class TrunkMeshGeneratorComponent : TreeFactoryComponent {
		#region Vars
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
			/*
			meshBuilder = BranchMeshBuilder.GetInstance ();
			meshBuilder.minPolygonSides = trunkMeshGeneratorElement.minPolygonSides;
			meshBuilder.maxPolygonSides = trunkMeshGeneratorElement.maxPolygonSides;
			meshBuilder.segmentAngle = trunkMeshGeneratorElement.segmentAngle;
			meshBuilder.useHardNormals = trunkMeshGeneratorElement.useHardNormals;

			meshBuilder.globalScale = treeFactory.treeFactoryPreferences.factoryScale;
			if (processControl.pass == 1 && !treeFactory.treeFactoryPreferences.prefabStrictLowPoly) {
				if (trunkMeshGeneratorElement.maxPolygonSides > 6) {
					meshBuilder.maxPolygonSides = 6;
				} else {
					meshBuilder.maxPolygonSides = trunkMeshGeneratorElement.maxPolygonSides;
				}
				if (trunkMeshGeneratorElement.minPolygonSides > 3) {
					meshBuilder.minPolygonSides = 3;
				} else {
					meshBuilder.minPolygonSides = trunkMeshGeneratorElement.minPolygonSides;
				}
			}
			*/
		}
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.Mesh;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear ()
		{
			base.Clear ();
		}
		#endregion

		#region Processing
		/// <summary>
		/// Process the tree according to the pipeline element.
		/// </summary>
		/// <param name="treeFactory">Parent tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="processControl">Process control.</param>
		public override bool Process (TreeFactory treeFactory, 
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl processControl = null) {
			/*
			if (pipelineElement != null && tree != null) {
				trunkMeshGeneratorElement = pipelineElement as BranchMeshGeneratorElement;
				PrepareParams (treeFactory, useCache, useLocalCache, processControl);
				tree.RecalculateNormals (0f);
				BuildMesh (treeFactory, processControl.pass);
				return true;
			}
			*/
			return false;
		}
		/// <summary>
		/// Removes the product of this component on the factory processing.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void Unprocess (TreeFactory treeFactory) {
			//treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Bark);
		}
		/// <summary>
		/// Builds the mesh.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="pass">Pass.</param>
		private void BuildMesh (TreeFactory treeFactory, int pass) {
			/*
			Mesh barkMesh;
			if (pass == 1) {
				meshBuilder.branchAngleTolerance = Mathf.Lerp (25f, 5f, trunkMeshGeneratorElement.minBranchCurveResolution);
			} else {
				meshBuilder.branchAngleTolerance = Mathf.Lerp (25f, 5f, trunkMeshGeneratorElement.maxBranchCurveResolution);
			}
			barkMesh = meshBuilder.MeshTree (tree);
			treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Bark);
			treeFactory.meshManager.RegisterBarkMesh (barkMesh);
			treeFactory.meshManager.AddMeshPart (0, 
				barkMesh.vertices.Length, 
				tree.obj.transform.position, 
				MeshManager.MeshData.Type.Bark);
			trunkMeshGeneratorElement.maxGirth = meshBuilder.maxGirth;
			trunkMeshGeneratorElement.minGirth = meshBuilder.minGirth;
			if (pass == 1) {
				trunkMeshGeneratorElement.verticesCountFirstPass = meshBuilder.verticesGenerated;
				trunkMeshGeneratorElement.trianglesCountFirstPass = meshBuilder.trianglesGenerated;
			} else {
				trunkMeshGeneratorElement.verticesCountSecondPass = meshBuilder.verticesGenerated;
				trunkMeshGeneratorElement.trianglesCountSecondPass = meshBuilder.trianglesGenerated;
			}
			if (treeFactory.treeFactoryPreferences.prefabStrictLowPoly) {
				trunkMeshGeneratorElement.showLODInfoLevel = 1;
			} else if (!treeFactory.treeFactoryPreferences.prefabUseLODGroups) {
				trunkMeshGeneratorElement.showLODInfoLevel = 2;
			} else {
				trunkMeshGeneratorElement.showLODInfoLevel = -1;
			}
			*/
		}
		#endregion

	}
}