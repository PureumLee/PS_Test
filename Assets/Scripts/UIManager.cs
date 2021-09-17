using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.UI;


public class UIManager : MonoBehaviour
{
    public Transform[] spotList;
    public Transform playerTr;

    void Start()
    {

    }

    public void Teleport(int idx)
    {
        Debug.Log(spotList[idx].position);
        playerTr.position = spotList[idx].position;
    }

}
