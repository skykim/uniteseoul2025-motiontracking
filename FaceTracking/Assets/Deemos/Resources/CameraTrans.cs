using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class CameraTrans : MonoBehaviour
{
    private List<GameObject> m_Model;
    private GameObject m_Target;
    public Transform MidShotCamera;
    public Transform CloseupCamera;
    private bool init = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.M))
        if (!init)
        {
            m_Model = GameObject.FindGameObjectsWithTag("BodyTPP").ToList<GameObject>();

            if (m_Model.Count == 0)
            {
                GameObject[] GOs = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (GameObject go in GOs)
                {
                    if (go.name == "additional_body")
                    {
                        Debug.Log(go.name);
                        m_Model.Add(go);
                    }
                }
            }
            if (m_Model.Count == 0)
            {
                Debug.Log("There is no BodyTPP in the scene.");
            }
            else
            {
                m_Target = m_Model[0];
                Transform[] gos = m_Target.GetComponentsInChildren<Transform>();
                Transform targetTrans = null;
                Transform targetTransHead = null;
                for (int i = 0; i < gos.Length; i++)
                {
                    if (gos[i].name == "with_rigged_body_Head_M")
                    {
                        targetTrans = gos[i];
                        Debug.Log(targetTrans);
                    }
                    if (gos[i].name == "additional_component")
                    {
                        targetTransHead = gos[i];
                        Debug.Log(targetTransHead);
                    }
                }

                if (targetTrans != null)
                {
                    if (targetTrans.position.y > 1f)
                    {
                        Debug.Log(targetTrans.position);
                        Debug.Log(MidShotCamera.position);
                        Debug.Log(CloseupCamera.position);
                        MidShotCamera.position = new Vector3(MidShotCamera.position.x, targetTrans.position.y, MidShotCamera.position.z);
                        CloseupCamera.position = GraphicsSettings.currentRenderPipeline.GetType().Name == "HDRenderPipelineAsset" ? new Vector3(CloseupCamera.position.x, targetTrans.position.y - 0.06f, CloseupCamera.position.z) : new Vector3(CloseupCamera.position.x, targetTrans.position.y - 0.03f, CloseupCamera.position.z);
                        Debug.Log(MidShotCamera.position);
                        Debug.Log(CloseupCamera.position);

                        init = true;
                    }

                }
                else if (targetTransHead != null)
                {
                    if (targetTransHead.position.y > 1f)
                    {
                        MidShotCamera.position = new Vector3(MidShotCamera.position.x, 1.7f, MidShotCamera.position.z);
                        CloseupCamera.position = new Vector3(CloseupCamera.position.x, 1.68f, CloseupCamera.position.z);

                        init = true;
                    }
                }
                else
                {
                    Debug.Log("Not Body component!");
                }
            }
        }
    }
}
