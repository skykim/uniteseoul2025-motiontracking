using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPPCameraLogic : MonoBehaviour
{
    CinemachineVirtualCamera m_camera;
    Cinemachine3rdPersonFollow followLogic;
    CinemachineCameraOffset offsetLogic;

    float velocity = 0.0f;
    [SerializeField]
    float ZoomSensitivity = 1f;
    [SerializeField]
    float LiftSensitivity = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        m_camera = gameObject.GetComponent<CinemachineVirtualCamera>();
        followLogic = m_camera.GetCinemachineComponent(CinemachineCore.Stage.Body) as Cinemachine3rdPersonFollow;
        offsetLogic = gameObject.GetComponent<CinemachineCameraOffset>();
    }

    // Update is called once per frame
    void Update()
    {
        float moveY = 0f;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        followLogic.CameraDistance = Mathf.Clamp(followLogic.CameraDistance - scroll * ZoomSensitivity, 0.5f, 10f);


        if (Input.GetKey(KeyCode.E))
        {
            velocity += Time.deltaTime;
            velocity = Mathf.Clamp01(velocity) * LiftSensitivity;
            moveY = velocity;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            velocity += Time.deltaTime;
            velocity = Mathf.Clamp01(velocity) * LiftSensitivity;
            moveY = -velocity;
        }
        else
        {
            velocity -= Time.deltaTime * LiftSensitivity;
            velocity = Mathf.Clamp01(velocity) * LiftSensitivity;
        }

        offsetLogic.m_Offset.y += moveY;
    }
}
