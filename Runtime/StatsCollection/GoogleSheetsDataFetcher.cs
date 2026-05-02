using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace LGSToolbox
{
    public static class GoogleSheetsDataFetcher
    {
        public static IEnumerator FetchText(string url, Action<string> onDone)
        {
            yield return FetchText(url, onDone, null);
        }

        public static IEnumerator FetchText(string url, Action<string> onDone, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                onError?.Invoke("URL is empty.");
                onDone?.Invoke(null);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string error = request.error;

#if UNITY_EDITOR
                    Debug.LogWarning($"[LGS GoogleSheetsDataFetcher] Fetch failed: {error}");
#endif

                    onError?.Invoke(error);
                    onDone?.Invoke(null);
                    yield break;
                }

                onDone?.Invoke(request.downloadHandler.text);
            }
        }
    }
}