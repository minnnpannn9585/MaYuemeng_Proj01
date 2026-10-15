using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private string firstLevelScene = "1";
    [SerializeField] private float inputDelay = 0.4f;

    private float readyAt;
    private bool isLoading;

    private void Start()
    {
        readyAt = Time.unscaledTime + inputDelay;
    }

    private void Update()
    {
        if (isLoading || Time.unscaledTime < readyAt || !PressedStart())
        {
            return;
        }

        if (SceneUtility.GetBuildIndexByScenePath(GetScenePath(firstLevelScene)) < 0)
        {
            Debug.LogWarning($"StartMenu: scene '{firstLevelScene}' is not in Build Settings.", this);
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(firstLevelScene);
    }

    private static bool PressedStart()
    {
        if (Input.anyKeyDown)
        {
            return true;
        }

        for (int button = 0; button < 20; button++)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton0 + button))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetScenePath(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                return path;
            }
        }

        return sceneName;
    }
}
