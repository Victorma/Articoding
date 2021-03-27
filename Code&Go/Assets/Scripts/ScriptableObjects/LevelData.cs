﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Level", menuName = "ScriptableObjects/Level")]
public class LevelData : ScriptableObject
{
    public string levelName;
    [TextArea(3,6)]
    public string description;

    public TextAsset statement; // Enunciado en .xml
    public TextAsset initialState; // Estado inicial en .xml

    public BlockData[] availableBlocks; // o cambiar por el enumerator

    [Header("Active Blocks")]
    public TextAsset activeBlocks;//Bloques y categorias disponibles    
    public bool allActive = false;
    
    [Space(10)]
    public Level levelPrefab;
}
