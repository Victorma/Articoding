﻿/****************************************************************************

Copyright 2016 sophieml1989@gmail.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

****************************************************************************/

using AssetPackage;
using Simva;
using System;
using System.Collections.Generic;
using uAdventure.Simva;
using UnityEngine;
using UnityEngine.UI;

namespace UBlockly.UGUI
{
    public abstract class BaseToolbox : MonoBehaviour
    {
        /// <summary>
        /// the current displayed block category
        /// </summary>
        protected string mActiveCategory;

        /// <summary>
        /// root objects of block views for different category
        /// </summary>
        protected Dictionary<string, GameObject> mRootList = new Dictionary<string, GameObject>();

        /// <summary>
        /// different toggle item for different block category
        /// </summary>
        protected Dictionary<string, Toggle> mMenuList = new Dictionary<string, Toggle>();

        protected Workspace mWorkspace;
        protected ToolboxConfig mConfig;
        protected BlockView mPickedBlockView;

        protected bool allActive = false;
        protected Dictionary<string, CategoryBlocks> activeCategories;
        protected int nActiveCategories = 0;

        public void SetActiveBlocks(Dictionary<string, CategoryBlocks> activeCategories, Dictionary<string, PopUpData> categoriesTutorials = null)
        {
            this.activeCategories = activeCategories;
            nActiveCategories = activeCategories.Keys.Count;
            TutorialTrigger trigger = GetComponent<TutorialTrigger>();
            if (trigger != null) trigger.enabled = nActiveCategories > 0;

            Block.blocksAvailable = new Dictionary<string, int>();
            int priority = 400;
            //Activate the category if it's not in the active list
            foreach (var category in mConfig.BlockCategoryList)
            {
                bool active = activeCategories.ContainsKey(category.CategoryName.ToLower());
                mMenuList[category.CategoryName].gameObject.SetActive(active);
                if (categoriesTutorials != null && categoriesTutorials.ContainsKey(category.CategoryName))
                {
                    TutorialTrigger categoryTrigger = mMenuList[category.CategoryName].gameObject.AddComponent<TutorialTrigger>();
                    categoryTrigger.isSaveCheckpoint = true;
                    categoryTrigger.priority = priority++;
                    categoryTrigger.highlightObject = true;
                    categoryTrigger.info = categoriesTutorials[category.CategoryName];
                }

                //Set Block Count
                if (active)
                {
                    mMenuList[category.CategoryName].gameObject.SetActive(true);
                    List<string> blockTypes = mConfig.GetBlockCategory(category.CategoryName).BlockList;
                    foreach (string blockType in blockTypes)
                    {
                        CategoryBlocks info = activeCategories[category.CategoryName.ToLower()];
                        if (info.activate == (info.activeBlocks.ContainsKey(blockType)))
                        {
                            int value = (info.activeBlocks.ContainsKey(blockType) ? info.activeBlocks[blockType] : Int16.MaxValue);
                            Block.blocksAvailable[blockType] = value;
                        }
                    }
                }
            }
        }

        public void SetActiveAllBlocks()
        {
            allActive = true;
            nActiveCategories = mConfig.BlockCategoryList.Count;
            foreach (ToolboxBlockCategory category in mConfig.BlockCategoryList)
            {
                mMenuList[category.CategoryName].gameObject.SetActive(true);
                List<string> blockTypes = mConfig.GetBlockCategory(category.CategoryName).BlockList;
                foreach (string blockType in blockTypes)
                    Block.blocksAvailable[blockType] = Int16.MaxValue;
            }
        }

        protected void SetBlockCount(BlockView block)
        {
            //Deactivate the block if it's not in the active list
            bool active = false;
            string blockType = block.BlockType;
            if (activeCategories != null && mActiveCategory != null && activeCategories.ContainsKey(mActiveCategory.ToLower()))
            {
                CategoryBlocks info = activeCategories[mActiveCategory.ToLower()];
                active = info.activate == (info.activeBlocks.ContainsKey(blockType));
                int value = (Block.blocksAvailable.ContainsKey(blockType) ? Block.blocksAvailable[blockType] : Int16.MaxValue);
                if (value <= 0)
                {
                    block.enabled = false;
                    block.ChangeBgColor(Color.grey);
                }
            }
            block.gameObject.SetActive(allActive || active);
            block.UpdateCount();
        }

        protected abstract void Build();
        protected virtual void OnPickBlockView() { }

        public void Init(Workspace workspace, ToolboxConfig config)
        {
            mWorkspace = workspace;
            mConfig = config;

            Build();

            mWorkspace.VariableMap.AddObserver(new VariableObserver(this));
            mWorkspace.ProcedureDB.AddObserver(new ProcedureObserver(this));
        }

        public void Clean()
        {
            mActiveCategory = null;

            foreach (GameObject obj in mRootList.Values)
            {
                GameObject.Destroy(obj);
            }
            mRootList.Clear();

            foreach (Toggle toggle in mMenuList.Values)
            {
                GameObject.Destroy(toggle.gameObject);
            }
            mMenuList.Clear();
        }

        /// <summary>
        /// Create a new block view in toolbox 
        /// </summary>
        protected BlockView NewBlockView(string blockType, Transform parent, int index = -1)
        {
            Block block = mWorkspace.NewBlock(blockType);
            return NewBlockView(block, parent, index);
        }

        /// <summary>
        /// Create a new block view in toolbox 
        /// </summary>
        protected BlockView NewBlockView(Block block, Transform parent, int index = -1)
        {
            mWorkspace.RemoveTopBlock(block);

            BlockView view = BlockViewFactory.CreateView(block);
            view.InToolbox = true;
            view.ViewTransform.SetParent(parent, false);

            if (index >= 0)
                view.ViewTransform.SetSiblingIndex(index);

            //add mask
            GameObject maskObj = new GameObject("ToolboxMask");
            maskObj.transform.SetParent(view.ViewTransform, false);
            RectTransform maskTrans = maskObj.AddComponent<RectTransform>();
            maskTrans.sizeDelta = view.Size;
            Image maskImage = maskObj.AddComponent<Image>();
            maskImage.color = new Color(1, 1, 1, 0);
            UIEventListener.Get(maskObj).onBeginDrag = data => PickBlockView(view);
            if (!BlockViewSettings.Get().MaskedInToolbox)
                maskTrans.SetAsFirstSibling();

            view.ActivateCountText(view.InToolbox);
            view.UpdateCount();

            return view;
        }

        protected void PickBlockView(BlockView blockView)
        {
            if (!blockView.enabled) return;
            if (mPickedBlockView != null)
            {
                Debug.LogError("Toolbox-PickBlockView: Already picked a block view.");
                return;
            }

            // compute the local position of the block view in coding area
            Vector3 localPos = BlocklyUI.WorkspaceView.CodingArea.InverseTransformPoint(blockView.ViewTransform.position);

            // clone a new block view for coding area
            mPickedBlockView = BlocklyUI.WorkspaceView.CloneBlockView(blockView, new Vector2(localPos.x, localPos.y));
            if (mPickedBlockView.InToolbox) return;
            mPickedBlockView.OnBeginDrag(null);
            mPickedBlockView.ActivateCountText(false);

            //if the max number of blocks have been used disable the block
            if (Block.blocksAvailable.ContainsKey(blockView.BlockType) && Block.blocksAvailable[blockView.BlockType] > 0)
            {
                Block.blocksAvailable[blockView.BlockType]--;
                if (Block.blocksAvailable[blockView.BlockType] <= 0)
                {
                    blockView.ChangeBgColor(Color.grey);
                    blockView.enabled = false;
                }
            }
            blockView.UpdateCount();

            OnPickBlockView();

            TrackerAsset.Instance.setVar("block_type", mPickedBlockView.Block.Type);
            TrackerAsset.Instance.setVar("action", "create");
            TrackerAsset.Instance.setVar("level", GameManager.Instance.GetCurrentLevelName().ToLower());
            TrackerAsset.Instance.GameObject.Interacted(GameManager.Instance.GetBlockId(mPickedBlockView.Block));
            
        }

        protected void UpdatePickedBlockView()
        {
            if (mPickedBlockView == null) return;

            if (!UnityEngine.Input.anyKey)
            {
                mPickedBlockView.OnEndDrag(null);
                mPickedBlockView = null;
                return;
            }
            mPickedBlockView.OnDrag(null);
        }

        /// <summary>
        /// Get the category name for block view
        /// </summary>
        public string GetCategoryNameOfBlockView(BlockView view)
        {
            foreach (var category in mConfig.BlockCategoryList)
            {
                foreach (string type in category.BlockList)
                {
                    if (string.Equals(view.BlockType, type))
                    {
                        return category.CategoryName;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get the background color for block view
        /// </summary>
        public Color GetColorOfBlockView(BlockView view)
        {
            foreach (var category in mConfig.BlockCategoryList)
            {
                foreach (string type in category.BlockList)
                {
                    if (string.Equals(view.BlockType, type))
                    {
                        return category.Color;
                    }
                }
            }
            return Color.white;
        }

        #region Variables

        protected Dictionary<string, BlockView> mVariableGetterViews = new Dictionary<string, BlockView>();
        protected List<BlockView> mVariableHelperViews = new List<BlockView>();

        protected void BuildVariableBlocks()
        {
            Transform parent = mRootList[Define.VARIABLE_CATEGORY_NAME].transform;

            //build createVar button
            GameObject obj = GameObject.Instantiate(BlockViewSettings.Get().PrefabBtnCreateVar);
            obj.transform.SetParent(parent, false);
            obj.GetComponentInChildren<Image>().color = mConfig.GetBlockCategory(Define.VARIABLE_CATEGORY_NAME).Color;
            obj.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogFactory.CreateDialog("variable_name");
            });

            List<VariableModel> allVars = mWorkspace.GetAllVariables();
            if (allVars.Count == 0) return;

            CreateVariableHelperViews();

            //list all variable getter views
            foreach (VariableModel variable in mWorkspace.GetAllVariables())
            {
                CreateVariableGetterView(variable.Name);
            }
        }

        protected void CreateVariableGetterView(string varName)
        {
            if (mVariableGetterViews.ContainsKey(varName))
                return;

            GameObject parentObj;
            if (!mRootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out parentObj))
                return;

            Block block = mWorkspace.NewBlock(Define.VARIABLE_GET_BLOCK_TYPE);
            block.SetFieldValue("VAR", varName);
            BlockView view = NewBlockView(block, parentObj.transform);
            mVariableGetterViews[varName] = view;
            SetBlockCount(view);
        }

        protected void DeleteVariableGetterView(string varName)
        {
            BlockView view;
            mVariableGetterViews.TryGetValue(varName, out view);
            if (view != null)
            {
                mVariableGetterViews.Remove(varName);
                view.Dispose();
            }
        }

        protected void CreateVariableHelperViews()
        {
            GameObject parentObj;
            if (!mRootList.TryGetValue(Define.VARIABLE_CATEGORY_NAME, out parentObj))
                return;

            string varName = mWorkspace.GetAllVariables()[0].Name;
            List<string> blockTypes = mConfig.GetBlockCategory(Define.VARIABLE_CATEGORY_NAME).BlockList;
            foreach (string blockType in blockTypes)
            {
                if (!blockType.Equals(Define.VARIABLE_GET_BLOCK_TYPE))
                {
                    Block block = mWorkspace.NewBlock(blockType);
                    block.SetFieldValue("VAR", varName);
                    BlockView view = NewBlockView(block, parentObj.transform);
                    mVariableHelperViews.Add(view);
                    SetBlockCount(view);
                }
            }
        }

        protected void DeleteVariableHelperViews()
        {
            foreach (BlockView view in mVariableHelperViews)
            {
                view.Dispose();
            }
            mVariableHelperViews.Clear();
        }

        protected void OnVariableUpdate(VariableUpdateData updateData)
        {
            switch (updateData.Type)
            {
                case VariableUpdateData.Create:
                    {
                        if (mVariableHelperViews.Count == 0)
                            CreateVariableHelperViews();
                        CreateVariableGetterView(updateData.VarName);
                        break;
                    }
                case VariableUpdateData.Delete:
                    {
                        DeleteVariableGetterView(updateData.VarName);

                        //change variable helper view
                        List<VariableModel> allVars = mWorkspace.GetAllVariables();
                        if (allVars.Count == 0)
                        {
                            DeleteVariableHelperViews();
                        }
                        else
                        {
                            foreach (BlockView view in mVariableHelperViews)
                            {
                                if (view.Block.GetFieldValue("VAR").Equals(updateData.VarName))
                                {
                                    view.Block.SetFieldValue("VAR", allVars[0].Name);
                                }
                            }
                        }
                        break;
                    }
                case VariableUpdateData.Rename:
                    {
                        BlockView view;
                        mVariableGetterViews.TryGetValue(updateData.VarName, out view);
                        if (view != null)
                        {
                            mVariableGetterViews.Remove(updateData.VarName);
                            mVariableGetterViews[updateData.NewVarName] = view;
                        }
                        break;
                    }
            }
        }

        private class VariableObserver : IObserver<VariableUpdateData>
        {
            private BaseToolbox mToolbox;

            public VariableObserver(BaseToolbox toolbox)
            {
                mToolbox = toolbox;
            }

            public void OnUpdated(object subject, VariableUpdateData args)
            {
                if (mToolbox == null || mToolbox.transform == null)
                    ((Observable<VariableUpdateData>)subject).RemoveObserver(this);
                else mToolbox.OnVariableUpdate(args);
            }
        }
        #endregion

        #region Procedures

        protected Dictionary<string, BlockView> mProcedureCallerViews = new Dictionary<string, BlockView>();

        protected void BuildProcedureBlocks()
        {
            Transform parent = mRootList[Define.PROCEDURE_CATEGORY_NAME].transform;
            List<string> blockTypes = mConfig.GetBlockCategory(Define.PROCEDURE_CATEGORY_NAME).BlockList;
            foreach (string blockType in blockTypes)
            {
                if (!blockType.Equals(Define.CALL_NO_RETURN_BLOCK_TYPE) &&
                    !blockType.Equals(Define.CALL_WITH_RETURN_BLOCK_TYPE))
                {
                    NewBlockView(blockType, parent);
                }
            }

            // list all caller views
            foreach (Block block in mWorkspace.ProcedureDB.GetDefinitionBlocks())
            {
                CreateProcedureCallerView(((ProcedureDefinitionMutator)block.Mutator).ProcedureInfo, ProcedureDB.HasReturn(block));
            }
        }

        protected void CreateProcedureCallerView(Procedure procedureInfo, bool hasReturn)
        {
            if (mProcedureCallerViews.ContainsKey(procedureInfo.Name))
                return;

            GameObject parentObj;
            if (!mRootList.TryGetValue(Define.PROCEDURE_CATEGORY_NAME, out parentObj))
                return;

            string blockType = hasReturn ? Define.CALL_WITH_RETURN_BLOCK_TYPE : Define.CALL_NO_RETURN_BLOCK_TYPE;
            Block block = mWorkspace.NewBlock(blockType);
            block.SetFieldValue("NAME", procedureInfo.Name);
            BlockView view = NewBlockView(block, parentObj.transform);
            mProcedureCallerViews[procedureInfo.Name] = view;
        }

        protected void DeleteProcedureCallerView(Procedure procedureInfo)
        {
            BlockView view;
            mProcedureCallerViews.TryGetValue(procedureInfo.Name, out view);
            if (view != null)
            {
                mProcedureCallerViews.Remove(procedureInfo.Name);
                view.Dispose();
            }
        }

        protected void OnProcedureUpdate(ProcedureUpdateData updateData)
        {
            switch (updateData.Type)
            {
                case ProcedureUpdateData.Add:
                    {
                        CreateProcedureCallerView(updateData.ProcedureInfo, ProcedureDB.HasReturn(updateData.ProcedureDefinitionBlock));
                        break;
                    }
                case ProcedureUpdateData.Remove:
                    {
                        DeleteProcedureCallerView(updateData.ProcedureInfo);
                        break;
                    }
                case ProcedureUpdateData.Mutate:
                    {
                        //mutate the caller prototype view
                        BlockView view = mProcedureCallerViews[updateData.ProcedureInfo.Name];
                        if (!updateData.ProcedureInfo.Name.Equals(updateData.NewProcedureInfo.Name))
                        {
                            mProcedureCallerViews.Remove(updateData.ProcedureInfo.Name);
                            mProcedureCallerViews[updateData.NewProcedureInfo.Name] = view;
                        }

                    ((ProcedureMutator)view.Block.Mutator).Mutate(updateData.NewProcedureInfo);
                        break;
                    }
            }
        }

        private class ProcedureObserver : IObserver<ProcedureUpdateData>
        {
            private BaseToolbox mToolbox;

            public ProcedureObserver(BaseToolbox toolbox)
            {
                mToolbox = toolbox;
            }

            public void OnUpdated(object subject, ProcedureUpdateData args)
            {
                if (mToolbox == null || mToolbox.transform == null)
                    ((Observable<ProcedureUpdateData>)subject).RemoveObserver(this);
                else mToolbox.OnProcedureUpdate(args);
            }
        }

        #endregion

        #region Bin

        /// <summary>
        /// Check the block view is over the bin area, preparing dropped in bin
        /// </summary>
        public abstract bool CheckBin(BlockView blockView);

        /// <summary>
        /// Finish the check. 
        /// If the block view is over bin, drop it. 
        /// </summary>
        public abstract void FinishCheckBin(BlockView blockView);

        #endregion

        #region Monobehavior calls

        private void Update()
        {
            UpdatePickedBlockView();
        }

        #endregion

    }
}