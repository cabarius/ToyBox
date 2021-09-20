using Kingmaker;
using UnityEngine;

namespace ToyBox {

    public class Utils {
        public static Vector3 PointerPosition() {
            Vector3 result = new Vector3();

            Camera camera = Game.GetCamera();
            RaycastHit raycastHit = default(RaycastHit);
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out raycastHit, camera.farClipPlane, 21761)) {
                result = raycastHit.point;
            }
            return result;
        }
    }
}