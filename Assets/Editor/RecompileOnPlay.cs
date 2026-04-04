using UnityEditor;

namespace _Code.Scripts._Utils.Editor
{
    [InitializeOnLoad]
    public class RecompileOnPlay
    {
        static RecompileOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingEditMode)
            {
                AssetDatabase.Refresh();
            }
        } 
    }
}