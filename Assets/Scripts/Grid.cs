using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public Level level;
    //runtime
    [HideInInspector]
    public Vector2Int[] directions = new Vector2Int[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
    [HideInInspector]
    public Vector2Int[] corners => new Vector2Int[]{new Vector2Int(1,1), new Vector2Int(1,0),new Vector2Int(0,0),new Vector2Int(0,1)};
    [HideInInspector]
    public Dictionary<Vector2Int,Tile> tiles => level.tiles;

    public Dictionary<Vector2Int,ActionPoint> actionPoints => level.actionPoints;
    Vector2Int size;
    public Vector2Int playerStartPosition => level.startPosition;
    public Camera camera;
    public void ResetGrid(){
        Services.GameController.currentLoop = 0;
        Services.GameController.currentTurn = 0;
        Services.GameController.turnLimit = 1;
        Services.GameController.currentPlayerIndex = 0;
        for(int i =  transform.childCount-1;i>=0;i--){
            GameObject.Destroy(transform.GetChild(i).gameObject);
        }
        for(int i = Services.GameController.turnLimitParent.childCount-1;i>=0;i--){
            GameObject.Destroy(Services.GameController.turnLimitParent.GetChild(i).gameObject);
        }
        Services.GameController.turnLimitDisplay.Clear();
        Services.GameController.turns.Clear();
        Services.GameController.players.Clear();
        tiles.Clear();
        actionPoints.Clear();
    }
    public void LoadLevel(Level l){
        SaveSystem.Save();
        if(Services.GameController.centerTimeLine == false){
            Services.GameController.timelineArrow.localPosition = Vector2.zero;
        }
        //
        Services.GameController.winTime = -1f;
        level = l;
        foreach(Exit exit in level.exits.Values){
            exit.TurnOff();
        }
        level.on = true;
        Services.GameController.state = GameState.InLevel;
        Services.GameController.turnLimit = l.turnLimit;
        foreach(ActionPoint turnPoint in level.turnPoints){

            turnPoint.gameObject.transform.parent = Services.GameController.turnLimitParent;
            turnPoint.gameObject.transform.localScale = Vector3.one;
            turnPoint.gameObject.transform.position = Services.GameController.timelineArrow.position-Vector3.right*0.5f;
            Services.GameController.turnLimitDisplay.Add(turnPoint);
        }
        foreach(Level le in Services.LevelSelect.levels){
            if(le.on == false){
                le.gameObject.SetActive(false);
            }
        }
    }
    public void LoadLevel(Vector2Int pos){
        if(Services.LevelSelect.v2Level.ContainsKey(pos)){
            LoadLevel(Services.LevelSelect.v2Level[pos]);
        }
    }
    public void LeaveLevel(){
        //Services.GameController.timelineArrow.localPosition = Vector2.zero;
        Services.GameController.timelineArrow.localScale = Vector3.zero;
        Services.GameController.winTime = -1f;
        #if UNITY_EDITOR
        if(level.changed){
            Services.LevelSelect.ResetLevelChanges(level);
        }
        #endif
        Services.GameController.turns.Clear();
        Services.GameController.currentLoop = 0;
        Services.GameController.currentTurn = 0;
        Services.GameController.turnLimit = 1;
        Services.GameController.currentPlayerIndex = 0;
        level.on = false;
        level.Reset();
        level = null;
        Services.GameController.state = GameState.LevelSelect;
        foreach(Level le in Services.LevelSelect.levels){
            le.gameObject.SetActive(Services.LevelSelect.unlocked[le.index]);
        }
    }
    public int opposite(int i){
        return (i+2+4)%4;
    }
}
