using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

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
        public enum SaveTextureFileFormat {
            PNG,
            JPG,
            EXR,
            TGA
        }
        static public void SaveTextureToFile(this Texture source,
                                         string filePath,
                                         int width,
                                         int height,
                                         SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG,
                                         int jpgQuality = 95,
                                         bool asynchronous = true,
                                         System.Action<bool> done = null) {
            // check that the input we're getting is something we can handle:
            if (!(source is Texture2D || source is RenderTexture)) {
                done?.Invoke(false);
                return;
            }

            // use the original texture size in case the input is negative:
            if (width < 0 || height < 0) {
                width = source.width;
                height = source.height;
            }

            // resize the original image:
            var resizeRT = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(source, resizeRT);

            // create a native array to receive data from the GPU:
            var narray = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // request the texture data back from the GPU:
            var request = AsyncGPUReadback.RequestIntoNativeArray(ref narray, resizeRT, 0, (AsyncGPUReadbackRequest request) => {
                // if the readback was successful, encode and write the results to disk
                if (!request.hasError) {
                    NativeArray<byte> encoded;

                    switch (fileFormat) {
                        case SaveTextureFileFormat.EXR:
                            encoded = ImageConversion.EncodeNativeArrayToEXR(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                        case SaveTextureFileFormat.JPG:
                            encoded = ImageConversion.EncodeNativeArrayToJPG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height, 0, jpgQuality);
                            break;
                        case SaveTextureFileFormat.TGA:
                            encoded = ImageConversion.EncodeNativeArrayToTGA(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                        default:
                            encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                    }

                    System.IO.File.WriteAllBytes(filePath, encoded.ToArray());
                    encoded.Dispose();
                }

                narray.Dispose();

                // notify the user that the operation is done, and its outcome.
                done?.Invoke(!request.hasError);
            });

            if (!asynchronous)
                request.WaitForCompletion();
        }

    }
}
