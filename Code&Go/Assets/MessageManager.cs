﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MSG_TYPE { INSTANTIATE, MOVE};

public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance = null;

    private List<Listener> listeners;

    private void Awake()
    {
        if (Instance != null) return;

        Instance = this;
        DontDestroyOnLoad(gameObject);
        listeners = new List<Listener>();
    }

    public void SubscribeListener(Listener listener)
    {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }

    public void UnsubscribeListener(Listener listener)
    {
        listeners.Remove(listener);
    }

    public void SendMessage(string msg, MSG_TYPE type)
    {
        foreach (Listener l in listeners)
            l.ReceiveMessage(msg, type);
    }
}
