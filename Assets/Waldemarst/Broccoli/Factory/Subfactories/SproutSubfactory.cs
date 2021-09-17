using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Generator;
using Broccoli.Manager;

namespace Broccoli.Factory
{
    using Pipeline = Broccoli.Pipe.Pipeline;
    public class SproutSubfactory {
        #region Vars
        /// <summary>
        /// Internal TreeFactory instance to create branches. 
        /// It must be provided from a parent TreeFactory when initializing this subfactory.
        /// </summary>
        public TreeFactory treeFactory = null;
        /// <summary>
        /// Branch descriptor collection to handle values.
        /// </summary>
        BranchDescriptorCollection branchDescriptorCollection = null;
        /// <summary>
        /// Selected branch descriptor index.
        /// </summary>
        public int branchDescriptorIndex = 0;
        /// <summary>
        /// Saves the branch structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> branchLevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout A structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> sproutALevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout B structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> sproutBLevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout mesh instances representing sprout groups.
        /// </summary>
        List<SproutMesh> sproutMeshes = new List<SproutMesh> ();
        /// <summary>
        /// Branch mapper element to set branch textures.
        /// </summary>
        BranchMapperElement branchMapperElement = null;
        /// <summary>
        /// Branch girth element to set branch girth.
        /// </summary>
        GirthTransformElement girthTransformElement = null;
        /// <summary>
        /// Sprout mapper element to set sprout textures.
        /// </summary>
        SproutMapperElement sproutMapperElement = null;
        /// <summary>
        /// Branch bender element to set branch noise.
        /// </summary>
        BranchBenderElement branchBenderElement = null;
        /// <summary>
        /// Number of branch levels available on the pipeline.
        /// </summary>
        /// <value>Count of branch levels.</value>
        public int branchLevelCount { get; private set; }
        /// <summary>
        /// Number of sprout levels available on the pipeline.
        /// </summary>
        /// <value>Count of sprout levels.</value>
        public int sproutLevelCount { get; private set; }
        /// <summary>
        /// Enum describing the possible materials to apply to a preview.
        /// </summary>
        public enum MaterialMode {
            Composite,
            Albedo,
            Normals,
            Extras,
            Subsurface
        }
        #endregion

        #region Texture Vars
        TextureManager textureManager;
        #endregion

        #region Initialization and Termination
        /// <summary>
        /// Initializes the subfactory instance.
        /// </summary>
        /// <param name="treeFactory">TreeFactory instance to use to produce branches.</param>
        public void Init (TreeFactory treeFactory) {
            this.treeFactory = treeFactory;
            if (textureManager != null) {
                textureManager.Clear ();
            }
            textureManager = new TextureManager ();
        }
        /// <summary>
        /// Check if there is a valid tree factory assigned to this sprout factory.
        /// </summary>
        /// <returns>True is there is a valid TreeFactory instance.</returns>
        public bool HasValidTreeFactory () {
            return treeFactory != null;
        }
        /// <summary>
        /// Clears data from this instance.
        /// </summary>
        public void Clear () {
            treeFactory = null;
            branchLevels.Clear ();
            sproutALevels.Clear ();
            sproutBLevels.Clear ();
            sproutMeshes.Clear ();
            branchMapperElement = null;
            girthTransformElement = null;
            sproutMapperElement = null;
            branchBenderElement = null;
            textureManager.Clear ();
        }
        #endregion

        #region Pipeline Load and Analysis
        /// <summary>
        /// Loads a Broccoli pipeline to process branches.
        /// The branch is required to have from 1 to 3 hierarchy levels of branch nodes.
        /// </summary>
        /// <param name="pipeline">Pipeline to load on this subfactory.</param>
        /// <param name="pathToAsset">Path to the asset.</param>
        public void LoadPipeline (Pipeline pipeline, BranchDescriptorCollection branchDescriptorCollection, string pathToAsset) {
            if (treeFactory != null) {
                treeFactory.UnloadAndClearPipeline ();
                treeFactory.LoadPipeline (pipeline.Clone (), pathToAsset, true , true);
                AnalyzePipeline ();
                this.branchDescriptorCollection = branchDescriptorCollection;
                ProcessTextures ();
            }
        }
        /// <summary>
        /// Analyzes the loaded pipeline to index the branch and sprout levels to modify using the
        /// BranchDescriptor instance values.
        /// </summary>
        void AnalyzePipeline () {
            branchLevelCount = 0;
            sproutLevelCount = 0;
            branchLevels.Clear ();
            sproutALevels.Clear ();
            sproutBLevels.Clear ();
            sproutMeshes.Clear ();
            // Get structures for branches and sprouts.
            StructureGeneratorElement structureGeneratorElement = 
                (StructureGeneratorElement)treeFactory.localPipeline.GetElement (PipelineElement.ClassType.StructureGenerator);
            AnalyzePipelineStructure (structureGeneratorElement.rootStructureLevel);
            // Get sprout meshes.
            SproutMeshGeneratorElement sproutMeshGeneratorElement = 
                (SproutMeshGeneratorElement)treeFactory.localPipeline.GetElement (PipelineElement.ClassType.SproutMeshGenerator);
            if (sproutMeshGeneratorElement != null) {
                for (int i = 0; i < sproutMeshGeneratorElement.sproutMeshes.Count; i++) {
                    sproutMeshes.Add (sproutMeshGeneratorElement.sproutMeshes [i]);
                }
            }
            // Get the branch mapper to set textures for branches.
            branchMapperElement = 
                (BranchMapperElement)treeFactory.localPipeline.GetElement (PipelineElement.ClassType.BranchMapper);
            girthTransformElement = 
                (GirthTransformElement)treeFactory.localPipeline.GetElement (PipelineElement.ClassType.GirthTransform);
            sproutMapperElement = 
                (SproutMapperElement)treeFactory.localPipeline.GetElement (PipelineElement.ClassType.SproutMapper);
            branchBenderElement = 
                (BranchBenderElement)treeFactory.localPipeline.GetElement (PipelineElement.ClassType.BranchBender);
        }
        void AnalyzePipelineStructure (StructureGenerator.StructureLevel structureLevel) {
            if (!structureLevel.isSprout) {
                // Add branch structure level.
                branchLevels.Add (structureLevel);
                branchLevelCount++;
                // Add sprout A structure level.
                StructureGenerator.StructureLevel sproutStructureLevel = 
                    structureLevel.GetFirstSproutStructureLevel ();
                if (sproutStructureLevel != null) {
                    sproutALevels.Add (sproutStructureLevel);
                    sproutLevelCount++;
                }
                // Add sprout B structure level.
                sproutStructureLevel = structureLevel.GetSproutStructureLevel (1);
                if (sproutStructureLevel != null) {
                    sproutBLevels.Add (sproutStructureLevel);
                }
                // Send the next banch structure level to analysis if found.
                StructureGenerator.StructureLevel branchStructureLevel = 
                    structureLevel.GetFirstBranchStructureLevel ();
                if (branchStructureLevel != null) {
                    AnalyzePipelineStructure (branchStructureLevel);                    
                }
            }
        }
        public void UnloadPipeline () {
            if (treeFactory != null) {
                treeFactory.UnloadAndClearPipeline ();
            }
        }
        public void GeneratePreview () {
            treeFactory.ProcessPipelinePreview ();
            BranchDescriptor branchDescriptor = branchDescriptorCollection.branchDescriptors [branchDescriptorIndex];
            branchDescriptor.seed = treeFactory.localPipeline.seed;
        }
        public void RegeneratePreview (MaterialMode materialMode = MaterialMode.Composite) {
            BranchDescriptor branchDescriptor = branchDescriptorCollection.branchDescriptors [branchDescriptorIndex];
            treeFactory.localPipeline.seed = branchDescriptor.seed;
            treeFactory.ProcessPipelinePreview (null, true, true);
            if (materialMode != MaterialMode.Composite) {
                MeshRenderer meshRenderer = treeFactory.previewTree.obj.GetComponent<MeshRenderer>();
                Material[] compositeMaterials = compositeMaterials = meshRenderer.sharedMaterials;
                if (materialMode == MaterialMode.Albedo) { // Albedo
                    List<Material> albedoMats = new List<Material> ();
                    for (int i = 0; i < compositeMaterials.Length; i++) {
                        Material m = new Material (compositeMaterials[i]);
                        //m.shader = Shader.Find ("Unlit/Transparent Cutout");
                        m.shader = Shader.Find ("Hidden/Broccoli/SproutLabAlbedo");
                        albedoMats.Add (m);
                    }
                    meshRenderer.sharedMaterials = albedoMats.ToArray ();
                } else if (materialMode == MaterialMode.Normals) { // Normals
                    List<Material> normalsMats = new List<Material> ();
                    for (int i = 0; i < compositeMaterials.Length; i++) {
                        Material m = new Material (compositeMaterials[i]);
                        m.shader = Shader.Find ("Hidden/Broccoli/SproutLabNormals");
                        normalsMats.Add (m);
                    }
                    meshRenderer.sharedMaterials = normalsMats.ToArray ();
                }
            }
        }
        #endregion

        #region Pipeline Reflection
        public void BranchDescriptorCollectionToPipeline () {
            BranchDescriptor.BranchLevelDescriptor branchLD;
            StructureGenerator.StructureLevel branchSL;
            BranchDescriptor.SproutLevelDescriptor sproutALD;
            StructureGenerator.StructureLevel sproutASL;
            BranchDescriptor.SproutLevelDescriptor sproutBLD;
            StructureGenerator.StructureLevel sproutBSL;

            BranchDescriptor branchDescriptor = branchDescriptorCollection.branchDescriptors [branchDescriptorIndex];

            // Set seed.
            treeFactory.localPipeline.seed = branchDescriptor.seed;

            // Update branch girth.
            if (girthTransformElement != null) {
                girthTransformElement.minGirthAtBase = branchDescriptor.girthAtBase;
                girthTransformElement.maxGirthAtBase = branchDescriptor.girthAtBase;
                girthTransformElement.minGirthAtTop = branchDescriptor.girthAtTop;
                girthTransformElement.maxGirthAtTop = branchDescriptor.girthAtTop;
            }
            // Update branch noise.
            if (branchBenderElement) {
                branchBenderElement.noiseAtBase = branchDescriptor.noiseAtBase;
                branchBenderElement.noiseAtTop = branchDescriptor.noiseAtTop;
                branchBenderElement.noiseScaleAtBase = branchDescriptor.noiseScaleAtBase;
                branchBenderElement.noiseScaleAtTop = branchDescriptor.noiseScaleAtTop;
            }
            // Update branch descriptor active levels.
            for (int i = 0; i < branchLevels.Count; i++) {
                if (i <= branchDescriptor.activeLevels) {
                    branchLevels [i].enabled = true;
                } else {
                    branchLevels [i].enabled = false;
                }
            }
            // Update branch level descriptors.
            for (int i = 0; i < branchDescriptor.branchLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    branchLD = branchDescriptor.branchLevelDescriptors [i];
                    branchSL = branchLevels [i];
                    // Pass Values.
                    branchSL.minFrequency = branchLD.minFrequency;
                    branchSL.maxFrequency = branchLD.maxFrequency;
                    branchSL.minLengthAtBase = branchLD.minLengthAtBase;
                    branchSL.maxLengthAtBase = branchLD.maxLengthAtBase;
                    branchSL.minLengthAtTop = branchLD.minLengthAtTop;
                    branchSL.maxLengthAtTop = branchLD.maxLengthAtTop;
                    branchSL.minParallelAlignAtBase = branchLD.minParallelAlignAtBase;
                    branchSL.maxParallelAlignAtBase = branchLD.maxParallelAlignAtBase;
                    branchSL.minParallelAlignAtTop = branchLD.minParallelAlignAtTop;
                    branchSL.maxParallelAlignAtTop = branchLD.maxParallelAlignAtTop;
                    branchSL.minGravityAlignAtBase = branchLD.minGravityAlignAtBase;
                    branchSL.maxGravityAlignAtBase = branchLD.maxGravityAlignAtBase;
                    branchSL.minGravityAlignAtTop = branchLD.minGravityAlignAtTop;
                    branchSL.maxGravityAlignAtTop = branchLD.maxGravityAlignAtTop;
                }
            }
            // Update branch mapping textures.
            if (branchMapperElement != null) {
                branchMapperElement.mainTexture = branchDescriptorCollection.branchAlbedoTexture;
                branchMapperElement.normalTexture = branchDescriptorCollection.branchNormalTexture;
                branchMapperElement.mappingYDisplacement = branchDescriptorCollection.branchTextureYDisplacement;
            }
            // Update sprout A level descriptors.
            for (int i = 0; i < branchDescriptor.sproutALevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutALD = branchDescriptor.sproutALevelDescriptors [i];
                    sproutASL = sproutALevels [i];
                    // Pass Values.
                    sproutASL.enabled = sproutALD.isEnabled;
                    sproutASL.minFrequency = sproutALD.minFrequency;
                    sproutASL.maxFrequency = sproutALD.maxFrequency;
                    sproutASL.minParallelAlignAtBase = sproutALD.minParallelAlignAtBase;
                    sproutASL.maxParallelAlignAtBase = sproutALD.maxParallelAlignAtBase;
                    sproutASL.minParallelAlignAtTop = sproutALD.minParallelAlignAtTop;
                    sproutASL.maxParallelAlignAtTop = sproutALD.maxParallelAlignAtTop;
                    sproutASL.minGravityAlignAtBase = sproutALD.minGravityAlignAtBase;
                    sproutASL.maxGravityAlignAtBase = sproutALD.maxGravityAlignAtBase;
                    sproutASL.minGravityAlignAtTop = sproutALD.minGravityAlignAtTop;
                    sproutASL.maxGravityAlignAtTop = sproutALD.maxGravityAlignAtTop;
                }
            }
            // Update sprout A properties.
            if (sproutMeshes.Count > 0) {
                sproutMeshes [0].width = branchDescriptor.sproutASize;
                sproutMeshes [0].scaleAtBase = branchDescriptor.sproutAScaleAtBase;
                sproutMeshes [0].scaleAtTop = branchDescriptor.sproutAScaleAtTop;
            }
            // Update sprout mapping textures.
            if (sproutMapperElement != null) {
                sproutMapperElement.sproutMaps [0].colorVarianceMode = (SproutMap.ColorVarianceMode)branchDescriptorCollection.colorVarianceA;
                sproutMapperElement.sproutMaps [0].minColorShade = branchDescriptorCollection.minColorShadeA;
                sproutMapperElement.sproutMaps [0].maxColorShade = branchDescriptorCollection.maxColorShadeA;
                sproutMapperElement.sproutMaps [0].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutAMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutAMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (0, i);
                    sproutMapperElement.sproutMaps [0].sproutAreas.Add (sma);
                }
            }
            // Update sprout B level descriptors.
            for (int i = 0; i < branchDescriptor.sproutBLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutBLD = branchDescriptor.sproutBLevelDescriptors [i];
                    sproutBSL = sproutBLevels [i];
                    // Pass Values.
                    sproutBSL.enabled = sproutBLD.isEnabled;
                    sproutBSL.minFrequency = sproutBLD.minFrequency;
                    sproutBSL.maxFrequency = sproutBLD.maxFrequency;
                    sproutBSL.minParallelAlignAtBase = sproutBLD.minParallelAlignAtBase;
                    sproutBSL.maxParallelAlignAtBase = sproutBLD.maxParallelAlignAtBase;
                    sproutBSL.minParallelAlignAtTop = sproutBLD.minParallelAlignAtTop;
                    sproutBSL.maxParallelAlignAtTop = sproutBLD.maxParallelAlignAtTop;
                    sproutBSL.minGravityAlignAtBase = sproutBLD.minGravityAlignAtBase;
                    sproutBSL.maxGravityAlignAtBase = sproutBLD.maxGravityAlignAtBase;
                    sproutBSL.minGravityAlignAtTop = sproutBLD.minGravityAlignAtTop;
                    sproutBSL.maxGravityAlignAtTop = sproutBLD.maxGravityAlignAtTop;
                }
            }
            // Update sprout A properties.
            if (sproutMeshes.Count > 1) {
                sproutMeshes [1].width = branchDescriptor.sproutBSize;
                sproutMeshes [1].scaleAtBase = branchDescriptor.sproutBScaleAtBase;
                sproutMeshes [1].scaleAtTop = branchDescriptor.sproutBScaleAtTop;
            }
            // Update sprout mapping textures.
            if (sproutMapperElement != null && sproutMapperElement.sproutMaps.Count > 1) {
                sproutMapperElement.sproutMaps [1].colorVarianceMode = (SproutMap.ColorVarianceMode)branchDescriptorCollection.colorVarianceB;
                sproutMapperElement.sproutMaps [1].minColorShade = branchDescriptorCollection.minColorShadeB;
                sproutMapperElement.sproutMaps [1].maxColorShade = branchDescriptorCollection.maxColorShadeB;
                sproutMapperElement.sproutMaps [1].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutBMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutBMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (1, i);
                    sproutMapperElement.sproutMaps [1].sproutAreas.Add (sma);
                }
            }
        }
        #endregion

        #region Texture Processing
        public Texture2D GenerateSnapshopTexture (int snapshotIndex, MaterialMode materialMode, int width, int height, string texturePath = "") {
            if (snapshotIndex >= branchDescriptorCollection.branchDescriptors.Count) {
                Debug.LogWarning ("Could not generate branch snapshot texture. Index out of range.");
            } else {
                // Regenerate branch mesh and apply material mode.
                branchDescriptorIndex = snapshotIndex;
                RegeneratePreview (materialMode);
                // Build and save texture.
                TextureBuilder tb = new TextureBuilder ();
                // Get tree mesh.
                GameObject previewTree = treeFactory.previewTree.obj;
                tb.debugKeepCameraAfterUsage = true;
                tb.BeginUsage (previewTree);
                tb.textureSize = new Vector2 (width, height);
                Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up, texturePath);
                tb.EndUsage ();
                return sproutTexture;
            }
            return null;
        }
        public Texture2D GetSproutTexture (int group, int index) {
            string textureId = GetSproutTextureId (group, index);
            return textureManager.GetTexture (textureId);
        }
        Texture2D GetOriginalSproutTexture (int group, int index) {
            Texture2D texture = null;
            List<SproutMap.SproutMapArea> sproutMapAreas = null;
            if (group == 0) {
                sproutMapAreas = branchDescriptorCollection.sproutAMapAreas;
            } else if (group == 1) {
                sproutMapAreas = branchDescriptorCollection.sproutBMapAreas;
            }
            if (sproutMapAreas != null && sproutMapAreas.Count >= index) {
                texture = sproutMapAreas[index].texture;
            }
            return texture;
        }
        public void ProcessTextures () {
            textureManager.Clear ();
            string textureId;
            // Process Sprout A albedo textures.
            for (int i = 0; i < branchDescriptorCollection.sproutAMapAreas.Count; i++) {    
                Texture2D texture = ApplyTextureTransformations (
                    branchDescriptorCollection.sproutAMapAreas [i].texture, 
                    branchDescriptorCollection.sproutAMapDescriptors [i].alphaFactor);
                if (texture != null) {
                    textureId = GetSproutTextureId (0, i);
                    textureManager.AddOrReplaceTexture (textureId, texture);
                }
            }
            // Process Sprout B albedo textures.
            for (int i = 0; i < branchDescriptorCollection.sproutBMapAreas.Count; i++) {    
                Texture2D texture = ApplyTextureTransformations (
                    branchDescriptorCollection.sproutBMapAreas [i].texture, 
                    branchDescriptorCollection.sproutBMapDescriptors [i].alphaFactor);
                if (texture != null) {
                    textureId = GetSproutTextureId (1, i);
                    textureManager.AddOrReplaceTexture (textureId, texture);
                }
            }
        }
        public void ProcessTexture (int group, int index, float alpha) {
            string textureId = GetSproutTextureId (group, index);
            if (textureManager.HasTexture (textureId)) {
                Texture2D originalTexture = GetOriginalSproutTexture (group, index);
                Texture2D newTexture = ApplyTextureTransformations (originalTexture, alpha);
                newTexture.alphaIsTransparency = true;
                textureManager.AddOrReplaceTexture (textureId, newTexture, true);
                BranchDescriptorCollectionToPipeline ();
            }
        }
        Texture2D ApplyTextureTransformations (Texture2D originTexture, float alpha) {
            if (originTexture != null) {
                return textureManager.GetCopy (originTexture, alpha);
            }
            return null;
        }
        public string GetSproutTextureId (int group, int index) {
            return  "sprout_" + group + "_" + index;
        }
        #endregion
    }
}