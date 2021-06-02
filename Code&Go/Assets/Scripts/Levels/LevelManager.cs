﻿using AssetPackage;
using System.Collections;
using System.Collections.Generic;
using UBlockly.UGUI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Components;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public struct CategoryTutorialsData
    {
        public PopUpData data;
        public string categoryName;
    }

    private Category currentCategory;
    [SerializeField] private LevelData currentLevel;
    private int currentLevelIndex = 0;

    [SerializeField] private bool buildLimits = true;

    [Space]
    [SerializeField] private StatementManager statementManager;

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraFit cameraFit;

    [SerializeField] private Category defaultCategory;
    [SerializeField] private int defaultLevelIndex;

    [SerializeField] private Text levelName;
    [SerializeField] private LocalizeStringEvent levelNameLocalized;

    [SerializeField] private GameObject saveButton;

    [SerializeField] private CategoryTutorialsData[] categoriesTutorials;

    [SerializeField] private int pasosOffset = 0;

    public GameObject endPanel;
    public GameObject blackRect;

    public GameObject endPanelMinimized;
    public GameObject debugPanel;

    public GameObject gameOverPanel;
    public GameObject gameOverMinimized;

    public StarsController starsController;

    private int minimosPasos = 0;

    public StreamRoom streamRoom;

    private void Awake()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            currentCategory = gameManager.GetCurrentCategory();
            currentLevelIndex = gameManager.GetCurrentLevelIndex();
            currentLevel = currentCategory.levels[currentLevelIndex];
            minimosPasos = currentLevel.minimosPasos;
        }
        else
        {
            currentCategory = defaultCategory;
            currentLevelIndex = defaultLevelIndex;
            currentLevel = currentCategory.levels[currentLevelIndex];
            minimosPasos = currentLevel.minimosPasos;
        }

        endPanel.SetActive(false);
        //blackRect.SetActive(false);

#if UNITY_EDITOR
        saveButton.SetActive(true);
#endif
    }

    private void Start()
    {
        Initialize();

        var dom = UBlockly.Xml.WorkspaceToDom(BlocklyUI.WorkspaceView.Workspace);
        string text = UBlockly.Xml.DomToText(dom);
        text = GameManager.Instance.ChangeCodeIDs(text);
        
        TrackerAsset.Instance.setVar("code", "\r\n" + text);
        TrackerAsset.Instance.Completable.Initialized(GameManager.Instance.GetCurrentLevelName().ToLower(), CompletableTracker.Completable.Level);


        //levelName.text = currentLevel.levelName;
        levelNameLocalized.StringReference = currentLevel.levelNameLocalized;
        levelNameLocalized.RefreshString();
    }

    private void Update()
    {
        if (boardManager == null)
            return;

        if (boardManager.GetCurrentSteps() > minimosPasos + pasosOffset)
            starsController.DeactivateMinimumStepsStar(boardManager.GetCurrentSteps() - (minimosPasos + pasosOffset));


        if (boardManager.BoardCompleted() && !endPanel.activeSelf && !endPanelMinimized.activeSelf)
        {
            string levelName = GameManager.Instance.GetCurrentLevelName();


            streamRoom.FinishLevel();

            endPanel.SetActive(true);
            blackRect.SetActive(true);
            if (!GameManager.Instance.InCreatedLevel())
            {
                TrackerAsset.Instance.setVar("steps", boardManager.GetCurrentSteps());
                TrackerAsset.Instance.setVar("first_execution", starsController.IsFirstRunStarActive());
                TrackerAsset.Instance.setVar("minimum_steps", starsController.IsMinimumStepsStarActive());
                TrackerAsset.Instance.setVar("no_hints", starsController.IsNoHintsStarActive());
                ProgressManager.Instance.LevelCompleted(starsController.GetStars());
            }
            else
            {
                TrackerAsset.Instance.setVar("level", levelName);
                TrackerAsset.Instance.Accessible.Accessed("created_level_end");
            }
        }

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.N))
            LoadNextLevel();
#endif
    }

    private void Initialize()
    {
        if (currentLevel == null)
        {
            Debug.LogError("Cannot initialize Level. CurrentLevel is null");
            return;
        }

        // Maybe do more stuff
        ActivateLevelBlocks(currentLevel.activeBlocks, currentLevel.allActive);
        LoadInitialBlocks(currentLevel.initialState);

        string boardJson = currentLevel.levelBoard != null ? currentLevel.levelBoard.text : currentLevel.auxLevelBoard;
        BoardState state = BoardState.FromJson(boardJson);
        boardManager.LoadBoard(state, buildLimits);
        cameraFit.FitBoard(boardManager.GetRows(), boardManager.GetColumns());

        BlocklyUI.WorkspaceView.InitIDs();
    }

    public void LoadLevel(Category category, int levelIndex)
    {
        currentCategory = category;
        currentLevelIndex = levelIndex;
        LoadLevel(category.levels[levelIndex]);
    }
    private void LoadLevel(LevelData level)
    {
        currentLevel = level;
        Initialize();
    }

    // It is called when the current level is completed
    public void LoadNextLevel()
    {
        int levelSize = currentCategory.levels.Count;
        if (++currentLevelIndex < levelSize)
            GameManager.Instance.LoadLevel(currentCategory, currentLevelIndex);
        else
            LoadMainMenu(); // Por ejemplo
    }

    public void RetryLevel()
    {
        ResetLevel();
        gameOverPanel.SetActive(false);
        blackRect.SetActive(false);
        gameOverMinimized.SetActive(false);

        streamRoom.Retry();

        starsController.DeactivateFirstRunStar();

        TrackerAsset.Instance.GameObject.Interacted("retry_button");

        var levelName = GameManager.Instance.GetCurrentLevelName();
        TrackerAsset.Instance.Completable.Initialized(levelName, CompletableTracker.Completable.Level);
    }

    public void MinimizeEndPanel()
    {
        endPanelMinimized.SetActive(true);
        gameOverPanel.SetActive(false);
        endPanel.SetActive(false);
        blackRect.SetActive(false);
        debugPanel.SetActive(false);
        TrackerAsset.Instance.GameObject.Interacted("end_panel_minimized_button");
    }

    public void MinimizeGameOverPanel()
    {
        gameOverMinimized.SetActive(true);
        gameOverPanel.SetActive(false);
        //endPanel.SetActive(false);
        blackRect.SetActive(false);
        debugPanel.SetActive(false);
        TrackerAsset.Instance.GameObject.Interacted("game_over_panel_minimized_button");
    }

    public void ResetLevel()
    {
        boardManager.Reset();
        string boardJson = currentLevel.levelBoard != null ? currentLevel.levelBoard.text : currentLevel.auxLevelBoard;
        BoardState state = BoardState.FromJson(boardJson);
        boardManager.GenerateBoardElements(state);
        debugPanel.SetActive(true);
        cameraFit.FitBoard(boardManager.GetRows(), boardManager.GetColumns());
    }

    public void ReloadLevel()
    {
        LoadLevel(currentLevel);
    }

    public void LoadMainMenu()
    {
        string levelName = GameManager.Instance.GetCurrentLevelName().ToLower();
        TrackerAsset.Instance.setVar("steps", boardManager.GetCurrentSteps());
        TrackerAsset.Instance.setVar("level", levelName);
        TrackerAsset.Instance.GameObject.Interacted("level_exit_button");


        var dom = UBlockly.Xml.WorkspaceToDom(BlocklyUI.WorkspaceView.Workspace);
        string text = UBlockly.Xml.DomToText(dom);
        text = GameManager.Instance.ChangeCodeIDs(text);

        TrackerAsset.Instance.setVar("code", "\r\n" + text);
        TrackerAsset.Instance.Completable.Completed(levelName, CompletableTracker.Completable.Level, false, -1f);

        if(LoadManager.Instance == null)
        {
            SceneManager.LoadScene("MenuScene");
            return;
        }

        LoadManager.Instance.LoadScene("MenuScene");
    }

    public void LoadInitialBlocks(LocalizedAsset<TextAsset> textAsset)
    {
        if (textAsset == null) return;

        StartCoroutine(AsyncLoadInitialBlocks(textAsset));
    }

    IEnumerator AsyncLoadInitialBlocks(LocalizedAsset<TextAsset> textAsset)
    {
        var loadingOp = textAsset.LoadAssetAsync();
        if (!loadingOp.IsDone)
            yield return loadingOp;

        TextAsset localizedTextAsset = loadingOp.Result;
        if (localizedTextAsset == null) yield break;

        BlocklyUI.WorkspaceView.CleanViews();

        var dom = UBlockly.Xml.TextToDom(localizedTextAsset.text);

        UBlockly.Xml.DomToWorkspace(dom, BlocklyUI.WorkspaceView.Workspace);
        BlocklyUI.WorkspaceView.BuildViews();

        yield return null;
    }

    public void ActivateLevelBlocks(TextAsset textAsset, bool allActive)
    {
        if (textAsset == null) return;

        StartCoroutine(AsyncActivateLevelBlocks(textAsset, allActive));
    }

    IEnumerator AsyncActivateLevelBlocks(TextAsset textAsset, bool allActive)
    {
        if (allActive) BlocklyUI.WorkspaceView.Toolbox.SetActiveAllBlocks();
        else if (textAsset != null)
        {
            ActiveBlocks blocks = ActiveBlocks.FromJson(textAsset.text);
            BlocklyUI.WorkspaceView.Toolbox.SetActiveBlocks(blocks.AsMap(), CategoriesTutorialsAsMap());
        }

        yield return null;
    }

    public Dictionary<string, PopUpData> CategoriesTutorialsAsMap()
    {
        Dictionary<string, PopUpData> map = new Dictionary<string, PopUpData>();
        foreach (CategoryTutorialsData c in categoriesTutorials)//For each category       
            map[c.categoryName.ToUpper()] = c.data;
        return map;
    }
}
