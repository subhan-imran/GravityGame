using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GravityGame.Editor
{
    // A batch Play mode run gives uGUI real rendered depths for raycast and serialization checks.
    [InitializeOnLoad]
    public static class MobilePlayModeChecks
    {
        private const string ActiveKey = "GravityGame.MobileChecks.Active";
        private static int firstFrame;
        private static bool renderPhase;

        static MobilePlayModeChecks() => EditorApplication.playModeStateChanged += OnState;

        public static void Run()
        {
            try
            {
                TwoPlanetDemo.ValidateAll();
                MobileDemo.ConfigureLandscape();
                MobileDemo.UpgradeStarScene();
                SessionState.SetBool(ActiveKey, true);
                EditorApplication.EnterPlaymode();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                Finish(1);
            }
        }

        private static void OnState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ActiveKey, false)) return;
            firstFrame = Time.frameCount;
            renderPhase = false;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (Time.frameCount < firstFrame + 6) return;
            try
            {
                if (!renderPhase)
                {
                    MobileDemo.BeginInteractionRender();
                    try { MobileDemo.ValidateCurrentScene(); }
                    finally { MobileDemo.EndInteractionRender(); }
                    EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/MobilePrototype.unity", new LoadSceneParameters(LoadSceneMode.Single));
                    renderPhase = true;
                    firstFrame = Time.frameCount;
                    return;
                }
                MobileDemo.RenderCurrentPreviews();
                Debug.Log("MOBILE_PLAY_MODE_CHECKS_PASSED: rendered touch priority, star collection/restart, serialized scene reload, gameplay/pause/loss/victory previews.");
                Finish(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                Finish(1);
            }
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.Exit(code);
        }
    }
}
