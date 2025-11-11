using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuMusic : MonoBehaviour
{
    public static MenuMusic Instance;

    private AudioSource _source;

    // Which scenes should have the menu music playing?
    [SerializeField] private string[] allowedScenes = { "MainMenu", "Credits" };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A copy already exists ? destroy this one
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldPlay = false;
        foreach (var s in allowedScenes)
        {
            if (scene.name == s)
            {
                shouldPlay = true;
                break;
            }
        }

        if (!_source) _source = GetComponent<AudioSource>();

        if (shouldPlay)
        {
            if (!_source.isPlaying)
                _source.Play(); // continues from wherever it was
        }
        else
        {
            if (_source.isPlaying)
                _source.Stop();
        }
    }
}
