using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System.Reflection;
using System;

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
		}

        private static int clampCameraCallsInFrame = 0;
        private static Vector3 frameInitialTargetPosition;
        private static float frameInitialTargetSize;

        private Vector3 frameInitialActualPosition;
        private Ray mouseRay;

        private void Update()
        {
            if (cameraController != null)
            {
                frameInitialTargetPosition = cameraController.m_targetPosition;
                frameInitialTargetSize = cameraController.m_targetSize;
                frameInitialActualPosition = cameraController.transform.position;
                clampCameraCallsInFrame = 0;
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            }
        }

        private void LateUpdate()
        {
            if (cameraController != null)
            {
                if (frameInitialTargetSize != cameraController.m_targetSize)
                {
                        // Debug.Log("final target pos " + cameraController.m_targetPosition);
                    var displacement = cameraController.transform.position - frameInitialActualPosition;



            // float deltaY = targetPosition.y - frameInitialTargetPosition.y;
            // Debug.Log("Delta y = " + deltaY);
            // float distance = deltaY / mouseRay.direction.y;
            // Debug.Log("Mouse ray origin " + mouseRay.origin + ", direction" + mouseRay.direction);
            // Debug.Log("wrong target " + targetPosition);
            // targetPosition.x += distance * mouseRay.direction.x;
            // targetPosition.z += distance * mouseRay.direction.z;
            // Debug.Log("fixed target " + targetPosition);


                }
            }
        }

        private static Vector3 ClampCameraPosition(Vector3 position)
        {
            if (cameraController != null && position == cameraController.m_targetPosition)
            {
                // this is the final call just before we update currentPosition, so time to correct the x and z coordinates
                // target size changed indicates that we've zoomed
                if (frameInitialTargetSize != cameraController.m_targetSize)
                {
                    position = TargetPositionWithFixedZoom(frameInitialTargetPosition, position);
                }
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

        private static Vector3 TargetPositionWithFixedZoom(Vector3 frameInitialTargetPosition, Vector3 targetPosition)
        {
            // Debug.Log("Fixing zoom");
            // Debug.Log("target before zoom calc " + frameInitialTargetPosition);
            // Debug.Log("actual camera position " + cameraController.transform.position);
            // Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            // float deltaY = targetPosition.y - frameInitialTargetPosition.y;
            // Debug.Log("Delta y = " + deltaY);
            // float distance = deltaY / mouseRay.direction.y;
            // Debug.Log("Mouse ray origin " + mouseRay.origin + ", direction" + mouseRay.direction);
            // Debug.Log("wrong target " + targetPosition);
            // targetPosition.x += distance * mouseRay.direction.x;
            // targetPosition.z += distance * mouseRay.direction.z;
            // Debug.Log("fixed target " + targetPosition);


            // // totally new approach

            // var shiftCentreX = (0.5f + Input.mousePosition.x / 2560) * (frameInitialTargetSize - cameraController.m_targetSize);
            // var shiftCentreY = (0.5f + Input.mousePosition.y / 1440) * (frameInitialTargetSize - cameraController.m_targetSize) * 0.7f;
            // // var mouseRay = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x + shiftCentreX, Input.mousePosition.y + shiftCentreY, 0));

            // targetPosition.x += shiftCentreX;
            // targetPosition.z += shiftCentreY;

            if (frameInitialTargetSize == 0f || cameraController.m_targetSize == 0f)
            {
                return targetPosition;
            }

            float m_mouseRayLength = Camera.main.farClipPlane;
            Ray m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            ToolBase.RaycastInput input = new ToolBase.RaycastInput(m_mouseRay, m_mouseRayLength);
            ToolBase.RaycastOutput output;
            if (raycast.RayCasted(input, out output))
            {
                var hitPosition = output.m_hitPos;

                Vector3 current = cameraController.m_currentPosition;

                Debug.Log("target size changed from " + frameInitialTargetSize + " to " + cameraController.m_targetSize);

                var zoomingIn = cameraController.m_targetSize < frameInitialTargetSize;

                var sizeFraction = zoomingIn ? frameInitialTargetSize / cameraController.m_targetSize : cameraController.m_targetSize / frameInitialTargetSize;

                var fractionTowardsCursor = (zoomingIn ? 1f : -1.25f) * (1f - 1f / sizeFraction);
                var deltaX =  fractionTowardsCursor * (hitPosition.x - current.x);
                var deltaZ = fractionTowardsCursor * (hitPosition.z - current.z);

                Debug.Log("fraction towards cursor = " + fractionTowardsCursor);
                Debug.Log("original target pos = " + targetPosition);

                targetPosition.x += deltaX;
                targetPosition.z += deltaZ;

                Debug.Log("new target pos = " + targetPosition);

                var angle = Vector3.Angle(m_mouseRay.direction, Vector3.down);
                Debug.Log("angle of mouse against vertical " + angle);
                // problems to correct:
                // when very zoomed in, still vertically above,
            }

            return targetPosition;
        }

        // void Update()
        // {
        //     if (cameraController.m_freeCamera)
        //     {
        //         // TODO
        //     }
        //     else if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        //     {
        //         float m_mouseRayLength = Camera.main.farClipPlane;
        //         Ray m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        //         var lowest_y = 0; // TODO

        //         Vector3 hitPos5 = new Vector3(0, 0, 0);

        //         ToolBase.RaycastInput input = new ToolBase.RaycastInput(m_mouseRay, m_mouseRayLength);
        //         ToolBase.RaycastOutput output;
        //         if (raycast.RayCasted(input, out output))
        //         {
        //             hitPos5 = output.m_hitPos;
        //             var hitPos4 = hitPos5;

        //             Vector3 newTargetPos;

        //             var a = cameraController.m_currentPosition.x;
        //             var b = cameraController.m_currentPosition.y;
        //             var c = cameraController.m_currentPosition.z;

        //             var d = hitPos4.x;
        //             var e = hitPos4.y;
        //             var f = hitPos4.z;

        //             float factor = 1.5f;

        //             if (zoomVel > 0)
        //             {
        //                 factor = 0.5f;
        //             }

        //             var g = b - factor * (b - cameraController.m_targetPosition.y) * (1 + lowest_y / b);
        //             if (g < e)
        //             {
        //                 g = e;

        //             }

        //             newTargetPos.x = (float)((-a * e + a * g + b * d - d * g) / (b - e));
        //             newTargetPos.z = (float)((b * f - c * e + c * g - f * g) / (b - e));

        //             if (zoomVel > 0)
        //             {
        //                 if (b - lowest_y > 100)
        //                 {
        //                     newTargetPos.x = 2 * a - newTargetPos.x;
        //                     newTargetPos.z = 2 * c - newTargetPos.z;
        //                 }
        //                 else {
        //                     newTargetPos = cameraController.m_targetPosition;
        //                 }
        //             }

        //             cameraController.m_targetPosition.x = newTargetPos.x;
        //             cameraController.m_targetPosition.z = newTargetPos.z;
        //         }
        //     }
        // }

	}
}
