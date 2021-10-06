﻿using UnityEngine;

namespace ModKit.Utility {
    public static class UnityExtensions {
        private static void SafeDestroyInternal(GameObject obj) {
            obj.transform.SetParent(null, false);
            obj.SetActive(false);
            Object.Destroy(obj);
        }

        public static void SafeDestroy(this GameObject obj) {
            if (obj) {
                SafeDestroyInternal(obj);
            }
        }

        public static void SafeDestroy(this Component obj) {
            if (obj) {
                SafeDestroyInternal(obj.gameObject);
            }
        }
    }
}
