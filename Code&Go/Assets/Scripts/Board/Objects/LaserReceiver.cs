﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserReceiver : BoardObject, ILaserReceiver
{
    [SerializeField] private Renderer receiverRenderer;

    private bool registered = false;

    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;

    [SerializeField] private ParticleSystem onParticles;
    [SerializeField] private Light onLight;

    private void Awake()
    {
        typeName = "Receiver";
    }

    private void Start()
    {
        boardManager.RegisterReceiver();
    }

    public void OnLaserReceived()
    {
        receiverRenderer.material = onMaterial;
        onParticles.Play();
        Invoke("ReceiverActive", 2.0f);
    }

    public void OnLaserReceiving()
    {
        // DO STUFF
    }

    public void OnLaserLost()
    {
        receiverRenderer.material = offMaterial;
        onParticles.Stop();
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
