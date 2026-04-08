using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromLobby
{
    static PlayFromLobby()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorPrefs.SetString("LastScene", EditorSceneManager.GetActiveScene().path);
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/LobbyScene.unity")
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
            }
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            string lastScene = EditorPrefs.GetString("LastScene");
            if (!string.IsNullOrEmpty(lastScene))
                EditorSceneManager.OpenScene(lastScene);
        }
    }
}
