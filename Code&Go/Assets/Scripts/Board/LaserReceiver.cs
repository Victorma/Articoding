﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserReceiver : BoardObject, ILaserReceiver
{
    [SerializeField] private Renderer receiverRenderer;

    private bool registered = false;

    private void Start()
    {
        boardManager.RegisterReceiver();
    }

    public void OnLaserReceived()
    {
        receiverRenderer.material.color = Color.green;
        Invoke("ReceiverActive", 2.0f);
    }

    public void OnLaserReceiving()
    {
        // DO STUFF
    }

    public void OnLaserLost()
    {
        receiverRenderer.material.color = Color.yellow;
        if (registered)
        {
            boardManager.ReceiverDeactivated();
            registered = false;
        }
        else
            CancelInvoke("ReceiverActive");
    }

    private void ReceiverActive()
    {
        registered = true;
        boardManager.ReceiverActivated();
    }
}
