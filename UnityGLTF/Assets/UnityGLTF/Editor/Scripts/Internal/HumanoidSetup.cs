using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	internal static class HumanoidSetup
	{
	    private static MethodInfo _SetupHumanSkeleton;

	    internal static Avatar AddAvatarToGameObject(GameObject gameObject)
	    {
                SkinnedMeshRenderer[] skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (SkinnedMeshRenderer rend in skinnedMeshRenderers) 
                {
                	Transform[] meshBones = rend.bones;
	                bool[] meshBonesUsed = new bool[meshBones.Length];
        	        BoneWeight[] weights = rend.sharedMesh.boneWeights;
                	foreach (BoneWeight w in weights)
                	{
                    		if (w.weight0 != 0)
		                        meshBonesUsed[w.boneIndex0] = true;
                 		if (w.weight1 != 0)
		                        meshBonesUsed[w.boneIndex1] = true;
                    		if (w.weight2 != 0)
                        		meshBonesUsed[w.boneIndex2] = true;
                    		if (w.weight3 != 0)
                        		meshBonesUsed[w.boneIndex3] = true;
                	}
					for (int i = 0; i < meshBones.Length; i++)
					{
						if (meshBonesUsed[i])
						{
							Debug.LogError(meshBones[i].name);
						}
                	}
            	}

            	HumanDescription description = AvatarUtils.CreateHumanDescription(gameObject);
				var bones = description.human;

            	UnityEngine.Debug.LogError("(0) Bones " + bones.Length);

            	foreach (var bone in bones)
            	{
                	UnityEngine.Debug.LogError(bone.humanName + "->" + bone.boneName);
            	}

            	SetupHumanSkeleton(gameObject, ref bones, out var skeletonBones, out var hasTranslationDoF);
				description.human = bones;
				description.skeleton = skeletonBones;
				description.hasTranslationDoF = hasTranslationDoF;

	       	 	UnityEngine.Debug.LogError("(1) Bones " + skeletonBones.Length);

				foreach (var bone in skeletonBones)
				{
					UnityEngine.Debug.LogError(bone.name);
				}

				Avatar avatar = AvatarBuilder.BuildHumanAvatar(gameObject, description);
				avatar.name = "Avatar";

				if (!avatar.isValid)
				{
					Object.DestroyImmediate(avatar);
					return null;
				}

				var animator = gameObject.GetComponent<Animator>();
				if (animator) animator.avatar = avatar;
				return avatar;
	    }

	    private static void SetupHumanSkeleton(
		    GameObject modelPrefab,
		    ref HumanBone[] humanBoneMappingArray,
		    out SkeletonBone[] skeletonBones,
		    out bool hasTranslationDoF)
	    {
		    _SetupHumanSkeleton = typeof(AvatarSetupTool).GetMethod(nameof(SetupHumanSkeleton), (BindingFlags)(-1));
		    skeletonBones = Array.Empty<SkeletonBone>();
		    hasTranslationDoF = false;

		    _SetupHumanSkeleton?.Invoke(null, new object[]
		    {
			    modelPrefab,
			    humanBoneMappingArray,
			    skeletonBones,
			    hasTranslationDoF
		    });
	    }


	    // AvatarSetupTools
	    // AvatarBuilder.BuildHumanAvatar
	    // AvatarConfigurationStage.CreateStage
	    // AssetImporterTabbedEditor
	    // ModelImporterRigEditor

#if TESTING
	    [MenuItem("Tools/Copy Hierarchy Array")]
	    static void _Copy(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    var sb = new System.Text.StringBuilder();

		    void Traverse(Transform tr)
		    {
			    sb.AppendLine(tr.name);
			    foreach (Transform child in tr)
			    {
				    Traverse(child);
			    }
		    }

		    Traverse(gameObject.transform);
		    EditorGUIUtility.systemCopyBuffer = sb.ToString();
	    }

	    [MenuItem("Tools/Setup Humanoid")]
	    static void _Do(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    // SetupHumanSkeleton(go, ref humanBoneMappingArray, out var skeletonBones, out var hasTranslationDoF);
			AddAvatarToGameObject(gameObject);
	    }

	    [MenuItem("Tools/Open Avatar Editor")]
	    static void _OpenEditor(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    var avatar = gameObject.GetComponent<Animator>().avatar;
		    var e = (AvatarEditor) Editor.CreateEditor(avatar, typeof(AvatarEditor));
		    e.m_CameFromImportSettings = true;
		    Selection.activeObject = e;
		    e.SwitchToEditMode();
	    }
#endif
	}
}
