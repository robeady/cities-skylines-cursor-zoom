using ICities;
using UnityEngine;
using System.Reflection;

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
        private static CameraController cameraController;

        private static RayCaster raycast = new RayCaster();

		void Start()
		{
            DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, "[CursorZoom] Hello!");

            cameraController = GameObject.FindObjectOfType<CameraController>();

            var oldMethod = typeof(CameraController).GetMethod("ClampCameraPosition", BindingFlags.Public | BindingFlags.Static);
            var newMethod = typeof(CursorZoomBehaviour).GetMethod("ClampCameraPosition", BindingFlags.NonPublic | BindingFlags.Static);
            RedirectionHelper.RedirectCalls(oldMethod, newMethod);

            cameraController.m_maxTiltDistance = 1000000f;
		}

        private static float frameInitialCurrentSize;

        private static bool readyForMagic;

        private void Update()
        {
            readyForMagic = false;
            if (cameraController != null)
            {
                frameInitialCurrentSize = cameraController.m_currentSize;
            }
        }

        private static Vector3 ClampCameraPosition(Vector3 position)
        {
            if (readyForMagic && frameInitialCurrentSize != cameraController.m_currentSize)
            {
                position = FixCurrentPosition(position);
            }
            else if (cameraController != null && position == cameraController.m_targetPosition)
            {
                // we are now at the end of the UpdateTargetPosition function
                // on the next call to this function we will be in UpdateTransform and then we can do our magic
                readyForMagic = true;
            }

            // original clamping code follows
            float num = 8640f;
            if (position.x < -num)
            {
                position.x = -num;
            }
            if (position.x > num)
            {
                position.x = num;
            }
            if (position.z < -num)
            {
                position.z = -num;
            }
            if (position.z > num)
            {
                position.z = num;
            }
            return position;
        }

        private static Vector3 FixCurrentPosition(Vector3 position)
        {
            // at this moment, we've been passed m_currentPosition + some rotation.
            // we can figure out from how much m_currentSize has changed what the zoom factor was,
            // and adjust x and z accordingly, by incrementing position.x and position.z that we return.
            // we will need to apply the same adjustment to m_currentPosition and m_targetPosition.

            if (frameInitialCurrentSize == 0f || cameraController.m_currentSize == 0f)
            {
                return position;
            }

            float m_mouseRayLength = Camera.main.farClipPlane;
            Ray m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            ToolBase.RaycastInput input = new ToolBase.RaycastInput(m_mouseRay, m_mouseRayLength);
            ToolBase.RaycastOutput output;
            if (raycast.RayCasted(input, out output))
            {
                var hitPosition = output.m_hitPos;
                Debug.Log("current size changed from " + frameInitialCurrentSize + " to " + cameraController.m_currentSize);
                var sizeFraction = frameInitialCurrentSize / cameraController.m_currentSize;

                var fractionTowardsCursor = 1f - 1f / sizeFraction;
                var deltaX =  fractionTowardsCursor * (hitPosition.x - cameraController.m_currentPosition.x);
                var deltaZ = fractionTowardsCursor * (hitPosition.z - cameraController.m_currentPosition.z);

                Debug.Log("fraction towards cursor = " + fractionTowardsCursor);
                Debug.Log("original current pos = " + cameraController.m_currentPosition);

                position.x += deltaX;
                position.z += deltaZ;
                cameraController.m_targetPosition.x += deltaX;
                cameraController.m_targetPosition.z += deltaZ;
                cameraController.m_currentPosition.x += deltaX;
                cameraController.m_currentPosition.z += deltaZ;
            }

            return position;
        }
	}
}
