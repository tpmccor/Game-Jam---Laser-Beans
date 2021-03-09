using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public Animator m_fadeScreen;
    public GameObject m_pauseMenu;

    public static bool IsPaused { get; private set; }

    private float m_currentTimescale;

    private SaveLoadLightMaps m_lightmapHandler;

    private void Awake()
    {
        //Need this initialized before Start() because PlayerManager sets initial lightmap in Start()
        m_lightmapHandler = GetComponent<SaveLoadLightMaps>();
    }

    private void Start()
    {
        IsPaused = false;
        m_pauseMenu.SetActive(false);
        m_currentTimescale = 1f;
    }

    private void Update()
    {
        //Toggle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void SetLightmaps(bool isPlayer1)
    {
        m_lightmapHandler.LoadLightmaps((isPlayer1 ? "player1" : "player2"));
    }

    public void LoadNextScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void RestartScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadScene(int sceneIndex)
    {
        m_fadeScreen.SetTrigger("FadeOut");
        StartCoroutine(WaitForFade(sceneIndex));
    }

    private IEnumerator WaitForFade(int sceneIndex)
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(sceneIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }

    public void PauseGame()
    {
        m_currentTimescale = Time.timeScale;
        Time.timeScale = 0f;
        IsPaused = true;
        m_pauseMenu.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = m_currentTimescale;
        IsPaused = false;
        m_pauseMenu.SetActive(false);
    }

    public static void SlowMotion(bool isSlow)
    {
        if (!IsPaused)
        {
            if (isSlow)
            {
                Time.timeScale = 0.1f;
            }
            else
            {
                Time.timeScale = 1f;
            }
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
        }
    }
}
