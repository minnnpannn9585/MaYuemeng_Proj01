using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool wrapToFirstScene = true;

    private bool isLoading;

    private void Awake()
    {
        Collider2D portalCollider = GetComponent<Collider2D>();
        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryLoadNextScene(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLoadNextScene(collision.collider);
    }

    private void TryLoadNextScene(Collider2D other)
    {
        if (isLoading || other == null || !other.CompareTag(playerTag))
        {
            return;
        }

        int targetIndex = ResolveNextSceneIndex();
        if (targetIndex < 0)
        {
            Debug.LogWarning("Portal: no next scene found in Build Settings.", this);
            return;
        }

        isLoading = true;
        StartCoroutine(EnterPortal(targetIndex, other.transform.position));
    }

    private IEnumerator EnterPortal(int targetIndex, Vector3 position)
    {
        InteractionFeedback.Play(InteractionCue.Portal, position);
        yield return new WaitForSeconds(0.18f);
        SceneManager.LoadScene(targetIndex);
    }

    private int ResolveNextSceneIndex()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            int namedIndex = SceneUtility.GetBuildIndexByScenePath(GetScenePath(nextSceneName));
            if (namedIndex >= 0)
            {
                return namedIndex;
            }

            Debug.LogWarning($"Portal: scene '{nextSceneName}' is not in Build Settings.", this);
            return -1;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            return nextIndex;
        }

        return wrapToFirstScene ? 0 : -1;
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
