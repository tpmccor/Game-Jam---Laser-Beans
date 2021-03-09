using UnityEngine;
using System;
using System.Collections;

public class UniqueId : MonoBehaviour
{
    public string m_uniqueID;

    public void GenerateID()
    {
        //Sometimes Guid.NewGuid() returns null
        while (m_uniqueID == null)
        {
            m_uniqueID = Guid.NewGuid().ToString();
        }
    }

    public string GetID()
    {
        return m_uniqueID;
    }
}