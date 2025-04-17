# uniteseoul2025-motiontracking
Unite Seoul 2025 Samples for Motion Tracking Session (Slide: [Korean](https://github.com/skykim/uniteseoul2025-motiontracking/blob/main/(distributed)_slide.pdf), [English](https://github.com/skykim/uniteseoul2025-motiontracking/blob/main/(distributed)_slide_en.pdf))

## Requirements ##
- Unity 6000.0.40f1
- Sentis 2.1.2

## Samples ##

### 1. Face Tracking ###
- Model: MediaPipe BlazeFace (Short Range), FashMesh-V2, Blendshape-V2
- Avatar: [ChatAvatar](https://hyper3d.ai/chatavatar)
- Dependency: [ChatAvatar for Unity](https://deemos.gumroad.com/l/ChatAvatarImportTool-Unity)

[![FaceTracking](https://img.youtube.com/vi/pgqtfsEd8xg/0.jpg)](https://www.youtube.com/watch?v=pgqtfsEd8xg)

### 2. Hands Tracking ###
- Model: MediaPipe Hand landmarker, Gesture Embedder, Gesture Classifier

[![HandsTracking](https://img.youtube.com/vi/sIXdtmpgyI8/0.jpg)](https://www.youtube.com/watch?v=sIXdtmpgyI8)

### 3. Pose Tracking ###
- Model: MediaPipe BlazePose Pose Detector, BlazePose GHUM 3D, (+ Retargeting)
- Avatar: [Mixamo](https://www.mixamo.com/), [VRoid](https://github.com/hinzka/52blendshapes-for-VRoid-face/tree/main)
- Dependency: [UniVRM](https://github.com/vrm-c/UniVRM/releases/tag/v0.128.3)
- Note: If the avatar does not load after the first execution, please re-import the Avatar/VRoid_V110_Male_v1.1.3.prefab and .vrm files.

[![PoseTracking](https://img.youtube.com/vi/D1YAG6eKwXo/0.jpg)](https://www.youtube.com/watch?v=D1YAG6eKwXo)

### 4. Multi-Person Pose Tracking ###
- Model: Yolov8n-pose

[![Multi-PersonTracking](https://img.youtube.com/vi/WvKL3Q2Pho8/0.jpg)](https://www.youtube.com/watch?v=WvKL3Q2Pho8)

### 5. XR Fullbody Tracking ###
- SDK: Meta Movement SDK

<img width="480" alt="Image" src="https://github.com/user-attachments/assets/792f5feb-4b92-4f6d-8fdd-45f72b4fef29" />
  
### 6. XR Pose Tracking ###
- SDK: Meta Passthrough Camera API
- Model: Yolov8n-pose

[![XRPoseTracking](https://img.youtube.com/vi/DpoQ3r1R8Bw/0.jpg)](https://www.youtube.com/watch?v=DpoQ3r1R8Bw)
