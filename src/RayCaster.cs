

namespace CursorZoom
{
    public class RayCaster : ToolBase
    {
        public bool RayCasted(ToolBase.RaycastInput inRay, out ToolBase.RaycastOutput outRay)
        {
            bool a = RayCast(inRay, out outRay);

            return a;
        }
    }
}
