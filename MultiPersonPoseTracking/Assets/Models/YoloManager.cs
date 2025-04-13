using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using Unity.Sentis;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class YoloManager : MonoBehaviour
{
    public RunYOLO8nPose yoloPoseModel;

    public BackendType backendType = BackendType.GPUCompute;

    public RawImage displayImage;

    public TMP_Text fpsText;
    public TMP_Text numOfPeopleText;

    private WebCamTexture webcamTexture;      
    private VideoPlayer videoPlayer;

    public bool isLiveCamera = false;
    public bool isYoloPoseModel = false;

    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        if (isLiveCamera)
        {
            webcamTexture = new WebCamTexture();
            webcamTexture.Play();
        }
        else
        {
            videoPlayer = GetComponent<VideoPlayer>();
            videoPlayer.Play();
        }

        yoloPoseModel.Initialize(backendType, displayImage);
        
    }
    async void Update()
    {
        Texture inputTexture;

        if (isLiveCamera)
        {
            inputTexture = webcamTexture;
        }
        else
        {
            if (videoPlayer.texture == null)
                return;

            inputTexture = videoPlayer.texture;
        }

        //calculate fps
        float deltaTime = Time.deltaTime;
        float fpsValue = 1.0f / deltaTime;
        fpsText.text = $"{fpsValue:F2}fps";

        int result = 0;
        result = await yoloPoseModel.ExecuteModel(inputTexture);
        if (result>0)
            numOfPeopleText.text = $"{result} people";
    }
}
