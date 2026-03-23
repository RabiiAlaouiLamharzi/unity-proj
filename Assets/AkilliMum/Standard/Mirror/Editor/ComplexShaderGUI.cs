using System;
using UnityEngine;
//using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;

namespace UnityEditor
{
    internal class ComplexShaderGUI : ShaderGUI
    {
        private enum WorkflowMode
        {
            Specular,
            Metallic,
            Dielectric
        }

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        private static class Styles
        {
            public static GUIContent uvSetLabel = EditorGUIUtility.TrTextContent("UV Set");

            public static GUIContent albedoText = EditorGUIUtility.TrTextContent("Albedo", "Albedo (RGB) and Transparency (A)");
            public static GUIContent alphaCutoffText = EditorGUIUtility.TrTextContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent specularMapText = EditorGUIUtility.TrTextContent("Specular", "Specular (RGB) and Smoothness (A)");
            public static GUIContent metallicMapText = EditorGUIUtility.TrTextContent("Metallic", "Metallic (R) and Smoothness (A)");
            public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness value");
            public static GUIContent smoothnessScaleText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessMapChannelText = EditorGUIUtility.TrTextContent("Source", "Smoothness texture and channel");
            public static GUIContent highlightsText = EditorGUIUtility.TrTextContent("Specular Highlights", "Specular Highlights");
            public static GUIContent reflectionsText = EditorGUIUtility.TrTextContent("Reflections", "Glossy Reflections");
            public static GUIContent normalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");
            public static GUIContent heightMapText = EditorGUIUtility.TrTextContent("Height Map", "Height Map (G)");
            public static GUIContent occlusionText = EditorGUIUtility.TrTextContent("Occlusion", "Occlusion (G)");
            public static GUIContent emissionText = EditorGUIUtility.TrTextContent("Color", "Emission (RGB)");
            public static GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Detail Mask", "Mask for Secondary Maps (A)");
            public static GUIContent detailAlbedoText = EditorGUIUtility.TrTextContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
            public static GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");

            //my guicontent
            //public static GUIContent reflectionIntensityText = EditorGUIUtility.TrTextContent("Intensity", "Reflection intensity");
            //public static GUIContent disableGIText = EditorGUIUtility.TrTextContent("Disable GI", "Disables GI to create (mimic) full reflective mirrors");
            //public static GUIContent usefresnelText = EditorGUIUtility.TrTextContent("Use Fresnel", "Uses fresnel to calculate reflection. So reflective object becomes more reflective on wide angles");
            //public static GUIContent disableRPText = EditorGUIUtility.TrTextContent("Disable Reflection Probes", "Disables reflection probes (deferred rendering) to not mix with our reflection");
            //public static GUIContent mixRefProbesText = EditorGUIUtility.TrTextContent("Mix Reflection Probes", "Mixes reflection probes with dynamic reflection. So you can only reflect dynamic-moving objects and get other info from pre-baked reflection probes to gain speed. But it can be used only for indoor environments, cause outdoor (skybox etc) may create unwanted square shapes around the corners");
            //public static GUIContent mipLevelText = EditorGUIUtility.TrTextContent("Mip Level", "Mip level of the texture to be used. Warning: Mip Mapping must be enabled on MirrorManager script!");
            //public static GUIContent refractionTexText = EditorGUIUtility.TrTextContent("Refraction Map", "Refraction normal map to mimic refraction on reflection");
            //public static GUIContent refractionValueText = EditorGUIUtility.TrTextContent("\tAmount", "Refraction amount");
            //public static GUIContent waveTexText = EditorGUIUtility.TrTextContent("Wave Map", "Wave normal map to mimic waves on reflection");
            //public static GUIContent waveSizeText = EditorGUIUtility.TrTextContent("\tSize", "Size of the waves");
            //public static GUIContent waveDistortionText = EditorGUIUtility.TrTextContent("\tDistortion", "Distortion amount of the waves");
            //public static GUIContent waveSpeedText = EditorGUIUtility.TrTextContent("\tSpeed", "Speed of the waves according to the time");
            //public static GUIContent enableDepthText = EditorGUIUtility.TrTextContent("Enable Simple Depth", "Enables simple depth for reflection. So you can create fade off effect on reflection (Warning: Depth must be enabled on MirrorManager script)!");
            //public static GUIContent depthCutOffText = EditorGUIUtility.TrTextContent("\tCut Off", "Depth cut off for fade off");
            //public static GUIContent enableDepthBlurText = EditorGUIUtility.TrTextContent("Enable Depth Blur", "Enables advanced depth calculations for reflection. So you can create fade off, blur on horizontal and vertical axises etc. on reflection (Warning: Depth Blur must be enabled on MirrorManager script)!");
            //public static GUIContent maskTexText = EditorGUIUtility.TrTextContent("Alpha Mask Map", "Alpha mask texture to create some transparent areas on reflection (like puddles)");
            //public static GUIContent maskCutOffText = EditorGUIUtility.TrTextContent("\tCut-Off", "Cut-Off value to set start-end alpha fade-off");
            //public static GUIContent maskEdgeDarknessText = EditorGUIUtility.TrTextContent("\tEdge Darkness", "To create darkness on edges of the mask to mimic intensity (like mud)");
            //public static GUIContent maskTilingText = EditorGUIUtility.TrTextContent("\tTiling", "Tiling of the mask texture to fit on object as you want");

            //my labels
            public static string reflectionPropText = "Reflection";
            
            public static string primaryMapsText = "Main Maps";
            public static string secondaryMapsText = "Secondary Maps";
            public static string forwardText = "Forward Rendering Options";
            public static string renderingMode = "Rendering Mode";
            public static string advancedText = "Advanced Options";
            public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
        }

        //my attributes
        MaterialProperty reflectionIntensity = null;
        MaterialProperty disableGI = null;
        MaterialProperty useFresnel = null;
        MaterialProperty useObjectUV = null;
        //MaterialProperty disableRP = null;
        //MaterialProperty mixRP = null;
        MaterialProperty mipLevel = null;
        MaterialProperty refractionTex = null;
        MaterialProperty refractionValue = null;
        MaterialProperty waveTex = null;
        //MaterialProperty enableWave = null;
        MaterialProperty waveSize = null;
        MaterialProperty waveDistortion = null;
        MaterialProperty waveSpeed = null;
        MaterialProperty enableSimpleDepth = null;
        MaterialProperty simpleDepthCutoff = null;
        MaterialProperty enableDepthBlur = null;
        MaterialProperty maskTex;
        MaterialProperty enableMask;
        MaterialProperty maskCutOff;
        MaterialProperty maskEdgeDarkness;
        MaterialProperty maskTiling;

        MaterialProperty blendMode = null;
        MaterialProperty albedoMap = null;
        MaterialProperty albedoColor = null;
        MaterialProperty alphaCutoff = null;
        MaterialProperty specularMap = null;
        MaterialProperty specularColor = null;
        MaterialProperty metallicMap = null;
        MaterialProperty metallic = null;
        MaterialProperty smoothness = null;
        MaterialProperty smoothnessScale = null;
        MaterialProperty smoothnessMapChannel = null;
        MaterialProperty highlights = null;
        MaterialProperty reflections = null;
        MaterialProperty bumpScale = null;
        MaterialProperty bumpMap = null;
        MaterialProperty occlusionStrength = null;
        MaterialProperty occlusionMap = null;
        MaterialProperty heigtMapScale = null;
        MaterialProperty heightMap = null;
        MaterialProperty emissionColorForRendering = null;
        MaterialProperty emissionMap = null;
        MaterialProperty detailMask = null;
        MaterialProperty detailAlbedoMap = null;
        MaterialProperty detailNormalMapScale = null;
        MaterialProperty detailNormalMap = null;
        MaterialProperty uvSetSecondary = null;

        MaterialEditor m_MaterialEditor;
        WorkflowMode m_WorkflowMode = WorkflowMode.Specular;

        bool m_FirstTimeApply = true;

        public void FindProperties(MaterialProperty[] props)
        {
            //my definitions
            reflectionIntensity = FindProperty("_ReflectionIntensity", props);
            disableGI = FindProperty("_DisableGI", props);
            useFresnel = FindProperty("_UseFresnel", props);
            useObjectUV = FindProperty("_UseObjectUV", props);
            //disableRP = FindProperty("_DisableRP", props);
            //mixRP = FindProperty("_MixBlackColor", props);
            mipLevel = FindProperty("_LODLevel", props);
            refractionTex = FindProperty("_RefractionTex", props);
            refractionValue = FindProperty("_ReflectionRefraction", props);
            //enableWave = FindProperty("_EnableWave", props);
            waveTex = FindProperty("_WaveNoiseTex", props); 
            waveSize = FindProperty("_WaveSize", props);
            waveDistortion = FindProperty("_WaveDistortion", props);
            waveSpeed = FindProperty("_WaveSpeed", props);
            enableSimpleDepth = FindProperty("_EnableSimpleDepth", props);
            simpleDepthCutoff = FindProperty("_SimpleDepthCutoff", props);
            enableDepthBlur = FindProperty("_EnableDepthBlur", props);
            maskTex = FindProperty("_MaskTex", props);
            //enableMask = FindProperty("_EnableMask", props);
            maskCutOff = FindProperty("_MaskCutoff", props);
            maskEdgeDarkness = FindProperty("_MaskEdgeDarkness", props);
            maskTiling = FindProperty("_MaskTiling", props);

            blendMode = FindProperty("_Mode", props);
            albedoMap = FindProperty("_MainTex", props);
            albedoColor = FindProperty("_Color", props);
            alphaCutoff = FindProperty("_Cutoff", props);
            specularMap = FindProperty("_SpecGlossMap", props, false);
            specularColor = FindProperty("_SpecColor", props, false);
            metallicMap = FindProperty("_MetallicGlossMap", props, false);
            metallic = FindProperty("_Metallic", props, false);
            if (specularMap != null && specularColor != null)
                m_WorkflowMode = WorkflowMode.Specular;
            else if (metallicMap != null && metallic != null)
                m_WorkflowMode = WorkflowMode.Metallic;
            else
                m_WorkflowMode = WorkflowMode.Dielectric;
            smoothness = FindProperty("_Glossiness", props);
            smoothnessScale = FindProperty("_GlossMapScale", props, false);
            smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", props, false);
            highlights = FindProperty("_SpecularHighlights", props, false);
            reflections = FindProperty("_GlossyReflections", props, false);
            bumpScale = FindProperty("_BumpScale", props);
            bumpMap = FindProperty("_BumpMap", props);
            heigtMapScale = FindProperty("_Parallax", props);
            heightMap = FindProperty("_ParallaxMap", props);
            occlusionStrength = FindProperty("_OcclusionStrength", props);
            occlusionMap = FindProperty("_OcclusionMap", props);
            emissionColorForRendering = FindProperty("_EmissionColor", props);
            emissionMap = FindProperty("_EmissionMap", props);
            detailMask = FindProperty("_DetailMask", props);
            detailAlbedoMap = FindProperty("_DetailAlbedoMap", props);
            detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
            detailNormalMap = FindProperty("_DetailNormalMap", props);
            uvSetSecondary = FindProperty("_UVSec", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            m_MaterialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a standard shader.
            // Do this before any GUI code has been issued to prevent layout issues in subsequent GUILayout statements (case 780071)
            if (m_FirstTimeApply)
            {
                MaterialChanged(material, m_WorkflowMode);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material, props);
        }

        public void ShaderPropertiesGUI(Material material, MaterialProperty[] properties)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                BlendModePopup();

                //my area
                DoReflectionArea(material, properties);

                // Primary properties
                GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
                DoAlbedoArea(material);
                DoSpecularMetallicArea();
                DoNormalArea();
                m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
                DoEmissionArea(material);
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
                if (EditorGUI.EndChangeCheck())
                    emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

                EditorGUILayout.Space();

                // Secondary properties
                GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
                m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
                m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);

                // Third properties
                GUILayout.Label(Styles.forwardText, EditorStyles.boldLabel);
                if (highlights != null)
                    m_MaterialEditor.ShaderProperty(highlights, Styles.highlightsText);
                if (reflections != null)
                    m_MaterialEditor.ShaderProperty(reflections, Styles.reflectionsText);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendMode.targets)
                    MaterialChanged((Material)obj, m_WorkflowMode);
            }

            EditorGUILayout.Space();

            // NB renderqueue editor is not shown on purpose: we want to override it based on blend mode
            GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
            m_MaterialEditor.EnableInstancingField();
            m_MaterialEditor.DoubleSidedGIField();
        }

        internal void DetermineWorkflow(MaterialProperty[] props)
        {
            if (FindProperty("_SpecGlossMap", props, false) != null && FindProperty("_SpecColor", props, false) != null)
                m_WorkflowMode = WorkflowMode.Specular;
            else if (FindProperty("_MetallicGlossMap", props, false) != null && FindProperty("_Metallic", props, false) != null)
                m_WorkflowMode = WorkflowMode.Metallic;
            else
                m_WorkflowMode = WorkflowMode.Dielectric;
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
                return;
            }

            BlendMode blendMode = BlendMode.Opaque;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                blendMode = BlendMode.Cutout;
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                blendMode = BlendMode.Fade;
            }
            material.SetFloat("_Mode", (float)blendMode);

            DetermineWorkflow(MaterialEditor.GetMaterialProperties(new Material[] { material }));
            MaterialChanged(material, m_WorkflowMode);
        }

        void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        //private bool MenuLocallyCorrection = true;
        private bool MenuReflection = true;

        void DoReflectionArea(Material material, MaterialProperty[] properties)
        {
            Material targetMat = material;


            //MaterialProperty _EnviCubeMapBaked = ShaderGUI.FindProperty("_EnviCubeMapBaked", properties);
            //materialEditorIn.ShaderProperty(_EnviCubeMapBaked, "Custom Cube Map");

            //#region Locally Correction

            //{
            //    MaterialProperty _EnableLocallyCorrection =
            //        ShaderGUI.FindProperty("_EnableLocallyCorrection", properties);

            //    MenuLocallyCorrection = EditorGUILayout.BeginFoldoutHeaderGroup(MenuLocallyCorrection,
            //        new GUIContent { text = "Locally Correction" });

            //    bool enableLCRS = false;
            //    if (MenuLocallyCorrection)
            //    {
            //        enableLCRS = _EnableLocallyCorrection.floatValue > 0.5f;
            //        enableLCRS = EditorGUILayout.Toggle("Enable", enableLCRS);
            //        _EnableLocallyCorrection.floatValue = enableLCRS ? 1.0f : -1.0f;

            //        //if (realTimeRef)
            //        //{
            //        //    MaterialProperty _EnviCubeIntensity = ShaderGUI.FindProperty("_EnviCubeIntensity", properties);
            //        //    materialEditorIn.ShaderProperty(_EnviCubeIntensity, "Intensity");

            //        //    MaterialProperty _EnviCubeSmoothness = ShaderGUI.FindProperty("_EnviCubeSmoothness", properties);
            //        //    materialEditorIn.ShaderProperty(_EnviCubeSmoothness, "Smoothness");
            //        //}
            //    }

            //    EditorGUILayout.EndFoldoutHeaderGroup();

            //    if (enableLCRS)
            //    {
            //        targetMat.EnableKeyword("_LOCALLYCORRECTION");
            //        //Debug.Log("Enabled LCRS");
            //    }
            //    else
            //    {
            //        targetMat.DisableKeyword("_LOCALLYCORRECTION");
            //        //Debug.Log("Disabled LCRS");
            //    }
            //}

            //#endregion


            #region Reflection
            {
                MenuReflection = EditorGUILayout.BeginFoldoutHeaderGroup(MenuReflection,
                    new GUIContent { text = "Reflection" });

                MaterialProperty _DisableGI = ShaderGUI.FindProperty("_DisableGI", properties);
                bool disableGI = _DisableGI.floatValue > 0.5f;

                MaterialProperty _UseFresnel = ShaderGUI.FindProperty("_UseFresnel", properties);
                bool useFresnel = _UseFresnel.floatValue > 0.5f;

                MaterialProperty _UseObjectUV = ShaderGUI.FindProperty("_UseObjectUV", properties);
                bool useObjectUV = _UseObjectUV.floatValue > 0.5f;

                MaterialProperty _EnableSimpleDepth = ShaderGUI.FindProperty("_EnableSimpleDepth", properties);
                bool useDepth = _EnableSimpleDepth.floatValue > 0.5f;

                MaterialProperty _EnableDepthBlur = ShaderGUI.FindProperty("_EnableDepthBlur", properties);
                //MaterialProperty _MixBlackColor = ShaderGUI.FindProperty("_MixBlackColor", properties);
                bool useDepthBlur = _EnableDepthBlur.floatValue > 0.5f;

                if (MenuReflection)
                {
                    //intensity
                    MaterialProperty _ReflectionIntensity = ShaderGUI.FindProperty("_ReflectionIntensity", properties);
                    float intensity = _ReflectionIntensity.floatValue;
                    intensity = EditorGUILayout.Slider(new GUIContent
                    {
                        text = "Intensity",
                        tooltip = "Intensity of reflection, like '0' = no reflection, '1' = full mirror"
                    }, intensity, 0f, 1f);
                    _ReflectionIntensity.floatValue = intensity;


                    //GI
                    disableGI = EditorGUILayout.Toggle(new GUIContent
                    {
                        text = "Disable GI",
                        tooltip =
                            "Disables GI to create full mirror, so it does not affected by GI and creates perfect reflection"
                    }, disableGI);
                    _DisableGI.floatValue = disableGI ? 1.0f : -1.0f;

                    //Fresnel Like Reflection
                    useFresnel = EditorGUILayout.Toggle(new GUIContent
                    {
                        text = "Use Fresnel",
                        tooltip =
                            "Uses fresnel to calculate reflection. So reflective object becomes more reflective on wide angles"
                    }, useFresnel);
                    _UseFresnel.floatValue = useFresnel ? 1.0f : -1.0f;

                    //use object uv not screen space uv
                    useObjectUV = EditorGUILayout.Toggle(new GUIContent
                    {
                        text = "Use Object UV",
                        tooltip =
                            "Uses object UV, not screen space UV, it is useful for static mirrors like car mirrors if UV's are correctly set!"
                    }, useObjectUV);
                    _UseObjectUV.floatValue = useObjectUV ? 1.0f : -1.0f;

                    //mip level
                    MaterialProperty _LODLevel = ShaderGUI.FindProperty("_LODLevel", properties);
                    m_MaterialEditor.ShaderProperty(_LODLevel, new GUIContent
                    {
                        text = "Mip Level",
                        tooltip =
                            "Mip level of the texture to be used. Warning: Mip Mapping must be enabled on MirrorManager script!"
                    });

                    //refraction
                    MaterialProperty _RefractionTex = ShaderGUI.FindProperty("_RefractionTex", properties);
                    m_MaterialEditor.TexturePropertySingleLine(
                        new GUIContent
                        {
                            text = "Refraction Map",
                            tooltip = "Refraction normal map to mimic refraction on reflection"
                        }, _RefractionTex);
                    //MaterialProperty _EnableRefraction = ShaderGUI.FindProperty("_EnableRefraction", properties);
                    if (_RefractionTex.textureValue != null)
                    {
                        //_EnableRefraction.floatValue = 1;
                        targetMat.EnableKeyword("_AKMU_MIRROR_REFRACTION");

                        MaterialProperty _ReflectionRefraction =
                            ShaderGUI.FindProperty("_ReflectionRefraction", properties);
                        m_MaterialEditor.ShaderProperty(_ReflectionRefraction, new GUIContent
                        {
                            text = "\tIntensity",
                            tooltip = "Refraction intensity to refract more or less"
                        });
                    }
                    else
                    {
                        //_EnableRefraction.floatValue = -1;
                        targetMat.DisableKeyword("_AKMU_MIRROR_REFRACTION");
                    }

                    //wave
                    MaterialProperty _WaveNoiseTex = ShaderGUI.FindProperty("_WaveNoiseTex", properties);
                    m_MaterialEditor.TexturePropertySingleLine(
                        new GUIContent
                        {
                            text = "Wave Map",
                            tooltip = "Wave normal map to mimic waves on reflection"
                        }, _WaveNoiseTex);
                    //MaterialProperty _EnableWave = ShaderGUI.FindProperty("_EnableWave", properties);
                    if (_WaveNoiseTex.textureValue != null)
                    {
                        //_EnableWave.floatValue = 1;
                        targetMat.EnableKeyword("_AKMU_MIRROR_WAVE");

                        MaterialProperty _WaveSize =
                            ShaderGUI.FindProperty("_WaveSize", properties);
                        m_MaterialEditor.ShaderProperty(_WaveSize, new GUIContent
                        {
                            text = "\tSize",
                            tooltip = "Size of the waves"
                        });

                        MaterialProperty _WaveDistortion =
                            ShaderGUI.FindProperty("_WaveDistortion", properties);
                        m_MaterialEditor.ShaderProperty(_WaveDistortion, new GUIContent
                        {
                            text = "\tDistortion",
                            tooltip = "Distortion amount of the waves"
                        });

                        MaterialProperty _WaveSpeed =
                            ShaderGUI.FindProperty("_WaveSpeed", properties);
                        m_MaterialEditor.ShaderProperty(_WaveSpeed, new GUIContent
                        {
                            text = "\tSpeed",
                            tooltip = "Speed of the waves according to the time"
                        });
                    }
                    else
                    {
                        //_EnableWave.floatValue = -1;
                        targetMat.DisableKeyword("_AKMU_MIRROR_WAVE");
                    }

                    //[HideInInspector]_EnableRipple("Enable Ripples", Float) = -1.0
                    //    [HideInInspector]_RippleTex("Ripple", 2D) = "bump" { }
                    //[HideInInspector]_RippleSize("Ripple Size", Float) = 2.0
                    //    [HideInInspector]_RippleRefraction("Ripple Refraction", Float) = 0.02
                    //    [HideInInspector]_RippleDensity("Ripple Density", Float) = 1.0
                    //    [HideInInspector]_RippleSpeed("Ripple Speed", Float) = 0.3
                    //ripple
                    MaterialProperty _RippleTex = ShaderGUI.FindProperty("_RippleTex", properties);
                    m_MaterialEditor.TexturePropertySingleLine(
                        new GUIContent
                        {
                            text = "Ripple Map",
                            tooltip = "Ripple normal map to mimic ripples on reflection"
                        }, _RippleTex);
                    //MaterialProperty _EnableRipple = ShaderGUI.FindProperty("_EnableRipple", properties);
                    if (_RippleTex.textureValue != null)
                    {
                        //_EnableRipple.floatValue = 1;
                        targetMat.EnableKeyword("_AKMU_MIRROR_RIPPLE");

                        MaterialProperty _RippleSize =
                            ShaderGUI.FindProperty("_RippleSize", properties);
                        m_MaterialEditor.ShaderProperty(_RippleSize, new GUIContent
                        {
                            text = "\tSize",
                            tooltip = "Size of the ripples"
                        });

                        MaterialProperty _RippleRefraction =
                            ShaderGUI.FindProperty("_RippleRefraction", properties);
                        m_MaterialEditor.ShaderProperty(_RippleRefraction, new GUIContent
                        {
                            text = "\tDistortion",
                            tooltip = "Distortion amount of the ripples"
                        });

                        MaterialProperty _RippleSpeed =
                            ShaderGUI.FindProperty("_RippleSpeed", properties);
                        m_MaterialEditor.ShaderProperty(_RippleSpeed, new GUIContent
                        {
                            text = "\tSpeed",
                            tooltip = "Speed of the ripples according to the time"
                        });

                        MaterialProperty _RippleDensity =
                            ShaderGUI.FindProperty("_RippleDensity", properties);
                        m_MaterialEditor.ShaderProperty(_RippleDensity, new GUIContent
                        {
                            text = "\tDensity",
                            tooltip = "Density of the ripples (hard-soft etc.)"
                        });
                    }
                    else
                    {
                        //_EnableRipple.floatValue = -1;
                        targetMat.DisableKeyword("_AKMU_MIRROR_RIPPLE");
                    }

                    //depth
                    useDepth = EditorGUILayout.Toggle(new GUIContent
                    {
                        text = "Depth",
                        tooltip =
                            "Use reflection depth to mimic some fade-off on reflection. Warning: Depth must be enabled on MirrorManager script too!"
                    }, useDepth);
                    _EnableSimpleDepth.floatValue = useDepth ? 1.0f : -1.0f;
                    if (useDepth)
                    {
                        MaterialProperty _SimpleDepthCutoff =
                            ShaderGUI.FindProperty("_SimpleDepthCutoff", properties);
                        m_MaterialEditor.ShaderProperty(_SimpleDepthCutoff, new GUIContent
                        {
                            text = "\tCut-Off",
                            tooltip = "Depth cut-off value to set start-end reflection fade-off"
                        });
                    }

                    //depth blur
                    useDepthBlur = EditorGUILayout.Toggle(new GUIContent
                    {
                        text = "Depth Blur",
                        tooltip =
                            "Use advanced depth calculations to mimic some fade-off and blur on reflection. Warning: Depth Blur must be enabled on MirrorManager script too!"
                    }, useDepthBlur);
                    //_EnableDepthBlur.floatValue = _MixBlackColor.floatValue = useDepthBlur ? 1.0f : -1.0f;
                    _EnableDepthBlur.floatValue = useDepthBlur ? 1.0f : -1.0f;

                    //mask
                    MaterialProperty _MaskTex = ShaderGUI.FindProperty("_MaskTex", properties);
                    m_MaterialEditor.TexturePropertySingleLine(
                        new GUIContent
                        {
                            text = "Alpha Mask Map",
                            tooltip = "Alpha mask texture to create some transparent areas on reflection (like puddles)"
                        }, _MaskTex);
                    //MaterialProperty _EnableMask = ShaderGUI.FindProperty("_EnableMask", properties);
                    if (_MaskTex.textureValue != null)
                    {
                        //_EnableMask.floatValue = 1;
                        targetMat.EnableKeyword("_AKMU_MIRROR_MASK");

                        MaterialProperty _MaskCutoff =
                            ShaderGUI.FindProperty("_MaskCutoff", properties);
                        m_MaterialEditor.ShaderProperty(_MaskCutoff, new GUIContent
                        {
                            text = "\tCut-Off",
                            tooltip = "Cut-Off value to set start-end alpha fade-off"
                        });

                        MaterialProperty _MaskEdgeDarkness =
                            ShaderGUI.FindProperty("_MaskEdgeDarkness", properties);
                        m_MaterialEditor.ShaderProperty(_MaskEdgeDarkness, new GUIContent
                        {
                            text = "\tEdge Darkness",
                            tooltip = "To create darkness on edges of the mask to mimic intensity (like mud)"
                        });

                        MaterialProperty _MaskTiling =
                            ShaderGUI.FindProperty("_MaskTiling", properties);
                        m_MaterialEditor.ShaderProperty(_MaskTiling, new GUIContent
                        {
                            text = "\tTiling",
                            tooltip = "Tiling of the mask texture to fit on object as you want"
                        });
                    }
                    else
                    {
                        //_EnableMask.floatValue = -1;
                        targetMat.DisableKeyword("_AKMU_MIRROR_MASK");
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();

                //enable disable GI with keyword
                if (disableGI)
                {
                    targetMat.EnableKeyword("_FULLMIRROR");
                }
                else
                {
                    targetMat.DisableKeyword("_FULLMIRROR");
                }
            }

            #endregion
             
            //return;

            /////OLDDDD CODEEEE
            ////main header
            //GUILayout.Label(Styles.reflectionPropText, EditorStyles.boldLabel);

            ////intensity
            //m_MaterialEditor.ShaderProperty(reflectionIntensity, Styles.reflectionIntensityText);

            ////disable GI
            //{
            //    var gi = disableGI.floatValue > 0.5f;
            //    gi = EditorGUILayout.Toggle(Styles.disableGIText, gi);
            //    disableGI.floatValue = gi ? 1.0f : 0.0f;
            //    if (gi)
            //    {
            //        material.EnableKeyword("_FULLMIRROR");
            //    }
            //    else
            //    {
            //        material.DisableKeyword("_FULLMIRROR");
            //    }
            //}

            ////disable reflection probes
            //{
            //    var rp = disableRP.floatValue > 0.5f;
            //    rp = EditorGUILayout.Toggle(Styles.disableRPText, rp);
            //    disableRP.floatValue = rp ? 1.0f : 0.0f;
            //    if (rp)
            //    {
            //        material.EnableKeyword("_DISABLEPROBES");
            //    }
            //    else
            //    {
            //        material.DisableKeyword("_DISABLEPROBES");
            //    }
            //}

            ////mix reflection probes
            //{
            //    var mix = mixRP.floatValue > 0.5f;
            //    mix = EditorGUILayout.Toggle(Styles.mixRefProbesText, mix);
            //    mixRP.floatValue = mix ? 1.0f : 0.0f;
            //}

            ////mip level
            //{
            //    m_MaterialEditor.RangeProperty(mipLevel, Styles.mipLevelText.text);
            //}

            ////refraction
            //{
            //    m_MaterialEditor.TextureProperty(refractionTex, Styles.refractionTexText.text);
            //    if (refractionTex.textureValue)
            //    {
            //        m_MaterialEditor.FloatProperty(refractionValue, Styles.refractionValueText.text);
            //    }
            //}

            ////wave
            //{
            //    m_MaterialEditor.TextureProperty(waveTex, Styles.waveTexText.text);
            //    if (waveTex.textureValue)
            //    {
            //        enableWave.floatValue = 1;
            //        m_MaterialEditor.FloatProperty(waveSize, Styles.waveSizeText.text);
            //        m_MaterialEditor.FloatProperty(waveDistortion, Styles.waveDistortionText.text);
            //        m_MaterialEditor.FloatProperty(waveSpeed, Styles.waveSpeedText.text);
            //    }
            //    else
            //    {
            //        enableWave.floatValue = -1;
            //    }
            //}
     
            ////simple depth
            //{
            //    var simpleDepth = enableSimpleDepth.floatValue > 0.5f;
            //    simpleDepth = EditorGUILayout.Toggle(Styles.enableDepthText, simpleDepth);
            //    enableSimpleDepth.floatValue = simpleDepth ? 1.0f : 0.0f;

            //    if(enableSimpleDepth.floatValue > 0.5)
            //        m_MaterialEditor.RangeProperty(simpleDepthCutoff, Styles.depthCutOffText.text);
            //}

            ////depth blur
            //{
            //    var depthBlur = enableDepthBlur.floatValue > 0.5f;
            //    depthBlur = EditorGUILayout.Toggle(Styles.enableDepthBlurText, depthBlur);
            //    enableDepthBlur.floatValue = depthBlur ? 1.0f : 0.0f;

            //}

            ////mask
            //{
            //    m_MaterialEditor.TextureProperty(maskTex, Styles.maskTexText.text);
            //    if (maskTex.textureValue)
            //    {
            //        enableMask.floatValue = 1;
            //        m_MaterialEditor.RangeProperty(maskCutOff, Styles.maskCutOffText.text);
            //        m_MaterialEditor.RangeProperty(maskEdgeDarkness, Styles.maskEdgeDarknessText.text);
            //        m_MaterialEditor.VectorProperty(maskTiling, Styles.maskTilingText.text);
            //    }
            //    else
            //    {
            //        enableMask.floatValue = -1;
            //    }
            //}

            ////MaterialProperty _EnableRealTimeReflection = ShaderGUI.FindProperty("_EnableRealTimeReflection", properties);

            ////MenuReflection = EditorGUILayout.BeginFoldoutHeaderGroup(MenuReflection, new GUIContent { text = "Reflection" });

            ////bool realTimeRef = false;
            ////if (MenuReflection)
            ////{
            ////    realTimeRef = _EnableRealTimeReflection.floatValue != 0.0f;
            ////    realTimeRef = EditorGUILayout.Toggle("RealTime Reflection", realTimeRef);
            ////    _EnableRealTimeReflection.floatValue = realTimeRef ? 1.0f : 0.0f;

            ////    //if (realTimeRef)
            ////    //{
            ////    //    MaterialProperty _EnviCubeIntensity = ShaderGUI.FindProperty("_EnviCubeIntensity", properties);
            ////    //    materialEditorIn.ShaderProperty(_EnviCubeIntensity, "Intensity");

            ////    //    MaterialProperty _EnviCubeSmoothness = ShaderGUI.FindProperty("_EnviCubeSmoothness", properties);
            ////    //    materialEditorIn.ShaderProperty(_EnviCubeSmoothness, "Smoothness");
            ////    //}
            ////}
        }

        void DoNormalArea()
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
            //todo:
            //if (bumpScale.floatValue != 1
            //    && BuildTargetDiscovery.PlatformHasFlag(EditorUserBuildSettings.activeBuildTarget, TargetAttributes.HasIntegratedGPU))
            //    if (m_MaterialEditor.HelpBoxWithButton(
            //        EditorGUIUtility.TrTextContent("Bump scale is not supported on mobile platforms"),
            //        EditorGUIUtility.TrTextContent("Fix Now")))
            //    {
            //        bumpScale.floatValue = 1;
            //    }
        }

        void DoAlbedoArea(Material material)
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
            if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
            }
        }

        void DoEmissionArea(Material material)
        {
            // Emission for GI?
            if (m_MaterialEditor.EmissionEnabledProperty())
            {
                bool hadEmissionTexture = emissionMap.textureValue != null;

                // Texture and HDR color controls
                m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, false);

                // If texture was assigned and color was black set color to white
                float brightness = emissionColorForRendering.colorValue.maxColorComponent;
                if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                    emissionColorForRendering.colorValue = Color.white;

                // change the GI flag and fix it up with emissive as black if necessary
                m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
            }
        }

        void DoSpecularMetallicArea()
        {
            bool hasGlossMap = false;
            if (m_WorkflowMode == WorkflowMode.Specular)
            {
                hasGlossMap = specularMap.textureValue != null;
                m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap, hasGlossMap ? null : specularColor);
            }
            else if (m_WorkflowMode == WorkflowMode.Metallic)
            {
                hasGlossMap = metallicMap.textureValue != null;
                m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap, hasGlossMap ? null : metallic);
            }

            bool showSmoothnessScale = hasGlossMap;
            if (smoothnessMapChannel != null)
            {
                int smoothnessChannel = (int)smoothnessMapChannel.floatValue;
                if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
                    showSmoothnessScale = true;
            }

            int indentation = 2; // align with labels of texture properties
            m_MaterialEditor.ShaderProperty(showSmoothnessScale ? smoothnessScale : smoothness, showSmoothnessScale ? Styles.smoothnessScaleText : Styles.smoothnessText, indentation);

            ++indentation;
            if (smoothnessMapChannel != null)
                m_MaterialEditor.ShaderProperty(smoothnessMapChannel, Styles.smoothnessMapChannelText, indentation);
        }

        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }
        }

        static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;
            else
                return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
        {
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
            if (workflowMode == WorkflowMode.Specular)
                SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            else if (workflowMode == WorkflowMode.Metallic)
                SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
            SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }
        }

        static void MaterialChanged(Material material, WorkflowMode workflowMode)
        {
            SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

            SetMaterialKeywords(material, workflowMode);
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
} // namespace UnityEditor