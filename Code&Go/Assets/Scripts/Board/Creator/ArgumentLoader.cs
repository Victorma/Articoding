﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArgumentLoader : MonoBehaviour
{
    [SerializeField] private ArgInput[] inputs;
    [SerializeField] private Text text;

    private BoardObject currentObject;

    public void SetBoardObject(BoardObject newObject)
    {
        if (newObject == null) return;

        currentObject = newObject;
        gameObject.SetActive(true);
        text.text = currentObject.GetNameWithIndex();

        string[] argsNames = currentObject.GetArgsNames();
        if (argsNames.Length == 0) gameObject.SetActive(false);
        for (int i = 0; i < inputs.Length; i++)
        {
            if (i < argsNames.Length)
                inputs[i].FillArg(argsNames[i]);
            else
                inputs[i].gameObject.SetActive(false);
        }
    }

    public void LoadArgs()
    {
        if (currentObject == null) return;

        string[] args = new string[inputs.Length];
        for (int i = 0; i < args.Length; i++)
        {
            if (inputs[i].gameObject.activeSelf)
                args[i] = inputs[i].GetInput();
        }

        currentObject.LoadArgs(args);
    }

    private void Update()
    {
        if (currentObject == null && gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
