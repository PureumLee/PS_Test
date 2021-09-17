using UnityEditor;
using UnityEngine;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Component;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Branch mapper node editor.
	/// </summary>
	[CustomEditor(typeof(BranchMapperNode))]
	public class BranchMapperNodeEditor : BaseNodeEditor {
		#region Vars
		/// <summary>
		/// The branch mapper node.
		/// </summary>
		public BranchMapperNode branchMapperNode;
		SerializedProperty propMaterialMode;
		SerializedProperty propCustomMaterial;
		SerializedProperty propMainTexture;
		SerializedProperty propNormalTexture;
		SerializedProperty propMappingXDisplacement;
		SerializedProperty propMappingYDisplacement;
		SerializedProperty propIsGirthSensitive;
		SerializedProperty propApplyMappingOffsetFromParent;
		SerializedProperty propColor;
		SerializedProperty propGlossiness;
		SerializedProperty propMetallic;
		SerializedProperty propDiffusionProfileSettings;
		#endregion

		#region Messages
		private static string MSG_MATERIAL_MODE = "Material mode to apply.";
		private static string MSG_CUSTOM_MATERIAL = "Material applied to the branches.";
		private static string MSG_MAIN_TEXTURE = "Main texture for the generated material.";
		private static string MSG_NORMAL_TEXTURE = "Normal map texture for the generated material.";
		private static string MSG_MAPPING_X_DISP = "Girth to be used at the base of the tree trunk.";
		private static string MSG_MAPPING_Y_DISP = "Girth to be used at the tip of a terminal branch.";
		private static string MSG_GIRTH_SENSITIVE = "UV mapping is smaller at lower values of girth on the branches.";
		private static string MSG_APPLY_PARENT_OFFSET = "Children branches get an UV mapping offset from their parent branch position.";
		private static string MSG_COLOR = "Color value to pass to the shader.";
		private static string MSG_GLOSSINESS = "Glossiness value to pass to the shader.";
		private static string MSG_METALLIC = "Metallic value to pass to the shader.";
		private static string MSG_DIFFUSION_PROFILE = "Diffusion Profile Settings asset for HDRP materials. Make sure this profile is listed at the HDRP Project Settings. " +
			"Broccoli can only assign a Diffusion Profile in Edit Mode, so it is not available when creating trees at runtime.";
		#endregion

		#region Events
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			branchMapperNode = target as BranchMapperNode;

			SetPipelineElementProperty ("branchMapperElement");

			propMaterialMode = GetSerializedProperty ("materialMode");
			propCustomMaterial = GetSerializedProperty ("customMaterial");
			propMainTexture = GetSerializedProperty ("mainTexture");
			propNormalTexture = GetSerializedProperty ("normalTexture");
			propMappingXDisplacement = GetSerializedProperty ("mappingXDisplacement");
			propMappingYDisplacement = GetSerializedProperty ("mappingYDisplacement");
			propIsGirthSensitive = GetSerializedProperty ("isGirthSensitive");
			propApplyMappingOffsetFromParent = GetSerializedProperty ("applyMappingOffsetFromParent");
			propColor = GetSerializedProperty ("color");
			propGlossiness = GetSerializedProperty ("glossiness");
			propMetallic = GetSerializedProperty ("metallic");
			propDiffusionProfileSettings = GetSerializedProperty ("diffusionProfileSettings");
		}
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		public override void OnInspectorGUI() {
			CheckUndoRequest ();

			UpdateSerialized ();

			// Log box.
			DrawLogBox ();

			int materialModeIndex = propMaterialMode.enumValueIndex;
			EditorGUILayout.PropertyField (propMaterialMode);
			ShowHelpBox (MSG_MATERIAL_MODE);
			EditorGUILayout.Space ();

			if (materialModeIndex == (int)BranchMapperElement.MaterialMode.Custom) {
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propCustomMaterial);
				ShowHelpBox (MSG_CUSTOM_MATERIAL);
				if (EditorGUI.EndChangeCheck () ||
				    materialModeIndex != propMaterialMode.enumValueIndex) {
					ApplySerialized ();
					UpdateComponent ((int)BranchMapperComponent.ComponentCommand.BuildMaterials, 
						GlobalSettings.processingDelayMedium);
					// TODO: update with pink material when no material is set.
					SetUndoControlCounter ();
				}
			} else if (materialModeIndex == (int)BranchMapperElement.MaterialMode.Texture) {
				bool mainTextureChanged = false;
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propMainTexture);
				if (EditorGUI.EndChangeCheck ()) {
					mainTextureChanged = true;
				}
				ShowHelpBox (MSG_MAIN_TEXTURE);

				bool normalTextureChanged = false;
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propNormalTexture);
				if (EditorGUI.EndChangeCheck ()) {
					normalTextureChanged = true;
				}
				ShowHelpBox (MSG_NORMAL_TEXTURE);

				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propColor);
				ShowHelpBox (MSG_COLOR);

				EditorGUILayout.Slider (propGlossiness, 0f, 1f, "Glossiness");
				ShowHelpBox (MSG_GLOSSINESS);

				EditorGUILayout.Slider (propMetallic, 0f, 1f, "Metallic");
				ShowHelpBox (MSG_METALLIC);

				if (ExtensionManager.isHDRP) {
					ScriptableObject former = (ScriptableObject)propDiffusionProfileSettings.objectReferenceValue;
					former = 
						(ScriptableObject)EditorGUILayout.ObjectField (
							"Diffusion Profile", 
							former, 
							System.Type.GetType ("UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings, Unity.RenderPipelines.HighDefinition.Runtime"), 
							false);
					if (former != (ScriptableObject)propDiffusionProfileSettings.objectReferenceValue) {
						propDiffusionProfileSettings.objectReferenceValue = former;
						mainTextureChanged = true;
					}
					ShowHelpBox (MSG_DIFFUSION_PROFILE);
				}
				if (materialModeIndex != propMaterialMode.enumValueIndex ||
				    mainTextureChanged || normalTextureChanged ||
					EditorGUI.EndChangeCheck ()) {
					ApplySerialized ();
					UpdateComponent ((int)BranchMapperComponent.ComponentCommand.BuildMaterials, 
						GlobalSettings.processingDelayLow);
					SetUndoControlCounter ();
				}
			}

			float textureXDisplacement = propMappingXDisplacement.floatValue;
			EditorGUILayout.Slider (propMappingXDisplacement, -5f, 5f, "Mapping X Displacement");
			ShowHelpBox (MSG_MAPPING_X_DISP);

			float textureYDisplacement = propMappingYDisplacement.floatValue;
			EditorGUILayout.Slider (propMappingYDisplacement, -5f, 5f, "Mapping Y Displacement");
			ShowHelpBox (MSG_MAPPING_Y_DISP);

			bool isGirthSensitive = propIsGirthSensitive.boolValue;
			EditorGUILayout.PropertyField (propIsGirthSensitive);
			ShowHelpBox (MSG_GIRTH_SENSITIVE);

			bool applyMappingOffsetFromParent = propApplyMappingOffsetFromParent.boolValue;
			EditorGUILayout.PropertyField (propApplyMappingOffsetFromParent);
			ShowHelpBox (MSG_APPLY_PARENT_OFFSET);

			if (textureXDisplacement != propMappingXDisplacement.floatValue ||
				textureYDisplacement != propMappingYDisplacement.floatValue ||
				isGirthSensitive != propIsGirthSensitive.boolValue ||
				applyMappingOffsetFromParent != propApplyMappingOffsetFromParent.boolValue) 
			{
				ApplySerialized ();
				UpdateComponent ((int)BranchMapperComponent.ComponentCommand.SetUVs, 
					GlobalSettings.processingDelayLow);
				SetUndoControlCounter ();
			}
			EditorGUILayout.Space ();

			// Field descriptors option.
			DrawFieldHelpOptions ();
		}
		#endregion
	}
}