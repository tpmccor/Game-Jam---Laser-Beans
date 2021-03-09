
using System.Collections.Generic;
using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [SerializeField] protected bool m_movePlatform;
    [SerializeField] protected float m_animationTime;

    public Transform m_startTransform;
    public Transform m_endTransform;
    public Transform m_platformTransform;

    private bool m_reverse;

    private float m_animationLerp;

    private Vector3 m_startPos;
    private Vector3 m_endPos;

    private List<CharacterController> m_playersOnPlatform;

    void Start()
    {
        m_reverse = false;

        m_startPos = m_startTransform.position;
        m_endPos = m_endTransform.position;

        m_playersOnPlatform = new List<CharacterController>();
    }

    void Update()
    {
        if (m_movePlatform)
        {
            AnimatePlatform();
            CarryPlayers();
        }
    }

    private void AnimatePlatform()
    {
        //Lerp factor, add for fowards, subtract for reverse animation
        m_animationLerp += (m_reverse ? -1f : 1f) * Time.deltaTime / m_animationTime;

        //Lerp between start and end positions
        m_platformTransform.position = Vector3.Lerp(m_startPos, m_endPos, m_animationLerp);

        //Change direction when platform reaches endpoints
        if (Mathf.Approximately(Vector3.Distance(m_platformTransform.position, m_endPos), 0f))
        {
            m_reverse = true;
        }

        if (Mathf.Approximately(Vector3.Distance(m_platformTransform.position, m_startPos), 0f))
        {
            m_reverse = false;
        }
    }
    
    private void CarryPlayers()
    {
        //Move any players standing on the platform with the platform
        Vector3 platformVelocity = (m_reverse ? -1f : 1f) * (m_endPos - m_startPos) / m_animationTime;

        foreach (CharacterController player in m_playersOnPlatform)
        {
            player.Move(platformVelocity * Time.deltaTime);
        }
    }

    public void AddToPlatform(CharacterController player)
    {
        //Keep a reference too every player on the platform
        if (!m_playersOnPlatform.Contains(player))
        {
            m_playersOnPlatform.Add(player);
        }
    }

    public void RemoveFromPlatform(CharacterController player)
    {
        //Remove player from onPlatform list when they jump off
        if (m_playersOnPlatform.Contains(player))
        {
            m_playersOnPlatform.Remove(player);
        }
    }
}
