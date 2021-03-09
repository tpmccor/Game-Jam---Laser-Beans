
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] protected float m_cameraDistance;
    [SerializeField] protected float m_cameraHeight;
    [SerializeField] protected float m_playerTransitionTime;

    public PlayerController m_player1;
    public PlayerController m_player2;

    public Image m_screenFlash;

    private bool m_triggerSwitch;
    private bool m_isChangingPlayer;
    private bool m_activePlayer1;

    private float m_cameraLerp;

    private Vector3 m_currentCamPos;
    private Vector3 m_targetCamPos;

    private Camera m_mainCamera;
    
    private GameManager m_gameManager;


    void Start()
    {
        m_activePlayer1 = true;
        m_player1.SetActive(true);
        m_player2.SetActive(false);

        //Set layers
        m_player1.SetLayers("Level", "Player1Level", "Player2");
        m_player2.SetLayers("Level", "Player2Level", "Player1");
        m_player1.IgnoreLayers("Player1", "Player2Level", "Player2Triggers");
        m_player2.IgnoreLayers("Player2", "Player1Level", "Player1Triggers");

        m_mainCamera = Camera.main;
        m_mainCamera.cullingMask = LayerMask.GetMask("Level", "Player1Level", "Player1", "Player2", "Portal");

        m_screenFlash.color = Color.clear;

        m_gameManager = FindObjectOfType<GameManager>();
        m_gameManager.SetLightmaps(m_activePlayer1);
    }

    private void Update()
    {
        if (!m_isChangingPlayer)
        {
            FollowPlayer();

            if (Input.GetButtonDown("Switch Players"))
            {
                SwitchPlayer();
            }
        }

        //End condition
        if(m_player1.AtPortal() && m_player2.AtPortal())
        {
            m_player1.SetActive(false);
            m_player2.SetActive(false);
            m_gameManager.LoadNextScene();
        }
    }

    private void LateUpdate()
    {
        //LateUpdate to overwrite the zero alpha set by the animator component
        if (m_isChangingPlayer)
        {
            Transition();
        }
    }

    private void SwitchPlayer()
    {
        m_activePlayer1 = !m_activePlayer1;
        m_player1.SetActive(m_activePlayer1);
        m_player2.SetActive(!m_activePlayer1);

        InitTransition();
    }

    private void FollowPlayer()
    {
        GetPlayerFocus(out Vector3 player1Focus, out Vector3 player2Focus);

        //Keep camera focused on active player
        if (m_activePlayer1)
        {
            m_mainCamera.transform.position = player1Focus;
        }
        else
        {
            m_mainCamera.transform.position = player2Focus;
        }
    }

    private void GetPlayerFocus(out Vector3 player1Focus, out Vector3 player2Focus)
    {
        Vector3 player1pos = m_player1.transform.position;
        Vector3 player2pos = m_player2.transform.position;

        //Camera aims between the two players with a bias towards the active player
        player1Focus = player1pos - (player1pos - player2pos) / 5f;
        player1Focus.x = m_cameraDistance;
        player1Focus.y += m_cameraHeight;

        player2Focus = player2pos - (player2pos - player1pos) / 5f;
        player2Focus.x = m_cameraDistance;
        player2Focus.y += m_cameraHeight;
    }

    private void InitTransition()
    {
        //Initialize target and current camera position for player transition
        GetPlayerFocus(out Vector3 player1Focus, out Vector3 player2Focus);

        if (m_activePlayer1)
        {
            m_targetCamPos = player1Focus;
            m_currentCamPos = player2Focus;
        }
        else
        {
            m_targetCamPos = player2Focus;
            m_currentCamPos = player1Focus;
        }

        m_isChangingPlayer = true;
        m_triggerSwitch = true;
    }

    private void Transition()
    {
        if (m_mainCamera.transform.position == m_targetCamPos)
        {
            m_cameraLerp = 0f;
            m_isChangingPlayer = false;
            m_screenFlash.color = Color.clear;
            GameManager.SlowMotion(false);
        }
        else
        {
            //Lerp camera to target position when changing characters
            if (!Mathf.Approximately(Time.timeScale, 0f)){ //Can't divide by zero
                m_cameraLerp += (Time.deltaTime / Time.timeScale) / m_playerTransitionTime;
            }
            m_mainCamera.transform.position = Vector3.Lerp(m_currentCamPos, m_targetCamPos, m_cameraLerp);

            //Flash the screen during the transition
            Color flashColor = Color.white;
            if(m_cameraLerp < 0.5f)
            {
                flashColor.a = 2f * m_cameraLerp;
            }
            else
            {
                flashColor.a = 2 - 2f * m_cameraLerp;
            }
            m_screenFlash.color = flashColor;

            //Switch to the active player's world when flash is most intense
            if (m_cameraLerp >= 0.5f && m_triggerSwitch)
            {
                if (m_activePlayer1)
                {
                    m_mainCamera.cullingMask = LayerMask.GetMask("Level", "Player1Level", "Player1", "Player2", "Portal");
                }
                else
                {
                    m_mainCamera.cullingMask = LayerMask.GetMask("Level", "Player2Level", "Player1", "Player2", "Portal");
                }
                m_gameManager.SetLightmaps(m_activePlayer1);
                m_triggerSwitch = false;
            }

            GameManager.SlowMotion(true);
        }
    }
}
