using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityAnimation.EditorTools
{
    /// <summary>
    /// Rebinds an existing skinned mesh to another humanoid skeleton without
    /// replacing the target Animator, Avatar, controller, or animation setup.
    /// </summary>
    public sealed class HumanoidSkinTransferWindow : EditorWindow
    {
        private const string DefaultSourcePath = "Assets/Character/Gunner.fbx";
        private const string DefaultMaterialPath = "Assets/Character/Gunner.mat";
        private const string DefaultOutputFolder = "Assets/Character/Generated";
        private const string DefaultTargetName = "GunnerAV";
        private const string DefaultTargetRendererName = "STYLIZED-BASEMESH-BIGMALE-OBJ";

        private GameObject _sourceModel;
        private Animator _targetAnimator;
        private SkinnedMeshRenderer _targetRenderer;
        private Material _overrideMaterial;
        private bool _fitToTargetBindPose = true;
        private bool _disableOldRenderer = true;
        private bool _clearBlendShapes = true;
        private string _outputFolder = DefaultOutputFolder;
        private Vector2 _scroll;

        [MenuItem("Tools/Character/Humanoid Skin Transfer")]
        private static void OpenWindow()
        {
            HumanoidSkinTransferWindow window =
                GetWindow<HumanoidSkinTransferWindow>("Skin Transfer");
            window.minSize = new Vector2(460f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            FillProjectDefaults(false);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Humanoid Skin Transfer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Character/Gunner의 메시·UV·머티리얼을 유지하면서 GunnerAV의 기존 " +
                "Bip001 뼈대에 다시 바인딩합니다. Target의 Animator, Avatar, " +
                "Controller와 애니메이션 설정은 변경하지 않습니다.",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Source Visual", EditorStyles.boldLabel);
            _sourceModel = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Source Model", "메시와 UV를 가져올 FBX 모델"),
                _sourceModel,
                typeof(GameObject),
                false);
            _overrideMaterial = (Material)EditorGUILayout.ObjectField(
                new GUIContent("Material", "비워 두면 Source Renderer의 머티리얼을 사용합니다."),
                _overrideMaterial,
                typeof(Material),
                false);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Target Rig", EditorStyles.boldLabel);
            _targetAnimator = (Animator)EditorGUILayout.ObjectField(
                new GUIContent("Target Animator", "유지할 Animator와 Bip001 뼈대"),
                _targetAnimator,
                typeof(Animator),
                true);
            _targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                new GUIContent("Old Renderer", "변환 후 비활성화할 기존 몸체 Renderer"),
                _targetRenderer,
                typeof(SkinnedMeshRenderer),
                true);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Conversion", EditorStyles.boldLabel);
            _fitToTargetBindPose = EditorGUILayout.ToggleLeft(
                "Fit vertices to the target bind pose",
                _fitToTargetBindPose);
            _disableOldRenderer = EditorGUILayout.ToggleLeft(
                "Disable the old renderer after conversion",
                _disableOldRenderer);
            _clearBlendShapes = EditorGUILayout.ToggleLeft(
                "Clear source blend shapes (safer for a different skeleton)",
                _clearBlendShapes);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.HelpBox(
                "Source FBX의 Read/Write가 꺼져 있으면 변환 중에만 자동으로 켰다가 " +
                "완료 후 원래 설정으로 복구합니다. 변환 결과는 새 Mesh 에셋과 새 " +
                "SkinnedMeshRenderer로 생성되므로 원본 메시를 덮어쓰지 않습니다.",
                MessageType.None);

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Gunner Defaults", GUILayout.Height(28f)))
                {
                    FillProjectDefaults(true);
                }

                using (new EditorGUI.DisabledScope(!CanConvert()))
                {
                    if (GUILayout.Button("Convert And Apply", GUILayout.Height(28f)))
                    {
                        ConvertAndApply();
                    }
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "이 도구는 토폴로지나 UV를 다시 그리지 않습니다. 체형 차이가 큰 경우 " +
                "어깨·팔꿈치·무릎과 Twist 영역은 Blender에서 웨이트 보정이 필요할 수 있습니다.",
                MessageType.Warning);

            EditorGUILayout.EndScrollView();
        }

        private bool CanConvert()
        {
            return _sourceModel != null &&
                   _targetAnimator != null &&
                   _targetAnimator.gameObject.scene.IsValid() &&
                   !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private void FillProjectDefaults(bool selectWindow)
        {
            _sourceModel =
                AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSourcePath) ?? _sourceModel;
            _overrideMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath) ?? _overrideMaterial;

            Animator animator = FindSceneAnimator(DefaultTargetName);
            if (animator != null)
            {
                _targetAnimator = animator;
                SkinnedMeshRenderer[] renderers =
                    animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                _targetRenderer =
                    renderers.FirstOrDefault(r => r.gameObject.name == DefaultTargetRendererName) ??
                    renderers.FirstOrDefault();
            }

            if (selectWindow)
            {
                Repaint();
            }
        }

        private static Animator FindSceneAnimator(string gameObjectName)
        {
            Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
            foreach (Animator animator in animators)
            {
                if (animator == null ||
                    EditorUtility.IsPersistent(animator) ||
                    !animator.gameObject.scene.IsValid() ||
                    !animator.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (animator.gameObject.name == gameObjectName)
                {
                    return animator;
                }
            }

            return null;
        }

        private void ConvertAndApply()
        {
            if (!CanConvert())
            {
                EditorUtility.DisplayDialog(
                    "Skin Transfer",
                    "Source Model과 씬의 Target Animator를 먼저 지정하세요.",
                    "OK");
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(_sourceModel);
            if (string.IsNullOrEmpty(sourcePath))
            {
                EditorUtility.DisplayDialog(
                    "Skin Transfer",
                    "Source Model은 Project 창의 FBX 모델 에셋이어야 합니다.",
                    "OK");
                return;
            }

            ModelImporter sourceImporter =
                AssetImporter.GetAtPath(sourcePath) as ModelImporter;
            bool restoreReadWrite = sourceImporter != null && !sourceImporter.isReadable;
            GameObject sourceInstance = null;

            try
            {
                if (restoreReadWrite)
                {
                    sourceImporter.isReadable = true;
                    sourceImporter.SaveAndReimport();
                    _sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                }

                sourceInstance = Instantiate(_sourceModel);
                sourceInstance.name = _sourceModel.name + "_SkinTransferSource";
                sourceInstance.hideFlags = HideFlags.HideAndDontSave;
                sourceInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                sourceInstance.transform.localScale = Vector3.one;

                SkinnedMeshRenderer sourceRenderer =
                    ChooseSourceRenderer(sourceInstance);
                if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
                {
                    throw new InvalidOperationException(
                        "Source Model에서 SkinnedMeshRenderer를 찾지 못했습니다.");
                }

                Animator sourceAnimator =
                    sourceInstance.GetComponentInChildren<Animator>(true);
                ConversionContext context = BuildConversionContext(
                    sourceInstance.transform,
                    sourceRenderer,
                    sourceAnimator,
                    _targetAnimator,
                    _targetRenderer);

                ValidateUsedBones(sourceRenderer.sharedMesh, sourceRenderer.bones, context);

                string outputPath = CreateConvertedMeshAsset(
                    sourceRenderer,
                    context,
                    _fitToTargetBindPose,
                    _clearBlendShapes);

                Mesh convertedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
                GameObject convertedObject = CreateTargetRendererObject(
                    sourceRenderer,
                    convertedMesh,
                    context);

                if (_disableOldRenderer && _targetRenderer != null)
                {
                    Undo.RecordObject(_targetRenderer, "Disable old character renderer");
                    _targetRenderer.enabled = false;
                    EditorUtility.SetDirty(_targetRenderer);
                }

                EditorSceneManager.MarkSceneDirty(_targetAnimator.gameObject.scene);
                Selection.activeGameObject = convertedObject;
                EditorGUIUtility.PingObject(convertedMesh);

                string message =
                    "변환이 완료되었습니다.\n\n" +
                    "Mapped bones: " + context.MappedBoneCount + "\n" +
                    "Fallback bones: " + context.FallbackBoneCount + "\n" +
                    "Mesh asset: " + outputPath + "\n\n" +
                    "GunnerAV의 Animator, Avatar, Controller는 변경하지 않았습니다.";
                Debug.Log("[Humanoid Skin Transfer] " + message, convertedObject);
                EditorUtility.DisplayDialog("Skin Transfer Complete", message, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Skin Transfer Failed",
                    exception.Message,
                    "OK");
            }
            finally
            {
                if (sourceInstance != null)
                {
                    DestroyImmediate(sourceInstance);
                }

                if (restoreReadWrite)
                {
                    ModelImporter importer =
                        AssetImporter.GetAtPath(sourcePath) as ModelImporter;
                    if (importer != null)
                    {
                        importer.isReadable = false;
                        importer.SaveAndReimport();
                    }

                    _sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                }
            }
        }

        private static SkinnedMeshRenderer ChooseSourceRenderer(GameObject sourceInstance)
        {
            SkinnedMeshRenderer[] renderers =
                sourceInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            return renderers
                .Where(renderer => renderer.sharedMesh != null)
                .OrderByDescending(renderer => renderer.sharedMesh.vertexCount)
                .FirstOrDefault();
        }

        private sealed class ConversionContext
        {
            public Transform TargetRoot;
            public Transform[] SourceBones;
            public Transform[] TargetBones;
            public Matrix4x4[] TargetBindWorld;
            public Dictionary<Transform, Transform> SourceToTarget;
            public int MappedBoneCount;
            public int FallbackBoneCount;
        }

        private static ConversionContext BuildConversionContext(
            Transform sourceRoot,
            SkinnedMeshRenderer sourceRenderer,
            Animator sourceAnimator,
            Animator targetAnimator,
            SkinnedMeshRenderer targetRenderer)
        {
            Transform[] sourceBones = sourceRenderer.bones;
            if (sourceBones == null || sourceBones.Length == 0)
            {
                throw new InvalidOperationException(
                    "Source Renderer에 스키닝 본 정보가 없습니다.");
            }

            Transform[] targetTransforms =
                targetAnimator.GetComponentsInChildren<Transform>(true);
            Dictionary<string, Transform> targetByName =
                BuildTargetNameLookup(targetTransforms);
            Dictionary<Transform, Transform> mapping =
                new Dictionary<Transform, Transform>();

            AddHumanoidMappings(sourceAnimator, targetAnimator, mapping);

            int directMappings = mapping.Count;
            foreach (Transform sourceBone in sourceBones)
            {
                if (sourceBone == null || mapping.ContainsKey(sourceBone))
                {
                    continue;
                }

                Transform targetBone = FindTargetByName(sourceBone.name, targetByName);
                if (targetBone != null)
                {
                    mapping[sourceBone] = targetBone;
                }
            }

            directMappings = mapping.Count;
            int fallbackMappings = 0;

            foreach (Transform sourceBone in sourceBones)
            {
                if (sourceBone == null || mapping.ContainsKey(sourceBone))
                {
                    continue;
                }

                Transform ancestor = sourceBone.parent;
                while (ancestor != null && ancestor != sourceRoot.parent)
                {
                    Transform mappedAncestor;
                    if (mapping.TryGetValue(ancestor, out mappedAncestor))
                    {
                        mapping[sourceBone] = mappedAncestor;
                        fallbackMappings++;
                        break;
                    }

                    ancestor = ancestor.parent;
                }
            }

            Transform targetFallback =
                FindTargetByName("Bip001 Pelvis", targetByName) ??
                targetAnimator.transform;

            Transform[] mappedBones = new Transform[sourceBones.Length];
            Matrix4x4[] targetBindWorld = new Matrix4x4[sourceBones.Length];
            Dictionary<Transform, Matrix4x4> targetBindLookup =
                BuildTargetBindPoseLookup(targetRenderer);

            for (int i = 0; i < sourceBones.Length; i++)
            {
                Transform sourceBone = sourceBones[i];
                Transform mappedBone;
                if (sourceBone == null ||
                    !mapping.TryGetValue(sourceBone, out mappedBone) ||
                    mappedBone == null)
                {
                    mappedBone = targetFallback;
                }

                mappedBones[i] = mappedBone;

                Matrix4x4 bindWorld;
                targetBindWorld[i] =
                    targetBindLookup.TryGetValue(mappedBone, out bindWorld)
                        ? bindWorld
                        : mappedBone.localToWorldMatrix;
            }

            return new ConversionContext
            {
                TargetRoot = targetAnimator.transform,
                SourceBones = sourceBones,
                TargetBones = mappedBones,
                TargetBindWorld = targetBindWorld,
                SourceToTarget = mapping,
                MappedBoneCount = directMappings,
                FallbackBoneCount = fallbackMappings
            };
        }

        private static void AddHumanoidMappings(
            Animator sourceAnimator,
            Animator targetAnimator,
            IDictionary<Transform, Transform> mapping)
        {
            if (!IsValidHuman(sourceAnimator) || !IsValidHuman(targetAnimator))
            {
                return;
            }

            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                HumanBodyBones humanBone = (HumanBodyBones)i;
                Transform sourceBone = sourceAnimator.GetBoneTransform(humanBone);
                Transform targetBone = targetAnimator.GetBoneTransform(humanBone);
                if (sourceBone != null && targetBone != null)
                {
                    mapping[sourceBone] = targetBone;
                }
            }
        }

        private static bool IsValidHuman(Animator animator)
        {
            return animator != null &&
                   animator.avatar != null &&
                   animator.avatar.isValid &&
                   animator.avatar.isHuman;
        }

        private static Dictionary<string, Transform> BuildTargetNameLookup(
            IEnumerable<Transform> targetTransforms)
        {
            Dictionary<string, Transform> result =
                new Dictionary<string, Transform>();

            foreach (Transform target in targetTransforms)
            {
                string normalized = NormalizeName(target.name);
                if (!result.ContainsKey(normalized))
                {
                    result.Add(normalized, target);
                }

                string withoutPrefix = RemoveTargetRigPrefix(normalized);
                if (!result.ContainsKey(withoutPrefix))
                {
                    result.Add(withoutPrefix, target);
                }
            }

            return result;
        }

        private static Transform FindTargetByName(
            string sourceName,
            IReadOnlyDictionary<string, Transform> targetByName)
        {
            string normalizedSource = NormalizeName(sourceName);
            Transform direct;
            if (targetByName.TryGetValue(normalizedSource, out direct))
            {
                return direct;
            }

            string sourceWithoutPrefix = RemoveSourceRigPrefix(normalizedSource);
            if (targetByName.TryGetValue(sourceWithoutPrefix, out direct))
            {
                return direct;
            }

            string[] aliases;
            if (!BoneAliases.TryGetValue(sourceWithoutPrefix, out aliases))
            {
                return null;
            }

            foreach (string alias in aliases)
            {
                if (targetByName.TryGetValue(NormalizeName(alias), out direct))
                {
                    return direct;
                }
            }

            return null;
        }

        private static readonly Dictionary<string, string[]> BoneAliases =
            new Dictionary<string, string[]>
            {
                { "hips", new[] { "Bip001 Pelvis", "Pelvis" } },
                { "pelvis", new[] { "Bip001 Pelvis", "Pelvis" } },
                { "spine", new[] { "Bip001 Spine" } },
                { "spine01", new[] { "Bip001 Spine1" } },
                { "spine1", new[] { "Bip001 Spine1" } },
                { "chest", new[] { "Bip001 Spine1" } },
                { "spine02", new[] { "Bip001 Spine2" } },
                { "spine2", new[] { "Bip001 Spine2" } },
                { "upperchest", new[] { "Bip001 Spine2" } },
                { "neck", new[] { "Bip001 Neck" } },
                { "head", new[] { "Bip001 Head" } },
                { "headend", new[] { "Bip001 Head" } },
                { "headfront", new[] { "Bip001 Head" } },
                { "leftshoulder", new[] { "Bip001 L Clavicle" } },
                { "leftarm", new[] { "Bip001 L UpperArm" } },
                { "leftupperarm", new[] { "Bip001 L UpperArm" } },
                { "leftforearm", new[] { "Bip001 L Forearm" } },
                { "leftlowerarm", new[] { "Bip001 L Forearm" } },
                { "lefthand", new[] { "Bip001 L Hand" } },
                { "rightshoulder", new[] { "Bip001 R Clavicle" } },
                { "rightarm", new[] { "Bip001 R UpperArm" } },
                { "rightupperarm", new[] { "Bip001 R UpperArm" } },
                { "rightforearm", new[] { "Bip001 R Forearm" } },
                { "rightlowerarm", new[] { "Bip001 R Forearm" } },
                { "righthand", new[] { "Bip001 R Hand" } },
                { "leftupleg", new[] { "Bip001 L Thigh" } },
                { "leftthigh", new[] { "Bip001 L Thigh" } },
                { "leftleg", new[] { "Bip001 L Calf" } },
                { "leftcalf", new[] { "Bip001 L Calf" } },
                { "leftfoot", new[] { "Bip001 L Foot" } },
                { "lefttoebase", new[] { "Bip001 L Toe0" } },
                { "lefttoe", new[] { "Bip001 L Toe0" } },
                { "rightupleg", new[] { "Bip001 R Thigh" } },
                { "rightthigh", new[] { "Bip001 R Thigh" } },
                { "rightleg", new[] { "Bip001 R Calf" } },
                { "rightcalf", new[] { "Bip001 R Calf" } },
                { "rightfoot", new[] { "Bip001 R Foot" } },
                { "righttoebase", new[] { "Bip001 R Toe0" } },
                { "righttoe", new[] { "Bip001 R Toe0" } }
            };

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        private static string RemoveSourceRigPrefix(string value)
        {
            return value
                .Replace("mixamorig", string.Empty)
                .Replace("armature", string.Empty);
        }

        private static string RemoveTargetRigPrefix(string value)
        {
            return value.StartsWith("bip001", StringComparison.Ordinal)
                ? value.Substring("bip001".Length)
                : value;
        }

        private static Dictionary<Transform, Matrix4x4> BuildTargetBindPoseLookup(
            SkinnedMeshRenderer targetRenderer)
        {
            Dictionary<Transform, Matrix4x4> result =
                new Dictionary<Transform, Matrix4x4>();

            if (targetRenderer == null ||
                targetRenderer.sharedMesh == null ||
                targetRenderer.bones == null)
            {
                return result;
            }

            try
            {
                Transform[] targetBones = targetRenderer.bones;
                Matrix4x4[] bindPoses = targetRenderer.sharedMesh.bindposes;
                int count = Mathf.Min(targetBones.Length, bindPoses.Length);

                for (int i = 0; i < count; i++)
                {
                    if (targetBones[i] == null)
                    {
                        continue;
                    }

                    Matrix4x4 boneBindWorld =
                        targetRenderer.localToWorldMatrix * bindPoses[i].inverse;
                    result[targetBones[i]] = boneBindWorld;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Humanoid Skin Transfer] Target bind pose를 읽지 못해 현재 " +
                    "씬의 본 포즈를 사용합니다.\n" + exception.Message);
            }

            return result;
        }

        private static void ValidateUsedBones(
            Mesh sourceMesh,
            Transform[] sourceBones,
            ConversionContext context)
        {
            BoneWeight[] weights = sourceMesh.boneWeights;
            HashSet<int> usedBoneIndices = new HashSet<int>();

            foreach (BoneWeight weight in weights)
            {
                AddUsedBone(usedBoneIndices, weight.boneIndex0, weight.weight0);
                AddUsedBone(usedBoneIndices, weight.boneIndex1, weight.weight1);
                AddUsedBone(usedBoneIndices, weight.boneIndex2, weight.weight2);
                AddUsedBone(usedBoneIndices, weight.boneIndex3, weight.weight3);
            }

            List<string> unmapped = new List<string>();
            foreach (int index in usedBoneIndices)
            {
                if (index < 0 || index >= sourceBones.Length || sourceBones[index] == null)
                {
                    unmapped.Add("Invalid bone index " + index);
                    continue;
                }

                if (!context.SourceToTarget.ContainsKey(sourceBones[index]))
                {
                    unmapped.Add(sourceBones[index].name);
                }
            }

            if (unmapped.Count > 0)
            {
                throw new InvalidOperationException(
                    "가중치가 사용된 Source 본을 GunnerAV에 매핑하지 못했습니다:\n" +
                    string.Join(", ", unmapped.Distinct()));
            }
        }

        private static void AddUsedBone(
            ISet<int> usedBoneIndices,
            int boneIndex,
            float weight)
        {
            if (weight > 0.0001f)
            {
                usedBoneIndices.Add(boneIndex);
            }
        }

        private string CreateConvertedMeshAsset(
            SkinnedMeshRenderer sourceRenderer,
            ConversionContext context,
            bool fitToTargetBindPose,
            bool clearBlendShapes)
        {
            Mesh sourceMesh = sourceRenderer.sharedMesh;
            Mesh convertedMesh = Instantiate(sourceMesh);
            convertedMesh.name =
                _targetAnimator.gameObject.name + "_" + sourceMesh.name + "_Converted";

            Vector3[] sourceVertices = sourceMesh.vertices;
            BoneWeight[] weights = sourceMesh.boneWeights;
            Matrix4x4 sourceRendererToWorld = sourceRenderer.localToWorldMatrix;
            Matrix4x4 targetRootWorldToLocal = context.TargetRoot.worldToLocalMatrix;
            Matrix4x4 sourceToTarget =
                targetRootWorldToLocal * sourceRendererToWorld;

            Matrix4x4[] fitMatrices = new Matrix4x4[context.SourceBones.Length];
            Matrix4x4[] convertedBindPoses =
                new Matrix4x4[context.SourceBones.Length];
            Matrix4x4 targetRendererBindWorld = context.TargetRoot.localToWorldMatrix;

            for (int i = 0; i < context.SourceBones.Length; i++)
            {
                Transform sourceBone = context.SourceBones[i];
                Matrix4x4 sourceBoneWorldToLocal =
                    sourceBone != null
                        ? sourceBone.worldToLocalMatrix
                        : sourceRendererToWorld.inverse;

                fitMatrices[i] =
                    targetRootWorldToLocal *
                    context.TargetBindWorld[i] *
                    sourceBoneWorldToLocal *
                    sourceRendererToWorld;

                convertedBindPoses[i] =
                    context.TargetBindWorld[i].inverse *
                    targetRendererBindWorld;
            }

            Vector3[] convertedVertices = new Vector3[sourceVertices.Length];
            for (int vertexIndex = 0; vertexIndex < sourceVertices.Length; vertexIndex++)
            {
                Vector3 sourceVertex = sourceVertices[vertexIndex];
                if (!fitToTargetBindPose)
                {
                    convertedVertices[vertexIndex] =
                        sourceToTarget.MultiplyPoint3x4(sourceVertex);
                    continue;
                }

                BoneWeight weight = weights[vertexIndex];
                Vector3 convertedVertex = Vector3.zero;
                float totalWeight = 0f;

                AccumulateVertex(
                    ref convertedVertex,
                    ref totalWeight,
                    sourceVertex,
                    fitMatrices,
                    weight.boneIndex0,
                    weight.weight0);
                AccumulateVertex(
                    ref convertedVertex,
                    ref totalWeight,
                    sourceVertex,
                    fitMatrices,
                    weight.boneIndex1,
                    weight.weight1);
                AccumulateVertex(
                    ref convertedVertex,
                    ref totalWeight,
                    sourceVertex,
                    fitMatrices,
                    weight.boneIndex2,
                    weight.weight2);
                AccumulateVertex(
                    ref convertedVertex,
                    ref totalWeight,
                    sourceVertex,
                    fitMatrices,
                    weight.boneIndex3,
                    weight.weight3);

                if (totalWeight < 0.999f)
                {
                    convertedVertex +=
                        sourceToTarget.MultiplyPoint3x4(sourceVertex) *
                        (1f - totalWeight);
                }

                convertedVertices[vertexIndex] = convertedVertex;
            }

            convertedMesh.vertices = convertedVertices;
            convertedMesh.bindposes = convertedBindPoses;

            if (clearBlendShapes && convertedMesh.blendShapeCount > 0)
            {
                convertedMesh.ClearBlendShapes();
            }

            convertedMesh.RecalculateBounds();
            convertedMesh.RecalculateNormals();
            try
            {
                convertedMesh.RecalculateTangents();
            }
            catch (Exception)
            {
                // A mesh without a suitable UV channel cannot recalculate tangents.
            }

            EnsureAssetFolder(_outputFolder);
            string safeMeshName = MakeSafeFileName(convertedMesh.name);
            string outputPath = AssetDatabase.GenerateUniqueAssetPath(
                _outputFolder.TrimEnd('/') + "/" + safeMeshName + ".asset");
            AssetDatabase.CreateAsset(convertedMesh, outputPath);
            AssetDatabase.SaveAssets();

            return outputPath;
        }

        private static void AccumulateVertex(
            ref Vector3 output,
            ref float totalWeight,
            Vector3 sourceVertex,
            IReadOnlyList<Matrix4x4> fitMatrices,
            int boneIndex,
            float weight)
        {
            if (weight <= 0.0001f ||
                boneIndex < 0 ||
                boneIndex >= fitMatrices.Count)
            {
                return;
            }

            output +=
                fitMatrices[boneIndex].MultiplyPoint3x4(sourceVertex) * weight;
            totalWeight += weight;
        }

        private GameObject CreateTargetRendererObject(
            SkinnedMeshRenderer sourceRenderer,
            Mesh convertedMesh,
            ConversionContext context)
        {
            GameObject convertedObject =
                new GameObject(_sourceModel.name + "_ConvertedVisual");
            Undo.RegisterCreatedObjectUndo(convertedObject, "Create converted character visual");

            Transform convertedTransform = convertedObject.transform;
            convertedTransform.SetParent(context.TargetRoot, false);
            convertedTransform.localPosition = Vector3.zero;
            convertedTransform.localRotation = Quaternion.identity;
            convertedTransform.localScale = Vector3.one;

            SkinnedMeshRenderer convertedRenderer =
                Undo.AddComponent<SkinnedMeshRenderer>(convertedObject);
            convertedRenderer.sharedMesh = convertedMesh;
            convertedRenderer.bones = context.TargetBones;
            convertedRenderer.rootBone =
                FindMappedRootBone(sourceRenderer, context) ??
                context.TargetBones.FirstOrDefault() ??
                context.TargetRoot;
            convertedRenderer.quality = sourceRenderer.quality;
            convertedRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;
            convertedRenderer.skinnedMotionVectors = sourceRenderer.skinnedMotionVectors;
            convertedRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            convertedRenderer.receiveShadows = sourceRenderer.receiveShadows;
            convertedRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            convertedRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;

            Bounds bounds = convertedMesh.bounds;
            bounds.extents *= 1.5f;
            convertedRenderer.localBounds = bounds;

            if (_overrideMaterial != null)
            {
                int materialCount = Mathf.Max(1, convertedMesh.subMeshCount);
                Material[] materials = new Material[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    materials[i] = _overrideMaterial;
                }

                convertedRenderer.sharedMaterials = materials;
            }
            else
            {
                convertedRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            }

            EditorUtility.SetDirty(convertedRenderer);
            return convertedObject;
        }

        private static Transform FindMappedRootBone(
            SkinnedMeshRenderer sourceRenderer,
            ConversionContext context)
        {
            if (sourceRenderer.rootBone == null)
            {
                return null;
            }

            Transform targetRootBone;
            return context.SourceToTarget.TryGetValue(
                sourceRenderer.rootBone,
                out targetRootBone)
                ? targetRootBone
                : null;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) ||
                !folderPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Output Folder는 Assets 아래의 경로여야 합니다.");
            }

            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string MakeSafeFileName(string value)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                builder.Append(invalidCharacters.Contains(character) ? '_' : character);
            }

            return builder.ToString();
        }
    }
}
