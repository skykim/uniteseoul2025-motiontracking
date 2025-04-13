using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Compression;
using Unity.LiveCapture.ARKitFaceCapture;
using Unity.LiveCapture;
using UnityEngine.Playables;
using InteractionUI;
using FacialController;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Collections;
using System.Security.Policy;

public class PluginWindowEditor : EditorWindow
{
    Texture2D texture, resultTex, reflectTex;

    string prompt = string.Empty;

    Dictionary<string, Texture2D> textureDictionary = new Dictionary<string, Texture2D>();

    Dictionary<string, string> importSettings = new Dictionary<string, string>();

    string filePath = string.Empty;

    bool isClicked2k = false;
    bool isClicked4k = false;
    bool isClickedURP = false;
    bool isClickedHDRP = false;
    bool isClickedBody = false;
    bool isClickedEye = false;
    bool isClickedBS = false;
    bool isClickedBack = false;
    bool isClickedBodyCtrl = false;
    bool isClickedLiveLink = false;

    bool canClick2k = false;
    bool canClick4k = false;
    bool canClickBody = false;
    bool canClickEye = false;
    bool canClickBS = false;
    bool canClickBack = false;
    bool canClickBodyCtrl = false;
    bool canClickLiveLink = false;

    private string _importPath = "Assets/Deemos/ChatAvatar/";
    private string _extractPath;
    private string[] _allFileNames;
    private string _matPathHDRP = "Assets/Deemos/HDRP/ParentAssets/";
    private string _matPathURP = "Assets/Deemos/URP/ParentAssets/";
    private string _modelPath;

    GameObject _model;
    private Material _face_mat;
    private Material _back_mat;
    private Material _eye_mat;
    private Material _teeth_mat;
    private Material _lacrimal_mat;
    private Material _brow_mat;
    private Material _tearLine_mat;


    enum PluginPages { 
        Page1, Page3, Page4, Page5, Page6
    }

    PluginPages currentPage = PluginPages.Page1;

    public static Dictionary<string, Texture2D> BuildMapping()
    {
        Dictionary<string, Texture2D> temp = new Dictionary<string, Texture2D>();

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Deemos/Resources/UI" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            temp[name] = texture;
        }
        return temp;
    }

    public Texture2D GetTextureByName(string name)
    {
        if (textureDictionary.ContainsKey(name))
        {
            return textureDictionary[name];
        }
        return null;
    }

    public Texture2D GetPreviewImage(Texture2D sourceTex)
    {
        Texture2D destTex = new Texture2D(sourceTex.width, sourceTex.height, sourceTex.format, false);
        destTex.LoadRawTextureData(sourceTex.GetRawTextureData());

        Texture2D tex2 = GetTextureByName("T_xiangkuangzhezhao_3");
        int width = 256;
        int height = 512;
        Texture2D result = new Texture2D(width, height);

        for (int y = 0; y < result.height; y++)
        {
            for (int x = 0; x < result.width; x++)
            {
                // calculate UV for scaling
                float u = ((float)x / (width - 1));
                float v = ((float)y / (height - 1));
                Color col1 = destTex.GetPixelBilinear(u, v);
                Color col2 = tex2.GetPixelBilinear(u, v);
                Color col = Color.Lerp(new Color(0.2196079f, 0.2196079f, 0.2196079f, 1f), col1, col2.r);
                // multiply colors and set to the result texture
                result.SetPixel(x, y, col);
            }
        }

        result.Apply();

        return result;
    }

    public Texture2D GetReflectionImage(Texture2D sourceTex)
    {
        Texture2D destTex = new Texture2D(sourceTex.width, sourceTex.height, sourceTex.format, false);
        destTex.LoadRawTextureData(sourceTex.GetRawTextureData());

        Texture2D tex2 = GetTextureByName("T_xiangkuangzhezhao_3");
        Texture2D tex3 = GetTextureByName("T_xiangkuangzhezhao_2");
        int width = 256;
        int height = 512;
        Texture2D result = new Texture2D(width, height);

        for (int y = 0; y < result.height; y++)
        {
            for (int x = 0; x < result.width; x++)
            {
                // calculate UV for scaling
                float u = ((float)x / (width - 1));
                float v = ((float)y / (height - 1));
                Color col1 = destTex.GetPixelBilinear(u, 1 - v);
                Color col2 = tex2.GetPixelBilinear(u, v);
                Color col3 = tex3.GetPixelBilinear(u, 1 - v);
                Color col = Color.Lerp(new Color(0.2196079f, 0.2196079f, 0.2196079f, 1f), col1 * col3, col2.r);
                // multiply colors and set to the result texture
                result.SetPixel(x, y, col);
            }
        }

        result.Apply();

        return result;
    }

    private static bool PropertyExists(SerializedProperty property, int start, int end, string value)
    {
        for (int i = start; i < end; i++)
        {
            SerializedProperty t = property.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(value))
            {
                return true;
            }
        }
        return false;
    }

    [MenuItem("Window/Deemos Import Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<PluginWindowEditor>("Deemos Import Tool");
        window.minSize = new Vector2(1280, 720);
        window.maxSize = window.minSize;

        string[] _testTagArr = { "CinemachineTarget", "ChatAvatar", "BodyTPP" };
        foreach (string tag in _testTagArr)
        {
            if (!UnityEditorInternal.InternalEditorUtility.tags.Equals(tag)) //如果tag列表中没有这个tag
            {
                UnityEditorInternal.InternalEditorUtility.AddTag(tag); //在tag列表中添加这个tag
            }
        }
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Layers Property
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        string layerName = "StarterCharacter";
        if (!PropertyExists(layersProp, 0, 31, layerName))
        {
            SerializedProperty sp;
            // Start at layer 9th index -> 8 (zero based) => first 8 reserved for unity / greyed out
            for (int i = 8, j = 31; i < j; i++)
            {
                sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.stringValue == "")
                {
                    // Assign string value to layer
                    sp.stringValue = layerName;
                    Debug.Log("Layer: " + layerName + " has been added");
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    break;
                }
                if (i == j)
                    Debug.Log("All allowed layers have been filled");
            }
        }
        else
        {
            //Debug.Log ("Layer: " + layerName + " already exists");
        }
    }

    private void OnEnable()
    {
        textureDictionary = BuildMapping();
        prompt = string.Empty;

        isClicked2k = false;
        isClicked4k = false;
        isClickedURP = false;
        isClickedHDRP = false;
        isClickedBody = false;
        isClickedEye = false;
        isClickedBS = false;
        isClickedBack = false;
        isClickedBodyCtrl = false;
        isClickedLiveLink = false;

        importSettings.Add("resolution", "2k");
        importSettings.Add("RP", "HDRP");
        importSettings.Add("parts", "model");
        importSettings.Add("back", "false");
        importSettings.Add("plugins", "none");
        importSettings["RP"] = GraphicsSettings.currentRenderPipeline.GetType().Name == "HDRenderPipelineAsset" ? "HDRP" : "URP";

        currentPage = PluginPages.Page1;

}

    private void OnGUI()
    {
        switch (currentPage)
        {
            case PluginPages.Page1:
                DrawPage1();
                break;
            case PluginPages.Page3:
                DrawPage3();
                break;
            case PluginPages.Page4:
                DrawPage4();
                break;
            case PluginPages.Page5:
                DrawPage5();
                break;
            case PluginPages.Page6:
                DrawPage6();
                break;
        }
    }

    private void DrawPage1()
    {
        // All Fixed Contents
        GUI.DrawTexture(new Rect(0, -20, position.width, position.height+40), GetTextureByName("Background"), ScaleMode.StretchToFill);

        GUI.DrawTexture(new Rect(68, 376, 234, 54), GetTextureByName("T_loader"), ScaleMode.StretchToFill);

        // Slogan Text
        GUIStyle sloganButtonStyle = new GUIStyle();
        sloganButtonStyle.normal.background = GetTextureByName("T_ChatAvatar");
        sloganButtonStyle.hover.background = GetTextureByName("T_ChatAvatar");
        if (GUI.Button(new Rect(55, 208, 879, 99), "", sloganButtonStyle))
        {
            Application.OpenURL("https://hyperhuman.deemos.com/");
        }

        // Import Button
        GUIStyle importButtonStyle = new GUIStyle();
        importButtonStyle.normal.background = GetTextureByName("T_anjian");
        importButtonStyle.hover.background = GetTextureByName("T_anjian");
        if (GUI.Button(new Rect(44, 484, 306, 127), "", importButtonStyle))
        {
            filePath = EditorUtility.OpenFilePanel("Select package you download from HYPERHUMAN!", "", "zip");

            bool isValid = true;
            if (!string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Selected File: " + filePath);
                isValid = UnzipFile_CheckContent(filePath);
            }

            if (!isValid)
            {
                currentPage = PluginPages.Page3;
            }
            else
            {
                CheckZip();
                // Reset Import Settings for Page4
                isClicked2k = false;
                isClicked4k = false;
                importSettings["resolution"] = "2k";
                if (GraphicsSettings.currentRenderPipeline.GetType().Name == "HDRenderPipelineAsset")
                {
                    importSettings["RP"] = "HDRP";
                    isClickedHDRP = true;
                    isClickedURP = false;
                }
                else
                {
                    importSettings["RP"] = "URP";
                    isClickedHDRP = false;
                    isClickedURP = true;
                }

                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(_importPath, "image.png"));
                resultTex = GetPreviewImage(texture);
                reflectTex = GetReflectionImage(texture);
                if (File.Exists(Path.Combine(_extractPath, "prompt.txt")))
                {
                    prompt = File.ReadAllText(Path.Combine(_extractPath, "prompt.txt"));
                }

                currentPage = PluginPages.Page4;
            }
        }

    }

    private void DrawPage3()
    {
        // All Fixed Contents
        GUI.DrawTexture(new Rect(0, -20, position.width, position.height + 40), GetTextureByName("Background"), ScaleMode.StretchToFill);

        GUI.DrawTexture(new Rect(68, 376, 234, 54), GetTextureByName("T_loader"), ScaleMode.StretchToFill);

        // Slogan Text
        GUIStyle sloganButtonStyle = new GUIStyle();
        sloganButtonStyle.normal.background = GetTextureByName("T_ChatAvatar");
        sloganButtonStyle.hover.background = GetTextureByName("T_ChatAvatar");
        if (GUI.Button(new Rect(55, 208, 879, 99), "", sloganButtonStyle))
        {
            Application.OpenURL("https://hyperhuman.deemos.com/");
        }

        // Import Button
        GUIStyle importButtonStyle = new GUIStyle();
        importButtonStyle.normal.background = GetTextureByName("T_anjian");
        importButtonStyle.hover.background = GetTextureByName("T_anjian");
        if (GUI.Button(new Rect(44, 484, 306, 127), "", importButtonStyle))
        {
            filePath = EditorUtility.OpenFilePanel("Select package you download from HYPERHUMAN!", "", "zip");

            bool isValid = true;
            if (!string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Selected File: " + filePath);
                isValid = UnzipFile_CheckContent(filePath);
            }

            if (!isValid)
            {
                currentPage = PluginPages.Page3;
            }
            else
            {

                canClick2k = false;
                canClick4k = false;
                canClickBody = false;
                canClickEye = false;
                canClickBS = false;
                canClickBack = false;
                canClickBodyCtrl = false;
                canClickLiveLink = false;


                CheckZip();
                // Reset Import Settings for Page4
                isClicked2k = false;
                isClicked4k = false;
                importSettings["resolution"] = "2k";
                if (GraphicsSettings.currentRenderPipeline.GetType().Name == "HDRenderPipelineAsset")
                {
                    importSettings["RP"] = "HDRP";
                    isClickedHDRP = true;
                    isClickedURP = false;
                }
                else
                {
                    importSettings["RP"] = "URP";
                    isClickedHDRP = false;
                    isClickedURP = true;
                }

                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(_importPath, "image.png"));
                resultTex = GetPreviewImage(texture);
                reflectTex = GetReflectionImage(texture);
                if (File.Exists(Path.Combine(_importPath, "prompt.txt"))){
                    prompt = File.ReadAllText(Path.Combine(_importPath, "prompt.txt"));
                }

                currentPage = PluginPages.Page4;
            }
        }

        // Fail Text
        GUIStyle failButtonStyle = new GUIStyle();
        failButtonStyle.normal.background = GetTextureByName("T_Failed__Please_Import_the_Package_download_from_HYPERHUMAN");
        failButtonStyle.hover.background = GetTextureByName("T_Failed__Please_Import_the_Package_download_from_HYPERHUMAN");
        if (GUI.Button(new Rect(75, 655, 820, 28), "", failButtonStyle))
        {
            Application.OpenURL("https://hyperhuman.deemos.com/");
        }

    }

    private void DrawPage4()
    {
        // All Fixed Contents
        GUI.DrawTexture(new Rect(51, 122, 284, 73), GetTextureByName("T_settings"), ScaleMode.StretchToFill);

        // Resolution Text
        GUI.DrawTexture(new Rect(67, 208, 149, 27), GetTextureByName("T_resolution"), ScaleMode.StretchToFill);

        // 2K Button
        GUI.enabled = canClick2k;
        GUI.color = canClick2k ? Color.white : Color.gray;
        GUIStyle FalseStyle2k = new GUIStyle();
        FalseStyle2k.normal.background = GetTextureByName("T_anjian_2kw");
        GUIStyle TrueStyle2k = new GUIStyle();
        TrueStyle2k.normal.background = GetTextureByName("T_anjian_2kl");
        if (GUI.Button(new Rect(61, 253, 248, 62), "", isClicked2k ? TrueStyle2k : FalseStyle2k))
        {
            isClicked2k = !isClicked2k;
            isClicked4k = false;
            Debug.Log("2k: " + isClicked2k.ToString());
        }

        // 4K Button
        GUI.enabled = canClick4k;
        GUI.color = canClick4k ? Color.white : Color.gray;
        GUIStyle FalseStyle4k = new GUIStyle();
        FalseStyle4k.normal.background = GetTextureByName("T_anjian_4kw");
        GUIStyle TrueStyle4k = new GUIStyle();
        TrueStyle4k.normal.background = GetTextureByName("T_anjian_4kl");
        if (GUI.Button(new Rect(339, 253, 248, 62), "", isClicked4k ? TrueStyle4k : FalseStyle4k))
        {
            isClicked4k = !isClicked4k;
            isClicked2k = false;
            Debug.Log("4k: " + isClicked4k.ToString());
        }
        GUI.enabled = true;
        GUI.color = Color.white;

        // Render Pipeline Text
        GUI.DrawTexture(new Rect(67, 347, 222, 34), GetTextureByName("T_renderPipeline"), ScaleMode.StretchToFill);

        // URP Button
        GUIStyle FalseStyleURP = new GUIStyle();
        FalseStyleURP.normal.background = GetTextureByName("T_anjian_urpw");
        GUIStyle TrueStyleURP = new GUIStyle();
        TrueStyleURP.normal.background = GetTextureByName("T_anjian_urpl");
        if (GUI.Button(new Rect(61, 391, 248, 62), "", isClickedURP ? TrueStyleURP : FalseStyleURP))
        {
            isClickedURP = !isClickedURP;
            isClickedHDRP = false;
            Debug.Log("URP: " + isClickedURP.ToString());
        }

        // HDRP Button
        GUIStyle FalseStyleHDRP = new GUIStyle();
        FalseStyleHDRP.normal.background = GetTextureByName("T_anjian_hdrpw");
        GUIStyle TrueStyleHDRP = new GUIStyle();
        TrueStyleHDRP.normal.background = GetTextureByName("T_anjian_hdrpl");
        if (GUI.Button(new Rect(339, 391, 248, 62), "", isClickedHDRP ? TrueStyleHDRP : FalseStyleHDRP))
        {
            isClickedHDRP = !isClickedHDRP;
            isClickedURP = false;
            Debug.Log("HDRP: " + isClickedHDRP.ToString());
        }

        // Next Button
        GUIStyle nextButtonStyle = new GUIStyle();
        nextButtonStyle.normal.background = GetTextureByName("T_anjian_5");
        if (GUI.Button(new Rect(45, 484, 306, 123), "", nextButtonStyle))
        {
            // Write import settings
            if (isClicked2k || isClicked4k)
            {
                importSettings["resolution"] = isClicked4k ? "4k" : "2k";
                importSettings["RP"] = isClickedURP ? "URP" : "HDRP";

                // Reset Import Settings for Page5
                isClickedBody = false;
                isClickedEye = false;
                isClickedBS = false;
                isClickedBack = false;
                importSettings["parts"] = "model";
                importSettings["back"] = "false";

                currentPage = PluginPages.Page5;
            }
        }

        // Back Button
        GUIStyle backButtonStyle = new GUIStyle();
        backButtonStyle.normal.background = GetTextureByName("T_Back");
        if (GUI.Button(new Rect(66, 42, 107, 37), "", backButtonStyle))
        {
            currentPage = PluginPages.Page1;

            // Reset import settings
            importSettings = new Dictionary<string, string>();
            importSettings.Add("resolution", "2k");
            importSettings.Add("RP", "HDRP");
            importSettings.Add("parts", "model");
            importSettings.Add("back", "false");
            importSettings.Add("plugins", "none");
            importSettings["RP"] = GraphicsSettings.currentRenderPipeline.GetType().Name == "HDRenderPipelineAsset" ? "HDRP" : "URP";

        }

        // Portrait ( and prompt )
        Rect rect = new Rect(813, 98, 400, 528);
        Rect rect2 = new Rect(813, 626, 400, 528);
        GUIStyle centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.wordWrap = true;

        if (rect.Contains(Event.current.mousePosition) && prompt != string.Empty)
        {
            GUI.Label(rect, prompt, centeredStyle);
            Repaint();
        }
        else
        {
            GUI.DrawTexture(rect, resultTex);
            GUI.DrawTexture(rect2, reflectTex);
            Repaint();
        }
    }

    private void DrawPage5()
    {
        // All Fixed Contents
        GUI.DrawTexture(new Rect(51, 122, 284, 73), GetTextureByName("T_settings"), ScaleMode.StretchToFill);

        GUI.DrawTexture(new Rect(67, 210, 74, 25), GetTextureByName("T_parts"), ScaleMode.StretchToFill);

        // Body Button
        GUI.enabled = canClickBody;
        GUI.color = canClickBody ? Color.white : Color.gray;
        GUIStyle FalseStyleBody = new GUIStyle();
        FalseStyleBody.normal.background = GetTextureByName("T_anjian_RiggedBodyw");
        GUIStyle TrueStyleBody = new GUIStyle();
        TrueStyleBody.normal.background = GetTextureByName("T_anjian_RiggedBodyl");
        if (GUI.Button(new Rect(61, 253, 248, 62), "", isClickedBody ? TrueStyleBody : FalseStyleBody))
        {
            isClickedBody = !isClickedBody;
            Debug.Log("Body: " + isClickedBody.ToString());
        }

        // Eye&Teeth Button
        GUI.enabled = canClickEye;
        GUI.color = canClickEye ? Color.white : Color.gray;
        GUIStyle FalseStyleEye = new GUIStyle();
        FalseStyleEye.normal.background = GetTextureByName("T_anjian_Eye_Teethw");
        GUIStyle TrueStyleEye = new GUIStyle();
        TrueStyleEye.normal.background = GetTextureByName("T_anjian_Eye_Teethl");
        if (GUI.Button(new Rect(339, 253, 248, 62), "", isClickedEye ? TrueStyleEye : FalseStyleEye))
        {
            isClickedEye = !isClickedEye;
            Debug.Log("Eye&Teeth: " + isClickedEye.ToString());
        }

        // BS Button
        GUI.enabled = canClickBS;
        GUI.color = canClickBS ? Color.white : Color.gray;
        GUIStyle FalseStyleBS = new GUIStyle();
        FalseStyleBS.normal.background = GetTextureByName("T_anjian_Expression_Blendshapes_1");
        GUIStyle TrueStyleBS = new GUIStyle();
        TrueStyleBS.normal.background = GetTextureByName("T_anjian_Expression_Blendshapes_2");
        if (GUI.Button(new Rect(61, 370, 248, 62), "", isClickedBS ? TrueStyleBS : FalseStyleBS))
        {
            isClickedBS = !isClickedBS;
            Debug.Log("Blendshape: " + isClickedBS.ToString());
        }

        // Back Tex Button
        GUI.enabled = canClickBack;
        GUI.color = canClickBack ? Color.white : Color.gray;
        GUIStyle FalseStyleBack = new GUIStyle();
        FalseStyleBack.normal.background = GetTextureByName("T_anjian_Back_Head_Texturew");
        GUIStyle TrueStyleBack = new GUIStyle();
        TrueStyleBack.normal.background = GetTextureByName("T_anjian_Back_Head_Texturel");
        if (GUI.Button(new Rect(339, 370, 248, 62), "", isClickedBack ? TrueStyleBack : FalseStyleBack))
        {
            isClickedBack = !isClickedBack;
            Debug.Log("Back Tex: " + isClickedBack.ToString());
        }
        GUI.enabled = true;
        GUI.color = Color.white;

        // Next Button
        GUIStyle nextButtonStyle = new GUIStyle();
        nextButtonStyle.normal.background = GetTextureByName("T_anjian_5");
        if (GUI.Button(new Rect(45, 484, 306, 123), "", nextButtonStyle))
        {
            // Write import settings
            if (isClickedBody)
            {
                importSettings["parts"] = "body";
            }
            else
            {
                if (isClickedEye)
                {
                    importSettings["parts"] = "eye";
                }
                else
                {
                    importSettings["parts"] = isClickedBS ? "bs" : "model";
                }
            }
            importSettings["back"] = isClickedBack ? "true" : "false";

            // Reset Import Settings for Page6
            isClickedBodyCtrl = false;
            isClickedLiveLink = false;
            importSettings["plugins"] = "none";
            if(isClickedBody || isClickedEye || isClickedBS)
            {
                canClickLiveLink = true;
            }
            if (isClickedBody)
            {
                canClickBodyCtrl = true;
            }

            currentPage = PluginPages.Page6;
        }

        // Back Button
        GUIStyle backButtonStyle = new GUIStyle();
        backButtonStyle.normal.background = GetTextureByName("T_Back");
        if (GUI.Button(new Rect(66, 42, 107, 37), "", backButtonStyle))
        {
            // Reset Import Settings for Page4
            isClicked2k = false;
            isClicked4k = false;
            importSettings["resolution"] = "2k";
            if (GraphicsSettings.currentRenderPipeline.GetType().Name == "HDRenderPipelineAsset")
            {
                importSettings["RP"] = "HDRP";
                isClickedHDRP = true;
                isClickedURP = false;
            }
            else
            {
                importSettings["RP"] = "URP";
                isClickedHDRP = false;
                isClickedURP = true;
            }

            currentPage = PluginPages.Page4;
        }

        // Portrait ( and prompt )
        Rect rect = new Rect(813, 98, 400, 528);
        Rect rect2 = new Rect(813, 626, 400, 528);
        GUIStyle centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.wordWrap = true;

        if (rect.Contains(Event.current.mousePosition) && prompt != string.Empty)
        {
            GUI.Label(rect, prompt, centeredStyle);
            Repaint();
        }
        else
        {
            GUI.DrawTexture(rect, resultTex);
            GUI.DrawTexture(rect2, reflectTex);
            Repaint();
        }
    }

    private void DrawPage6()
    {
        // All Fixed Contents
        GUI.DrawTexture(new Rect(51, 122, 284, 73), GetTextureByName("T_settings"), ScaleMode.StretchToFill);

        GUI.DrawTexture(new Rect(67, 210, 105, 34), GetTextureByName("T_plugins"), ScaleMode.StretchToFill);

        // BodyCtrl Button
        GUI.enabled = canClickBodyCtrl;
        GUI.color = canClickBodyCtrl ? Color.white : Color.gray;
        GUIStyle FalseStyleBC = new GUIStyle();
        FalseStyleBC.normal.background = GetTextureByName("T_anjian_BodyControlw");
        GUIStyle TrueStyleBC = new GUIStyle();
        TrueStyleBC.normal.background = GetTextureByName("T_anjian_BodyControll");
        if (GUI.Button(new Rect(61, 312, 248, 62), "", isClickedBodyCtrl ? TrueStyleBC : FalseStyleBC))
        {
            isClickedBodyCtrl = !isClickedBodyCtrl;
            Debug.Log("Body Control: " + isClickedBodyCtrl.ToString());
        }

        // LiveLink Button
        GUI.enabled = canClickLiveLink;
        GUI.color = canClickLiveLink ? Color.white : Color.gray;
        GUIStyle FalseStyleLL = new GUIStyle();
        FalseStyleLL.normal.background = GetTextureByName("T_anjian_LiveLinkFacew");
        GUIStyle TrueStyleLL = new GUIStyle();
        TrueStyleLL.normal.background = GetTextureByName("T_anjian_LiveLinkFacel");
        if (GUI.Button(new Rect(339, 312, 248, 62), "", isClickedLiveLink ? TrueStyleLL : FalseStyleLL))
        {
            isClickedLiveLink = !isClickedLiveLink;
            Debug.Log("LiveLink: " + isClickedLiveLink.ToString());
        }

        // Confirm Button
        GUI.enabled = true;
        GUI.color = Color.white;
        GUIStyle confirmButtonStyle = new GUIStyle();
        confirmButtonStyle.normal.background = GetTextureByName("T_anjian_4");
        if (GUI.Button(new Rect(45, 484, 306, 123), "", confirmButtonStyle))
        {
            int spec = Convert.ToInt32(isClickedBodyCtrl) * 2 + Convert.ToInt32(isClickedLiveLink);
            LoadMatAndPic();
            LoadModel();
            switch (spec)
            {
                case 0:
                    importSettings["plugins"] = "none";
                    _model.tag = ("ChatAvatar");
                    if (importSettings["parts"] == "model")
                    {
                        _model.transform.localScale = new Vector3(.01f, .01f, .01f);
                    }
                    if (importSettings["parts"] != "body")
                    {
                        _model.transform.localPosition = new Vector3(0, 1.725f, 0);
                    }
                    else
                    {
                        _model.transform.localPosition = new Vector3(0, 0, 0);
                    }
                    _model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                    // Set vars in DemoScene
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "ModelTestScene")
                    {
                        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                        GameObject rigPanelObj = null;
                        GameObject interactionUI = null;
                        foreach (GameObject go in rootObjects)
                        {
                            if (go.name == "Main Camera Root")
                            {
                                go.SetActive(true);
                            }
                            if (go.name == "Camera_FullShot_root")
                            {
                                Transform tr = go.transform.Find("Camera Closeup");
                                tr.position = new Vector3(0, 1.68f, -0.5f);
                                Transform tr1 = go.transform.Find("Camera MidShot");
                                tr1.position = new Vector3(0, 1.7f, -1.5f);
                            }
                            if (go.name == "Canvas1")
                            {
                                rigPanelObj = go.transform.Find("ControlRigPanel").gameObject;
                                interactionUI = go.transform.Find("InteractionUI").gameObject;
                            }
                        }

                        if (importSettings["parts"] == "bs" || importSettings["parts"] == "eye")
                        {
                            FacialController.HeadGroup headGroup = _model.AddComponent<FacialController.HeadGroup>();
                            headGroup.m_headComponents = new List<GameObject>();
                            headGroup.m_headComponents.Add(_model);
                            headGroup.m_skinnedMeshRendererItems = new List<FacialController.SkinnedMeshRendererItem>();
                            headGroup.m_skinnedMeshRendererItems.Add(null);
                            headGroup.Start();

                            rigPanelObj.GetComponent<FacialControllers>().m_HeadGroup = headGroup;
                            interactionUI.GetComponent<HeadUI>().m_HeadGroup = headGroup;
                        }
                    }

                    Close();
                    Debug.Log("Set Neither");

                    break;
                case 1:
                    importSettings["plugins"] = "ll";
                    SetLiveLink();
                    Close();
                    Debug.Log("Live Link Only");

                    break;
                case 2:
                    importSettings["plugins"] = "bc";
                    SetBodyControl();
                    Close();
                    Debug.Log("BC Only");

                    break;
                case 3:
                    importSettings["plugins"] = "both";
                    SetBoth();
                    Close();
                    Debug.Log("Set Both");

                    break;
            }

            Debug.Log("Confirm in Page6 Clicked!");
        }

        // Back Button
        GUIStyle backButtonStyle = new GUIStyle();
        GUI.color = Color.white;
        backButtonStyle.normal.background = GetTextureByName("T_Back");
        if (GUI.Button(new Rect(66, 42, 107, 37), "", backButtonStyle))
        {
            // Reset Import Settings for Page5
            isClickedBody = false;
            isClickedEye = false;
            isClickedBS = false;
            isClickedBack = false;
            importSettings["parts"] = "model";
            importSettings["back"] = "false";

            currentPage = PluginPages.Page5;
        }

        // Portrait ( and prompt )
        Rect rect = new Rect(813, 98, 400, 528);
        Rect rect2 = new Rect(813, 626, 400, 528);
        GUIStyle centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.wordWrap = true;

        if (rect.Contains(Event.current.mousePosition) && prompt != string.Empty)
        {
            GUI.Label(rect, prompt, centeredStyle);
            Repaint();
        }
        else
        {
            GUI.DrawTexture(rect, resultTex);
            GUI.DrawTexture(rect2, reflectTex);
            Repaint();
        }
    }

    private bool UnzipFile_CheckContent(string zipPath)
    {
        if (string.IsNullOrEmpty(zipPath))
        {
            EditorGUILayout.HelpBox("Not a valid zip file or not such a path.", MessageType.Error);
            return false;
        }
        
        if (Path.GetExtension(zipPath).ToLower() == ".zip")
        {
            var filename = Path.GetFileNameWithoutExtension(zipPath);
            _extractPath = "Library/Deemos/" + filename;
            _importPath = "Assets/ChatAvatar/" + filename + "/";

            Debug.Log("file:: " + filename);
            // Extract the zip to ./Extracted
            ZipFile.ExtractToDirectory(zipPath, _extractPath, true);

            _allFileNames = Directory.GetFiles(_extractPath, "*.*", SearchOption.AllDirectories);

            string[] requiredFiles =
            {
                "USCBasicPack/model.obj",
                "USCBasicPack/additional_component.fbx",
                "USCBasicPack/additional_blendshape.fbx",
                "USCBasicPack/additional_body.fbx",
                "USCHighPack/model.obj",
            };
            int flag = 0; //To record if all 4 are missing
            foreach (string file in requiredFiles)
            {
                string fullPath = Path.Combine(_extractPath, file);
                if (File.Exists(fullPath))
                {
                    flag = 1;
                }
            }
            if (flag == 0)//missing model
            {
                EditorGUILayout.HelpBox("File missing, Please check your file or re-download the package", MessageType.Error);
                return false;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Selected file is not a Deemos package file.", MessageType.Error);
            return false;
        }

        CopyAndImport("image.png");
        return true;
    }

    private void CopyAndImport(string filePath)
    {

        string copyFrom = Path.Combine(_extractPath, filePath);
        string copyDest = Path.Combine(_importPath, filePath);

        if (!Directory.Exists(Path.GetDirectoryName(copyDest)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(copyDest));
        }

        File.Copy(copyFrom, copyDest, true);
        AssetDatabase.ImportAsset(copyDest, ImportAssetOptions.ForceUpdate);
    }

    private void CheckZip()
    {
        Debug.Log(_extractPath);
        if (File.Exists(Path.Combine(_extractPath, "USCBasicPack/texture_diffuse.png")))
        {
            canClick2k = true;
        }
        if (File.Exists(Path.Combine(_extractPath, "USCHighPack/texture_diffuse.png")))
        {
            canClick4k = true;
        }
        if (File.Exists(Path.Combine(_extractPath, "USCBasicPack/additional_body.fbx")))
        {
            canClickBody = true;
        }
        if (File.Exists(Path.Combine(_extractPath, "USCBasicPack/additional_component.fbx")) || File.Exists(Path.Combine(_extractPath, "USCBasicPack/additional_component_neutral.obj")))
        {
            canClickEye = true;
        }
        if (File.Exists(Path.Combine(_extractPath, "USCBasicPack/additional_blendshape.fbx")))
        {
            canClickBS = true;
        }
        if (File.Exists(Path.Combine(_extractPath, "USCBasicPack/texture_diffuse_backhead.png")) && File.Exists(Path.Combine(_extractPath, "USCBasicPack/texture_specular_backhead.png")) && File.Exists(Path.Combine(_extractPath, "USCBasicPack/texture_normal_backhead.png")))
        {
            canClickBack = true;
        }
    }

    private void LoadMatAndPic()
    {
        List<string> toImport = new List<string>();
        string finalPath = "";

        if (importSettings["resolution"] == "2k")
        {
            //_extractPath = Path.Combine(_extractPath, "/USCBasicPack/");

            _importPath = Path.Combine(_importPath, "default/");
            finalPath = Path.Combine(_importPath, "USCBasicPack/");

            if (importSettings["parts"] == "model")
            {
                toImport.Add("USCBasicPack/model.obj");
                _modelPath = Path.Combine(finalPath, "model.obj");

            }
            else if (importSettings["parts"] == "body")
            {
                toImport.Add("USCBasicPack/additional_body.fbx");
                _modelPath = Path.Combine(finalPath, "additional_body.fbx");
            }
            else if (importSettings["parts"] == "bs")
            {
                toImport.Add("USCBasicPack/additional_blendshape.fbx");
                _modelPath = Path.Combine(finalPath, "additional_blendshape.fbx");
            }
            else if (importSettings["parts"] == "eye")
            {
                int flag = 0;
                string[] files2 = Directory.GetFiles(_extractPath, "*.*", SearchOption.AllDirectories);
                foreach (var item in files2)
                {
                    if (item.Contains("additional_component.fbx"))
                    {
                        flag = 1;
                        break;
                    }
                    else if (item.Contains("additional_component_neutral.obj"))
                    {
                        flag = 2;
                        break;
                    }

                }

                if (flag == 1)
                {

                    toImport.Add("USCBasicPack/additional_component.fbx");
                    _modelPath = Path.Combine(finalPath, "additional_component.fbx");
                }
                else if (flag == 2)
                {

                    toImport.Add("USCBasicPack/additional_component_neutral.obj");
                    _modelPath = Path.Combine(finalPath, "additional_component_neutral.obj");
                }
            }
            if (importSettings["back"] == "true")
            {
                toImport.Add("USCBasicPack/texture_diffuse_backhead.png");
                toImport.Add("USCBasicPack/texture_normal_backhead.png");
                toImport.Add("USCBasicPack/texture_specular_backhead.png");
            }
            toImport.Add("USCBasicPack/texture_diffuse.png");
            toImport.Add("USCBasicPack/texture_normal.png");
            toImport.Add("USCBasicPack/texture_specular.png");

        }
        else if (importSettings["resolution"] == "4k")
        {
            //_extractPath = Path.Combine(_extractPath, "/USCHighPack");

            _importPath = Path.Combine(_importPath, "default/");
            finalPath = Path.Combine(_importPath, "USCHighPack/");
            if (importSettings["parts"] == "model")
            {
                toImport.Add("USCHighPack/model.obj");
                _modelPath = Path.Combine(finalPath, "model.obj");

            }
            else if (importSettings["parts"] == "body")
            {
                toImport.Add("USCBasicPack/additional_body.fbx");
                _modelPath = Path.Combine(_importPath, "USCBasicPack/additional_body.fbx");
            }
            else if (importSettings["parts"] == "bs")
            {
                toImport.Add("USCBasicPack/additional_blendshape.fbx");
                _modelPath = Path.Combine(_importPath, "USCBasicPack/additional_blendshape.fbx");
            }
            else if (importSettings["parts"] == "eye")
            {
                int flag = 0;
                string[] files2 = Directory.GetFiles(_extractPath, "*.*", SearchOption.AllDirectories);
                foreach (var item in files2)
                {
                    if (item.Contains("additional_component.fbx"))
                    {
                        flag = 1;
                        break;
                    }
                    else if (item.Contains("additional_component_neutral.obj"))
                    {
                        flag = 2;
                        break;
                    }

                }

                if (flag == 1)
                {

                    toImport.Add("USCBasicPack/additional_component.fbx");
                    _modelPath = Path.Combine(_importPath, "USCBasicPack/additional_component.fbx");
                }
                else if (flag == 2)
                {

                    toImport.Add("USCBasicPack/additional_component_neutral.obj");
                    _modelPath = Path.Combine(_importPath, "USCBasicPack/additional_component_neutral.obj");
                }
            }
            if (importSettings["back"] == "true")
            {
                toImport.Add("USCBasicPack/texture_diffuse_backhead.png");
                toImport.Add("USCBasicPack/texture_normal_backhead.png");
                toImport.Add("USCBasicPack/texture_specular_backhead.png");
            }
            toImport.Add("USCHighPack/texture_diffuse.png");
            toImport.Add("USCHighPack/texture_normal.png");
            toImport.Add("USCHighPack/texture_specular.png");

        }

        foreach (string file in toImport) //import models and textures
        {
            CopyAndImport(file);
        }

        if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(_importPath, "Materials/PMat_Face.mat")))) //check if folder exists
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(_importPath, "Materials/PMat_Face.mat")));
        }
        if (importSettings["RP"] == "HDRP")
        {
            File.Copy(Path.Combine(_matPathHDRP, "Materials/PMat_Face.mat"), Path.Combine(_importPath, "Materials/PMat_Face.mat"), true); //copy and load Mats
        }
        else if (importSettings["RP"] == "URP")
        {
            File.Copy(Path.Combine(_matPathURP, "Materials/PMat_Face.mat"), Path.Combine(_importPath, "Materials/PMat_Face.mat"), true); //copy and load Mats
        }
        AssetDatabase.ImportAsset(Path.Combine(_importPath, "Materials/PMat_Face.mat"), ImportAssetOptions.ForceUpdate);

        if (importSettings["back"] == "true")
        {
            if (importSettings["RP"] == "HDRP")
            {
                File.Copy(Path.Combine(_matPathHDRP, "Materials/PMat_Back.mat"), Path.Combine(_importPath, "Materials/PMat_Back.mat"), true);
            }
            else if (importSettings["RP"] == "URP")
            {
                File.Copy(Path.Combine(_matPathURP, "Materials/PMat_Back.mat"), Path.Combine(_importPath, "Materials/PMat_Back.mat"), true);

            }

            AssetDatabase.ImportAsset(Path.Combine(_importPath, "Materials/PMat_Back.mat"), ImportAssetOptions.ForceUpdate);

            AssetDatabase.Refresh();

            _back_mat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(_importPath, "Materials/PMat_Back.mat"));
        }



        _face_mat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(_importPath, "Materials/PMat_Face.mat"));


        TextureImporter textureImporter = AssetImporter.GetAtPath(Path.Combine(finalPath, "texture_normal.png")) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.NormalMap;
            AssetDatabase.ImportAsset(Path.Combine(finalPath, "texture_normal.png"), ImportAssetOptions.ForceUpdate);
        }

        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture>(Path.Combine(finalPath, "texture_diffuse.png")) as Texture2D;
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture>(Path.Combine(finalPath, "texture_normal.png")) as Texture2D;
        Texture2D specular = AssetDatabase.LoadAssetAtPath<Texture>(Path.Combine(finalPath, "texture_specular.png")) as Texture2D;

        if (_face_mat == null)
        {
            UnityEngine.Debug.Log("face mat is null");
        }

        _face_mat.SetTexture("Albedo", diffuse);
        _face_mat.SetTexture("TangentNormal", normal);
        _face_mat.SetTexture("_Specular", specular);

        if (importSettings["back"] == "true")
        {
            TextureImporter textureImporterBack = AssetImporter.GetAtPath(Path.Combine(_importPath, "USCBasicPack/texture_normal_backhead.png")) as TextureImporter;
            if (textureImporterBack != null)
            {
                textureImporterBack.textureType = TextureImporterType.NormalMap;
                AssetDatabase.ImportAsset(Path.Combine(_importPath, "USCBasicPack/texture_normal_backhead.png"), ImportAssetOptions.ForceUpdate);
            }

            Texture Bdiffuse = AssetDatabase.LoadAssetAtPath<Texture>(Path.Combine(_importPath, "USCBasicPack/texture_diffuse_backhead.png"));
            Texture Bnormal = AssetDatabase.LoadAssetAtPath<Texture>(Path.Combine(_importPath, "USCBasicPack/texture_normal_backhead.png"));
            Texture Bspecular = AssetDatabase.LoadAssetAtPath<Texture>(Path.Combine(_importPath, "USCBasicPack/texture_specular_backhead.png"));
            _back_mat.SetTexture("Albedo", Bdiffuse);
            _back_mat.SetTexture("TangentNormal", Bnormal);
            _back_mat.SetTexture("Specular", Bspecular);
        }

        //import mats according to demand
        if (isClickedBody || isClickedEye)
        {
            if (importSettings["RP"] == "HDRP")
            {
                File.Copy(Path.Combine(_matPathHDRP, "Materials/PMat_Eye.mat"), Path.Combine(_importPath, "Materials/PMat_Eye.mat"), true);
                File.Copy(Path.Combine(_matPathHDRP, "Materials/PMat_Eyebrow.mat"), Path.Combine(_importPath, "Materials/PMat_Eyebrow.mat"), true);
                File.Copy(Path.Combine(_matPathHDRP, "Materials/PMat_Lacrimal.mat"), Path.Combine(_importPath, "Materials/PMat_Lacrimal.mat"), true);
                File.Copy(Path.Combine(_matPathHDRP, "Materials/PMat_TearLine.mat"), Path.Combine(_importPath, "Materials/PMat_TearLine.mat"), true);
                File.Copy(Path.Combine(_matPathHDRP, "Materials/PMat_Teeth.mat"), Path.Combine(_importPath, "Materials/PMat_Teeth.mat"), true);
            }
            else if (importSettings["RP"] == "URP")
            {
                File.Copy(Path.Combine(_matPathURP, "Materials/PMat_Eye.mat"), Path.Combine(_importPath, "Materials/PMat_Eye.mat"), true);
                File.Copy(Path.Combine(_matPathURP, "Materials/PMat_Eyebrow.mat"), Path.Combine(_importPath, "Materials/PMat_Eyebrow.mat"), true);
                File.Copy(Path.Combine(_matPathURP, "Materials/PMat_Lacrimal.mat"), Path.Combine(_importPath, "Materials/PMat_Lacrimal.mat"), true);
                File.Copy(Path.Combine(_matPathURP, "Materials/PMat_TearLine.mat"), Path.Combine(_importPath, "Materials/PMat_TearLine.mat"), true);
                File.Copy(Path.Combine(_matPathURP, "Materials/PMat_Teeth.mat"), Path.Combine(_importPath, "Materials/PMat_Teeth.mat"), true);
            }
            AssetDatabase.ImportAsset(Path.Combine(_importPath, "Materials/PMat_Eye.mat"), ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Path.Combine(_importPath, "Materials/PMat_Eyebrow.mat"), ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Path.Combine(_importPath, "Materials/PMat_Lacrimal.mat"), ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Path.Combine(_importPath, "Materials/PMat_TearLine.mat"), ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Path.Combine(_importPath, "Materials/PMat_Teeth.mat"), ImportAssetOptions.ForceUpdate);
            _eye_mat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(_importPath, "Materials/PMat_Eye.mat"));
            _brow_mat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(_importPath, "Materials/PMat_Eyebrow.mat"));
            _lacrimal_mat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(_importPath, "Materials/PMat_Lacrimal.mat"));
            _tearLine_mat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(_importPath, "Materials/PMat_TearLine.mat"));
            _teeth_mat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(_importPath, "Materials/PMat_Teeth.mat"));
        }




    }


    private void LoadModel()
    {
        Debug.Log(_modelPath);
        if (Path.GetExtension(_modelPath).ToLower() == ".fbx" && importSettings["parts"] == "body") //change AnimationTyoe to humanoid
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(_modelPath) as ModelImporter;

            if (modelImporter != null)
            {
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.SaveAndReimport();
            }
            else
            {
                UnityEngine.Debug.LogWarning("ModelImporter not found for path: " + _modelPath);
            }
        }
        else if (importSettings["parts"] == "eye")
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(_modelPath) as ModelImporter;

            if (modelImporter != null)
            {
                modelImporter.animationType = ModelImporterAnimationType.Generic; 
                
                modelImporter.isReadable = true;
                string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
                PropertyInfo prop = modelImporter.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                prop.SetValue(modelImporter, true);
                EditorUtility.SetDirty(modelImporter);

                modelImporter.SaveAndReimport();
            }
            else
            {
                UnityEngine.Debug.LogWarning("ModelImporter not found for path: " + _modelPath);
            }
        }
        else if (importSettings["parts"] == "bs")
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(_modelPath) as ModelImporter;

            if (modelImporter != null)
            {
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.SaveAndReimport();
            }
            else
            {
                UnityEngine.Debug.LogWarning("ModelImporter not found for path: " + _modelPath);
            }
        }

        UnityEngine.Object modelObj = AssetDatabase.LoadAssetAtPath(_modelPath, typeof(GameObject));  //Load

        if (modelObj == null)  //Check if null
        {
            UnityEngine.Debug.LogError("Model not found. Check the path: " + _modelPath);
            return;
        }

        _model = (GameObject)PrefabUtility.InstantiatePrefab(modelObj);
        Debug.Log(_model.name);

        if (_model == null)
        {
            UnityEngine.Debug.LogError("Failed to instantiate the prefab in Editor");
            return;
        }

        if (_face_mat != null)
        {
            Renderer[] renderers = _model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                Debug.Log("lenth is:" + materials.Length);
                for (int i = 0; i < materials.Length; i++)
                {
                    Debug.Log(materials[i]?.name);//for testing
                    if (importSettings["parts"] == "model")
                    {
                        SetRenderMat(materials, "defaultMat", _face_mat);
                    }
                    else if (importSettings["parts"] == "body" || importSettings["parts"] == "eye")
                    {
                        if (importSettings["back"] == "true")
                        {
                            SetRenderMat(materials, "M_Back", _back_mat);
                        }
                        SetRenderMat(materials, "right_eyeball", _eye_mat);
                        SetRenderMat(materials, "left_eyeball", _eye_mat);
                        SetRenderMat(materials, "M_EyeLa", _brow_mat);
                        SetRenderMat(materials, "teeth.", _teeth_mat);
                        SetRenderMat(materials, "Occ", _tearLine_mat);
                        SetRenderMat(materials, "Fluid", _lacrimal_mat);
                        SetRenderMat(materials, "teeth_fluid", _tearLine_mat);
                        
                        SetRenderMat(materials, "M_Face", _face_mat);

                    }
                    else if (importSettings["parts"] == "bs")
                    {

                        SetRenderMat(materials, "M_Face", _face_mat);
                        if (importSettings["back"] == "true")
                        {
                            SetRenderMat(materials, "M_Back", _back_mat);
                        }

                    }
                }
                renderer.sharedMaterials = materials;
            }
        }

        // Instantiate

        _model.SetActive(true);
        _model.transform.position = Vector3.zero;
        if (importSettings["parts"] != "body")
        {
            _model.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
        Selection.activeGameObject = _model;
    }


    private void SetRenderMat(Material[] materials, string matName, Material newMat)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
            {
                continue;
            }
            if (materials[i].name.Contains(matName))
            {
                //UnityEngine.Debug.Log(materials[i].name);
                // Change the material
                materials[i] = newMat;
            }
        }
    }

    private void SetLiveLink()
    {
        UnityEngine.Object LLprefab;
        if (importSettings["parts"] == "body")
        {
            LLprefab = AssetDatabase.LoadAssetAtPath("Assets/Deemos/" + importSettings["RP"] + "/Prefabs/BodyLL.prefab", typeof(GameObject));
        }
        else
        {
            LLprefab = AssetDatabase.LoadAssetAtPath("Assets/Deemos/" + importSettings["RP"] + "/Prefabs/HeadLL.prefab", typeof(GameObject));
        }

        GameObject instantiatedPrefab = PrefabUtility.InstantiatePrefab(LLprefab) as GameObject;
        PrefabUtility.UnpackPrefabInstance(instantiatedPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instantiatedPrefab.SetActive(true);
        Transform bodyImported = instantiatedPrefab.transform.Find("BodyImported");
        foreach (UnityEngine.Component component in bodyImported.gameObject.GetComponents<UnityEngine.Component>())
        {
            //UnityEngine.Debug.Log("qwq");
            UnityEditorInternal.ComponentUtility.CopyComponent(component);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(_model);
        }



        // Check if bodyI exists
        if (bodyImported != null)
        {
            Transform templateRecorder = bodyImported.Find("Take Recorder");


            templateRecorder.transform.SetParent(_model.transform);
            // Set the parent of the new model to be ""
            _model.transform.SetParent(instantiatedPrefab.transform);
            GameObject oldmodel = _model;
            _model.tag = ("ChatAvatar");
            _model.transform.position = importSettings["parts"] == "body" ? new Vector3(-0.045f, 0, 0) : new Vector3(0, 1.725f, 0);

            FacialController.HeadGroup headGroup = _model.GetComponent<FacialController.HeadGroup>();
            if (headGroup == null)
            {
                headGroup = _model.AddComponent<FacialController.HeadGroup>();
            }
            headGroup.m_headComponents = new List<GameObject>();
            headGroup.m_headComponents.Add(_model);
            headGroup.m_skinnedMeshRendererItems = new List<FacialController.SkinnedMeshRendererItem>();
            headGroup.m_skinnedMeshRendererItems.Add(null);
            headGroup.Start();
            _model = instantiatedPrefab;

            Transform thirdPersonObject = _model.transform.Find("ThirdPerson_Objects");
            //thirdPersonObject.gameObject.SetActive(false);
            thirdPersonObject.Find("MainCamera").gameObject.SetActive(false);
            thirdPersonObject.Find("PlayerFollowCamera").gameObject.SetActive(false);

            Transform geometry = thirdPersonObject.Find("PlayerArmature");
            geometry.GetComponent<CharacterController>().enabled = false;
            geometry.GetComponent<ThirdPersonController>().enabled = false;
            geometry.GetComponent<PlayerInput>().enabled = false;
            geometry.GetComponent<StarterAssetsInputs>().cursorLocked = false;
            geometry.GetComponent<StarterAssetsInputs>().cursorInputForLook = false;


            Transform newDevice = oldmodel.transform.Find("Take Recorder/New FaceDevice");
            if (newDevice == null)
            {
                UnityEngine.Debug.LogWarning("newDevice is null");
            }
            FaceDevice faceDevice = newDevice.GetComponent<FaceDevice>();
            FaceActor faceActor = oldmodel.GetComponent<FaceActor>();
            FaceMapper faceMapper = AssetDatabase.LoadAssetAtPath("Assets/Deemos/" + importSettings["RP"] + "/Prefabs/FaceMapperHead.asset", typeof(UnityEngine.Object)) as FaceMapper;
            if (importSettings["parts"] != "body") 
            {
                faceActor.SetMapper(faceMapper);
            }

            if (faceDevice != null)
            {
                faceDevice.Actor = faceActor;
            }
            else
            {
                UnityEngine.Debug.LogWarning("ARKit Face Device component not found on the prefab.");
            }

            Transform takeRecorder = oldmodel.transform.Find("Take Recorder");
            PlayableDirector director = takeRecorder.gameObject.GetComponent<PlayableDirector>();
            //var obj = new SerializedObject(director);
            //var bindings = obj.FindProperty("m_SceneBindings");
            //var binding = bindings.GetArrayElementAtIndex(0);
            //var trackProp = binding.FindPropertyRelative("key");
            ////var sceneObjProp = binding.FindPropertyRelative("value");
            //var track = trackProp.objectReferenceValue;
            //director.SetGenericBinding(track, oldmodel.GetComponent<Animator>());

            DestroyImmediate(bodyImported.gameObject);
            _model.transform.localPosition = Vector3.zero;
            UnityEngine.Debug.Log("Set Livelink complete");
            _model.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Set vars in DemoScene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "ModelTestScene")
            {
                GameObject m_interactionUI = GameObject.Find("InteractionUI");
                HeadUI m_headUI = m_interactionUI.GetComponent<HeadUI>();
                m_headUI.m_FaceActor = faceActor;
                m_headUI.m_HeadGroup = headGroup;
                m_headUI.m_PresetPlayer = director;
                //m_headUI.m_TakeRecorder = takeRecorder.GetComponent<TakeRecorder>();
                m_headUI.m_FaceDevice = faceDevice;

                FacialControllers m_facialController = GameObject.Find("ControlRigPanel").GetComponent<FacialControllers>();
                m_facialController.m_HeadGroup = headGroup;

                GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (GameObject go in rootObjects)
                {
                    if (go.name == "Main Camera Root")
                    {
                        go.SetActive(true);
                    }
                    if (go.name == "Camera_FullShot_root")
                    {
                        Transform tr = go.transform.Find("Camera Closeup");
                        tr.position = new Vector3(0, 1.68f, -0.5f);
                        Transform tr1 = go.transform.Find("Camera MidShot");
                        tr1.position = new Vector3(0, 1.7f, -1.5f);
                    }
                }
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("BodyImported not found in prefab.");
        }
        
    }


    private void SetBodyControl()
    {

        if (importSettings["parts"] == "body")
        {
            UnityEngine.Object LLprefab = AssetDatabase.LoadAssetAtPath("Assets/Deemos/" + importSettings["RP"] + "/Prefabs/BodyLL.prefab", typeof(GameObject));
            GameObject instantiatedPrefab = PrefabUtility.InstantiatePrefab(LLprefab) as GameObject;
            PrefabUtility.UnpackPrefabInstance(instantiatedPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instantiatedPrefab.SetActive(true);
            Transform bodyImported = instantiatedPrefab.transform.Find("BodyImported");
            foreach (UnityEngine.Component component in bodyImported.gameObject.GetComponents<UnityEngine.Component>())
            {
                //UnityEngine.Debug.Log("qwq");
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(_model);
            }


            // Check if bodyI exists
            if (bodyImported != null)
            {
                Transform templateRecorder = bodyImported.Find("Take Recorder");
                templateRecorder.transform.SetParent(_model.transform);

                // Set the parent of the new model to be ""
                _model.transform.SetParent(instantiatedPrefab.transform);
                GameObject oldmodel = _model;
                _model.tag = ("ChatAvatar");
                _model.transform.position = new Vector3(-0.045f, 0, 0);
                _model = instantiatedPrefab;

                Transform thirdPersonObject = _model.transform.Find("ThirdPerson_Objects");
                thirdPersonObject.gameObject.SetActive(true);


                Transform newDevice = oldmodel.transform.Find("Take Recorder/New FaceDevice");
                if (newDevice == null)
                {
                    UnityEngine.Debug.LogWarning("newDevice is null");
                }


                FaceDevice faceDevice = newDevice.GetComponent<FaceDevice>();

                FaceActor faceActor = oldmodel.GetComponent<FaceActor>();
                if (faceDevice != null)
                {
                    faceDevice.Actor = faceActor;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("ARKit Face Device component not found on the prefab.");
                }

                // just set body control, so set them falses
                faceActor.enabled = false;
                faceDevice.enabled = false;

                DestroyImmediate(bodyImported.gameObject);
                _model.transform.localPosition = Vector3.zero;
                UnityEngine.Debug.Log("set body complete");
                _model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                UnityEngine.Debug.LogWarning("BodyImported not found in prefab.");
            }

            // Set vars in DemoScene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "ModelTestScene")
            {
                GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (GameObject go in rootObjects)
                {
                    if (go.name == "Camera_FullShot_root")
                    {
                        Transform tr = go.transform.Find("Camera Closeup");
                        tr.position = new Vector3(0, 1.6f, -0.5f);
                        Transform tr1 = go.transform.Find("Camera MidShot");
                        tr1.position = new Vector3(0, 1.65f, -1.5f);
                        break;
                    }
                }
            }

        }
        else
        {
            UnityEngine.Debug.LogWarning("body not imported"); ;
        }



    }


    private void SetBoth()
    {
        if (importSettings["parts"] == "body")
        {
            UnityEngine.Object LLprefab = AssetDatabase.LoadAssetAtPath("Assets/Deemos/" + importSettings["RP"] + "/Prefabs/BodyLL.prefab", typeof(GameObject));
            GameObject instantiatedPrefab = PrefabUtility.InstantiatePrefab(LLprefab) as GameObject;
            PrefabUtility.UnpackPrefabInstance(instantiatedPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instantiatedPrefab.SetActive(true);
            Transform bodyImported = instantiatedPrefab.transform.Find("BodyImported");
            foreach (UnityEngine.Component component in bodyImported.gameObject.GetComponents<UnityEngine.Component>())
            {
                //UnityEngine.Debug.Log("qwq");
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(_model);
            }

            // Check if bodyI exists
            if (bodyImported != null)
            {
                Transform templateRecorder = bodyImported.Find("Take Recorder");


                templateRecorder.transform.SetParent(_model.transform);
                // Set the parent of the new model to be ""
                _model.transform.SetParent(instantiatedPrefab.transform);
                GameObject oldmodel = _model;
                _model.tag = ("ChatAvatar");
                _model.transform.position = new Vector3(-0.045f, 0, 0);

                // Add HeadGroup
                FacialController.HeadGroup headGroup = _model.AddComponent<FacialController.HeadGroup>();
                headGroup.m_headComponents = new List<GameObject>();
                headGroup.m_headComponents.Add(_model);
                headGroup.m_skinnedMeshRendererItems = new List<FacialController.SkinnedMeshRendererItem>();
                headGroup.m_skinnedMeshRendererItems.Add(null);
                headGroup.Start();

                _model = instantiatedPrefab;

                Transform thirdPersonObject = _model.transform.Find("ThirdPerson_Objects");
                thirdPersonObject.gameObject.SetActive(true);

                Transform newDevice = oldmodel.transform.Find("Take Recorder/New FaceDevice");
                if (newDevice == null)
                {
                    UnityEngine.Debug.LogWarning("newDevice is null");
                }


                FaceDevice faceDevice = newDevice.GetComponent<FaceDevice>();

                FaceActor faceActor = oldmodel.GetComponent<FaceActor>();
                if (faceDevice != null)
                {
                    faceDevice.Actor = faceActor;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("ARKit Face Device component not found on the prefab.");
                }


                Transform takeRecorder = oldmodel.transform.Find("Take Recorder");
                PlayableDirector director = takeRecorder.gameObject.GetComponent<PlayableDirector>();
                var obj = new SerializedObject(director);
                var bindings = obj.FindProperty("m_SceneBindings");
                var binding = bindings.GetArrayElementAtIndex(0);
                var trackProp = binding.FindPropertyRelative("key");
                //var sceneObjProp = binding.FindPropertyRelative("value");
                var track = trackProp.objectReferenceValue;
                director.SetGenericBinding(track, oldmodel);

                //TakeRecorder recorder = takeRecorder.gameObject.GetComponent<TakeRecorder>();
                //var obj = new SerializedObject(recorder);
                //var bindings = obj.FindProperty("m_SceneBindings");
                //var binding = bindings.GetArrayElementAtIndex(0);
                //var trackProp = binding.FindPropertyRelative("key");
                ////var sceneObjProp = binding.FindPropertyRelative("value");
                //var track = trackProp.objectReferenceValue;
                //recorder.SetGenericBinding(track, oldmodel);


                DestroyImmediate(bodyImported.gameObject);
                _model.transform.localPosition = Vector3.zero;
                UnityEngine.Debug.Log("set both complete");
                _model.transform.localRotation = Quaternion.Euler(0, 180, 0);

                // Set vars in DemoScene
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "ModelTestScene")
                {
                    GameObject m_interactionUI = GameObject.Find("InteractionUI");
                    HeadUI m_headUI = m_interactionUI.GetComponent<HeadUI>();
                    m_headUI.m_FaceActor = faceActor;
                    m_headUI.m_HeadGroup = headGroup;
                    m_headUI.m_PresetPlayer = director;
                    //m_headUI.m_TakeRecorder = takeRecorder.GetComponent<TakeRecorder>();
                    m_headUI.m_FaceDevice = faceDevice;

                    FacialControllers m_facialController = GameObject.Find("ControlRigPanel").GetComponent<FacialControllers>();
                    m_facialController.m_HeadGroup = headGroup;

                    GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (GameObject go in rootObjects)
                    {
                        if (go.name == "Camera_FullShot_root")
                        {
                            Transform tr = go.transform.Find("Camera Closeup");
                            tr.position = new Vector3(0, 1.6f, -0.5f);
                            Transform tr1 = go.transform.Find("Camera MidShot");
                            tr1.position = new Vector3(0, 1.65f, -1.5f);
                            break;
                        }
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("BodyImported not found in prefab.");
            }

        }
        else
        {
            UnityEngine.Debug.LogWarning("body not imported"); ;
        }
    }
}
