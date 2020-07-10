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

        private static RayCaster raycaster = new RayCaster();

        private MethodInfo oldClampMethod = typeof(CameraController).GetMethod("ClampCameraPosition", BindingFlags.Public | BindingFlags.Static);
        private MethodInfo newClampMethod = typeof(CursorZoomBehaviour).GetMethod("ClampCameraPosition", BindingFlags.NonPublic | BindingFlags.Static);
        private RedirectCallsState redirectState;

		private void Start()
		{
            cameraController = GameObject.FindObjectOfType<CameraController>();

            redirectState = RedirectionHelper.RedirectCalls(oldClampMethod, newClampMethod);

            // originally 5000, this value causes tilt to change as you zoom in and out.
            // we need to disable this behaviour for fixed-cursor-on-zoom to make sense.
            cameraController.m_maxTiltDistance = 1000000f;
		}

        private void OnDestroy()
        {
            RedirectionHelper.RevertRedirect(oldClampMethod, redirectState);
        }

        private static float frameInitialCurrentSize;

        private static bool readyToApplyFix;

        private void Update()
        {
            readyToApplyFix = false;
            frameInitialCurrentSize = cameraController.m_currentSize;
        }

        private static Vector3 ClampCameraPosition(Vector3 position)
        {
            if (readyToApplyFix)
            {
                position = FixCurrentPosition(position);
                readyToApplyFix = false;
            }
            else if (position == cameraController.m_targetPosition)
            {
                // we are now at the end of the UpdateTargetPosition function
                // on the next call to this function we will be in UpdateTransform and then we can do our magic
                readyToApplyFix = true;
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

            if (frameInitialCurrentSize == 0f || cameraController.m_currentSize == 0f || frameInitialCurrentSize == cameraController.m_currentSize)
            {
                return position;
            }

            float mouseRayLength = Camera.main.farClipPlane;
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            ToolBase.RaycastOutput output;
            if (raycaster.CastRay(new ToolBase.RaycastInput(mouseRay, mouseRayLength), out output))
            {
                var sizeFraction = cameraController.m_currentSize / frameInitialCurrentSize;
                var fractionTowardsCursor = 1f - sizeFraction;
                var correction =  fractionTowardsCursor * (output.m_hitPos - cameraController.m_currentPosition);
                correction.y = 0f;
                position += correction;
                cameraController.m_targetPosition += correction;
                cameraController.m_currentPosition += correction;
            }
            return position;
        }
	}
    class RayCaster : ToolBase
    {
        public bool CastRay(ToolBase.RaycastInput input, out ToolBase.RaycastOutput output)
        {
            return RayCast(input, out output);
        }
    }
}
