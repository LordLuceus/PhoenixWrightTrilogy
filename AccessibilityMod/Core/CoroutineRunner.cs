using System.Collections;
using UnityEngine;

namespace AccessibilityMod.Core
{
    public class CoroutineRunner : MonoBehaviour
    {
        public static CoroutineRunner Instance { get; private set; }

        private Coroutine _clipboardCoroutine;
        private const float ClipboardProcessInterval = 0.025f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                StartClipboardProcessor();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartClipboardProcessor()
        {
            if (_clipboardCoroutine == null)
            {
                _clipboardCoroutine = StartCoroutine(ProcessClipboardQueue());
            }
        }

        public void StopClipboardProcessor()
        {
            if (_clipboardCoroutine != null)
            {
                StopCoroutine(_clipboardCoroutine);
                _clipboardCoroutine = null;
            }
        }

        private IEnumerator ProcessClipboardQueue()
        {
            while (true)
            {
                string message = ClipboardManager.DequeueMessage();
                if (message != null)
                {
                    try
                    {
                        GUIUtility.systemCopyBuffer = message;
                    }
                    catch (System.Exception ex)
                    {
                        AccessibilityMod.Logger?.Error($"Failed to set clipboard: {ex.Message}");
                    }

                    yield return new WaitForSeconds(ClipboardProcessInterval);
                }
                else
                {
                    yield return null;
                }
            }
        }
    }
}
