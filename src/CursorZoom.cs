using ICities;
using UnityEngine;

namespace CursorZoom
{
    public class CursorZoomMod : IUserMod
    {
        public string Name
        {
            get { return "Cursor Zoom"; }
        }

        public string Description
        {
            get { return "Zooms the camera keeping the cursor position constant"; }
        }
    }


    public class CursorZoomLoader: LoadingExtensionBase
    {
        CursorZoomBehaviour instance;

        public override void OnLevelUnloading()
        {
            if (instance != null)
            {
                MonoBehaviour.Destroy(instance);
            }
            base.OnLevelUnloading();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            instance = GameObject.FindObjectOfType<CameraController>().gameObject.AddComponent<CursorZoomBehaviour>();
            base.OnLevelLoaded(mode);
        }
    }


    public class CursorZoomBehaviour: MonoBehaviour
    {
        private CameraController cameraController;

        private static RayCaster raycaster = new RayCaster();

        private void Start()
        {
            cameraController = GameObject.FindObjectOfType<CameraController>();

            // originally 5000, this value causes tilt to change as you zoom in and out.
            // we need to disable this behaviour for fixed-cursor-on-zoom to make sense.
            cameraController.m_maxTiltDistance = 1000000f;
        }

        private float frameInitialCurrentSize;

        private RayCaster.RaycastInput frameInitialMouseRaycastInput;

        private void Update()
        {
            frameInitialCurrentSize = cameraController.m_currentSize;
            frameInitialMouseRaycastInput = new RayCaster.RaycastInput(
                Camera.main.ScreenPointToRay(Input.mousePosition),
                Camera.main.farClipPlane
            );
        }

        private void LateUpdate()
        {
            if (frameInitialCurrentSize == 0f || cameraController.m_currentSize == 0f || frameInitialCurrentSize == cameraController.m_currentSize)
            {
                return;
            }
            RayCaster.RaycastOutput output;
            if (raycaster.CastRay(frameInitialMouseRaycastInput, out output))
            {
                var fractionTowardsCursor = 1f - cameraController.m_currentSize / frameInitialCurrentSize;
                var correction =  fractionTowardsCursor * (output.m_hitPos - cameraController.m_currentPosition);
                correction.y = 0f;
                cameraController.transform.position += correction;
                cameraController.m_targetPosition += correction;
                cameraController.m_currentPosition += correction;
            }
        }
    }

    class RayCaster : ToolBase
    {
        public bool CastRay(RaycastInput input, out RaycastOutput output)
        {
            return RayCast(input, out output);
        }
    }
}
