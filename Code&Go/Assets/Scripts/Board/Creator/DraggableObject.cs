﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using AssetPackage;

public class DraggableObject : MonoBehaviour, IMouseListener
{
    private BoardManager board;
    private ArgumentLoader argumentLoader = null;
    private BoardObject boardObject;

    private Vector3 mouseOffset;
    Vector2Int lastPos = -Vector2Int.one;
    private float zCoord;
    private bool dragging = false;
    private bool modifiable = true;

    private CameraMouseInput cameraInput = null;
    private OrbitCamera orbitCamera = null;

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void Start()
    {
        boardObject = GetComponent<BoardObject>();
    }

    private void Update()
    {
        if (modifiable && dragging && cameraInput)
        {
            transform.position = GetMouseWorldPos() + mouseOffset;
            if (Input.GetMouseButtonUp(0))
                OnLeftUp();
        }
    }

    public void SetBoard(BoardManager board)
    {
        this.board = board;
    }

    public void SetOrbitCamera(OrbitCamera orbitCamera)
    {
        this.orbitCamera = orbitCamera;
    }

    public void SetArgumentLoader(ArgumentLoader loader)
    {
        this.argumentLoader = loader;
    }

    public void SetCameraInput(CameraMouseInput cameraInput)
    {
        this.cameraInput = cameraInput;
    }

    private void OnLeftDown()
    {
        if (!modifiable) return;
        if (orbitCamera != null && !orbitCamera.IsReset()) return;
        if (boardObject == null) boardObject = GetComponent<BoardObject>();
        if (argumentLoader != null) argumentLoader.SetBoardObject(boardObject);
        if (cameraInput != null) cameraInput.SetDragging(true);

        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        mouseOffset = transform.position - GetMouseWorldPos();
        dragging = true;

        string name = boardObject.GetName();
        TrackerAsset.Instance.setVar("element_type", name.ToLower());
        TrackerAsset.Instance.setVar("element_name", boardObject.GetNameWithIndex().ToLower());
        TrackerAsset.Instance.setVar("position", lastPos.ToString());
        TrackerAsset.Instance.setVar("rotation", boardObject.GetDirection().ToString().ToLower());
        TrackerAsset.Instance.setVar("action", "pick");
        TrackerAsset.Instance.GameObject.Interacted(boardObject.GetID());
    }

    private void OnRightDown()
    {
        if (!modifiable) return;
        if (dragging) return;
        if (boardObject == null) boardObject = GetComponent<BoardObject>();
        if (argumentLoader != null) argumentLoader.SetBoardObject(boardObject);

        boardObject.Rotate(1);

        string name = boardObject.GetName();
        TrackerAsset.Instance.setVar("element_type", name.ToLower());
        TrackerAsset.Instance.setVar("element_name", boardObject.GetNameWithIndex().ToLower());
        TrackerAsset.Instance.setVar("position", lastPos.ToString());
        TrackerAsset.Instance.setVar("rotation", boardObject.GetDirection().ToString().ToLower());
        TrackerAsset.Instance.setVar("action", "rotate");
        TrackerAsset.Instance.GameObject.Interacted(boardObject.GetID());
    }

    private void OnLeftUp()
    {
        if (modifiable && dragging && orbitCamera.IsReset())
        {
            dragging = false;
            if (cameraInput != null) cameraInput.SetDragging(false);

            Vector3 pos = board.GetLocalPosition(transform.position);
            pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));

            string name = boardObject.GetName();
            TrackerAsset.Instance.setVar("element_type", name.ToLower());
            TrackerAsset.Instance.setVar("element_name", boardObject.GetNameWithIndex().ToLower());
            TrackerAsset.Instance.setVar("old_position", lastPos.ToString());
            TrackerAsset.Instance.setVar("rotation", boardObject.GetDirection().ToString().ToLower());

            if (boardObject != null && pos.x < board.GetColumns() && pos.x >= 0 && pos.z < board.GetRows() && pos.z >= 0)
            {
                Vector2Int newPos = new Vector2Int(Mathf.FloorToInt(pos.x), (Mathf.FloorToInt(pos.z)));
                TrackerAsset.Instance.setVar("new_position", newPos.ToString());

                //Si la posicion en la que se suelta es donde estaba colocado no se hace nada
                if (lastPos == newPos)
                {
                    transform.localPosition = new Vector3(lastPos.x, 0, lastPos.y);
                    TrackerAsset.Instance.setVar("action", "move");
                    TrackerAsset.Instance.setVar("exception", "placed_in_the_same_cell");
                    TrackerAsset.Instance.GameObject.Interacted(boardObject.GetID());
                    return;
                }
                //Si se suelta en una celda ocupada o en un agujero se elimina
                if (board.GetBoardCellType(newPos.x, newPos.y) == 1 || board.IsCellOccupied(newPos.x, newPos.y))
                {
                    //Se elimina el objeto en la posicion anterior
                    board.RemoveBoardObject(lastPos.x, lastPos.y);
                    Destroy(gameObject, 0.3f);
                    TrackerAsset.Instance.setVar("action", "remove");
                    TrackerAsset.Instance.setVar("exception", "placed_on_non_valid_cell");
                    TrackerAsset.Instance.GameObject.Interacted(boardObject.GetID());
                    return;
                }
                //Si el objeto no se ha añadido al tablero
                if (lastPos == -Vector2Int.one)
                {
                    board.AddBoardObject(newPos.x, newPos.y, boardObject);
                    TrackerAsset.Instance.setVar("first_time_placed", true);
                    if (argumentLoader != null) argumentLoader.SetBoardObject(boardObject);
                }
                //Se mueve el objeto
                else if (!board.MoveBoardObject(lastPos, newPos))
                {
                    //Si no se ha podido mover se deja donde estaba
                    transform.localPosition = new Vector3(lastPos.x, 0, lastPos.y);

                    TrackerAsset.Instance.setVar("action", "move");
                    TrackerAsset.Instance.setVar("exception", "denied_by_board");
                    TrackerAsset.Instance.GameObject.Interacted(boardObject.GetID());
                    return;
                }
                lastPos = newPos;

                TrackerAsset.Instance.setVar("action", "move");
                TrackerAsset.Instance.GameObject.Interacted(boardObject.GetID());
            }
            else
            {
                board.RemoveBoardObject(lastPos.x, lastPos.y);
                Destroy(gameObject, 0.3f);

                TrackerAsset.Instance.setVar("action", "remove");
                TrackerAsset.Instance.GameObject.Interacted(boardObject.GetID());
            }
        }
    }

    public void OnMouseButtonDown(int index)
    {
        if (index == 0)
            OnLeftDown();
        else if (index == 1)
            OnRightDown();
    }

    public void OnMouseButtonUp(int index)
    {

    }

    public void SetLastPos(Vector2Int lastPos)
    {
        this.lastPos = lastPos;
    }

    public void SetModifiable(bool modifiable)
    {
        this.modifiable = modifiable;
    }

    public void OnMouseButtonUpAnywhere(int index)
    {
        //throw new System.NotImplementedException();
    }
}
