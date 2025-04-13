using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.LiveCapture.ARKitFaceCapture;
using UnityEditor;
using UnityEngine;

namespace FacialController
{
    [Serializable]
    public class SkinnedMeshRendererItem
    {
        [SerializeField]
        public SkinnedMeshRenderer m_SkinnedMeshRenderer;
        [SerializeField]
        public string prefix;
        Dictionary<string, int> m_NameIndexCache = new Dictionary<string, int>();
        int bs_count;
        string[] bs_names;

        public SkinnedMeshRendererItem(SkinnedMeshRenderer renderer)
        {
            m_SkinnedMeshRenderer = renderer;
            Init();
        }

        public void Init()
        {
            if (m_SkinnedMeshRenderer == null)
            {
                return;
            }
            bs_count = m_SkinnedMeshRenderer.sharedMesh.blendShapeCount;
            bs_names = new string[bs_count];
            for (int i = 0; i < bs_count; i++)
            {
                bs_names[i] = m_SkinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
            }
            //prefix = HelperFuncs.BSPrefix(bs_names);
            prefix = string.Empty;

        }

        public int GetBlendShapeIndex(string name)
        {
            int index;
            if (m_NameIndexCache.TryGetValue(name, out index))
            {
                return index;
            }
            index = -1;
            for (int i = 0; i < bs_count; i++)
            {
                string bs_name = bs_names[i];
                if (bs_name.Contains(name))
                {
                    if (index != -1)
                    {
                        Debug.LogWarning(bs_name[i] + " and " + bs_name[index] + " both have name " + name + "!");
                    }
                    //Debug.Log(name + " -> " + bs_name);
                    index = i;
                    //break;
                }
            }
            m_NameIndexCache[name] = index;
            return index;
        }

        public bool SetBlendShapeWeight(string name, float value)
        {
            int index = GetBlendShapeIndex(name);
            if (index == -1)
            {
                return false;
            }
            SetBlendShapeWeight(GetBlendShapeIndex(name), value);
            return true;
        }
        public bool SetBlendShapeWeight(int index, float value)
        {
            m_SkinnedMeshRenderer.SetBlendShapeWeight(index, value);
            return true;
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(HeadGroup))]
    class HeadGroupEditor : Editor
    {
        HeadGroup headGroup;
        private void OnEnable()
        {
            headGroup = (HeadGroup)target;
        }
        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();
            if (GUILayout.Button("Load All SkinnedMeshRenderer"))
            {
                headGroup.Start();
            }
            if (GUILayout.Button("Reset BlendShape"))
            {
                headGroup.ResetBlendShape();
            }
        }
    }
    #endif

    public class HeadGroup: MonoBehaviour
    {
        [SerializeField]
        public List<GameObject> m_headComponents;
        [SerializeField]
        public List<SkinnedMeshRendererItem> m_skinnedMeshRendererItems;
        Dictionary<Tuple<SkinnedMeshRendererItem, string>, int> m_rendererLookup = new Dictionary<Tuple<SkinnedMeshRendererItem, string>, int>();
        public void Start()
        {
            if (m_skinnedMeshRendererItems.Count != 0) {
                m_skinnedMeshRendererItems.Clear();
            }
            foreach (var gameObject in m_headComponents)
            {
                foreach (var skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    m_skinnedMeshRendererItems.Add(new SkinnedMeshRendererItem(skinnedMeshRenderer));
                }
            }
        }

        public void SetBlendShapeWeight(string bs_name, float value)
        {
            foreach (var rendererItem in m_skinnedMeshRendererItems)
            {
                int lookup_result;
                var lookup_key = new Tuple<SkinnedMeshRendererItem, string>(rendererItem, bs_name);
                if (!m_rendererLookup.TryGetValue(lookup_key, out lookup_result))
                {
                    lookup_result = m_rendererLookup[lookup_key] = rendererItem.GetBlendShapeIndex(bs_name);
                }
                if (lookup_result != -1)
                {
                    rendererItem.SetBlendShapeWeight(lookup_result, value);
                }
            }
        }

        public void ResetBlendShape()
        {
            foreach (var handle in m_skinnedMeshRendererItems)
            {
                var renderer = handle.m_SkinnedMeshRenderer;
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    renderer.SetBlendShapeWeight(i, 0f);
                }
            }
        }
    }
}
