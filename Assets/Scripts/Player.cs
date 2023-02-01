using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Shapes;

public class Player
{
    public Vector2Int position;
    public bool isMoving;
    public bool completedLoop;
    public int index;
    public int failureDirection = -1;
    public int waitingStatus;//0: not waiting, 1: turning
    public bool sittingDown;
    public bool changingSitting;
    float timeTurnStarted;

    public List<int> moves = new List<int>();
    
    GameObject gameObject;
    Disc head;
    RegularPolygon body;
    Rectangle[] legs = new Rectangle[2];
    GameObject noseCenter;
    RegularPolygon nose;

    TextMeshPro cloneNumber;
    float[] lineWidths = new float[2];
    Player pusher;//player pushing me
    public bool winning;
    public bool dead;
    public bool inPit;

    public Player(Vector2Int pos, Transform parent){
        index = 0;//Services.GameController.players.Count;
        position = pos;
        gameObject = GameObject.Instantiate(Services.GameController.playerPrefab,(Vector2)position,Quaternion.identity,parent);
        gameObject.transform.localPosition = (Vector2)position + Vector2.one*0.5f;
        head = gameObject.GetComponentInChildren<Disc>();
        body = gameObject.GetComponentInChildren<RegularPolygon>();
        legs[0] = body.transform.GetChild(1).GetComponent<Rectangle>();
        legs[1] = body.transform.GetChild(2).GetComponent<Rectangle>();
        cloneNumber = gameObject.GetComponentInChildren<TextMeshPro>();
        noseCenter = head.transform.GetChild(0).gameObject;
        nose = noseCenter.GetComponentInChildren<RegularPolygon>();
    }
    public void SetPosition(Vector2Int pos){
        position = pos;
        gameObject.transform.localPosition = (Vector2)position + Vector2.one*0.5f;
    }
    public void Destroy(){
        GameObject.Destroy(gameObject);
    }
    public void Reset(){
        dead = false;
        inPit = false;
        position = Services.Grid.playerStartPosition;
        gameObject.transform.localPosition = (Vector2)position + Vector2.one*0.5f;
    }
    public void UndoReset(){
        //this needs to only move the amount of steps it took the last loop
        position = Services.Grid.playerStartPosition;
        for(int i = 0; i < Services.GameController.currentTurn;i++){
            if(moves.Count <= i){break;}
            position+=Services.Grid.directions[moves[i]];
        }
        gameObject.transform.localPosition = (Vector2)position;
    }
    //doesn't actually push, just checks to see if it can be pushed
    public bool CanPush(int direction, int depth){
        if(Services.Grid.tiles[position].canMove[direction] == false){
            if(Services.Grid.tiles[position].spikes[direction]){
                if(depth == 0){
                    //you're dead!
                    return false;
                }else{
                    //go through!
                    if(Services.Grid.tiles.ContainsKey(position+Services.Grid.directions[direction]) == false){
                        //you're dead!
                        return false;
                    }
                }
            }else{
                return false;
            }
        }
        Vector2Int newPos = position+Services.Grid.directions[direction];
        foreach(Player p in Services.GameController.players){
            if(p == this){continue;}
            if(p.position == newPos){
                return p.CanPush(direction, depth+1);
            }
        }
        return true;
    }
    public void Push(int direction){
        foreach(Player p in Services.GameController.players){
            if(p == this){continue;}
            if(p.position == position+Services.Grid.directions[direction]){
                p.BePushed(this,direction);
            }
        }
    }
    public void BePushed(Player pushert,int direction){
        pusher = pushert;
        Move(direction);
        foreach(Player p in Services.GameController.players){
            if(p == this){continue;}
            if(p.position == position){
                p.BePushed(this,direction);
            }
        }
    }
    public void Move(int direction){
        if(direction >= Services.Grid.directions.Length){return;}
        //physically move, seperate from remembering it
        position+=Services.Grid.directions[direction];
        if(Services.Grid.tiles[position].CanWalk == false){
            Services.Grid.tiles[position].FillPit();
            inPit = true;
            dead = true;
            //TODO: figure out if an action point is already here!!
            foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                if(ap.held && ap.playerHolding == this){
                    ap.UnGrab();
                    break;
                }
            }
            position = new Vector2Int(-10,-10);
        }
        isMoving = true;
        if(sittingDown){
            changingSitting = true;
        }
    }
    public void FailureMove(int direction){
        failureDirection = direction;
        isMoving = true;
        if(sittingDown){
            changingSitting = true;
        }
    }
    public void AddMove(int direction){
        //add a move to your history
        moves.Add(direction);
    }
    public void PopMoves(){
        moves.RemoveAt(moves.Count-1);
    }
    public void Wait(){
        if(dead){
            return;
        }
        waitingStatus = 1;
        timeTurnStarted = Time.time;
        isMoving =true;
        if(sittingDown == false){
            changingSitting = true;
        }
    }

    public void Draw(){
        //nose stuff
        int noseDirection = 2;
        int toCheck = Services.GameController.currentTurn;
        if(Services.GameController.currentPlayerIndex > index && Services.GameController.doClonesMove == false){
            toCheck++;
        }
        for(int i = toCheck; i>= 0; i--){
            if(i >= moves.Count){
                continue;
            }
            if(moves[i] <= 3){
                Debug.Log("B");
                noseDirection = moves[i];
                break;
            }
        }
        noseCenter.transform.localEulerAngles = new Vector3(0,0,Services.Visuals.angles[noseDirection]);
        //end nose stuff
        if(winning){
            gameObject.transform.eulerAngles += new Vector3(0,0,10f*(Time.deltaTime/0.016f));
            gameObject.transform.localScale += (Vector3.zero-gameObject.transform.localScale)*0.05f*(Time.deltaTime/0.016f);
        }else{
            gameObject.transform.eulerAngles = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
        }
        cloneNumber.text = (index+1).ToString();//((char)(index+65)).ToString();
        cloneNumber.color = Services.Visuals.tileColor;
        if(index == Services.GameController.currentLoop){
            cloneNumber.text = "<voffset=0.45em><sprite=6>";
            if(Services.GameController.canLoop){
                cloneNumber.text+="<line-height=80%><voffset=0.6em>\n<sprite=6>";
            }else if(dead){
                cloneNumber.text+="<line-height=70%>\nx";
            }
        }else{
            cloneNumber.text+="<line-height=70%>\n";
            int currentTurn = Services.GameController.currentTurn;
            if(Services.GameController.currentPlayerIndex > index && Services.GameController.doClonesMove == false){
                currentTurn++;
            }
            if(dead){
                cloneNumber.text+="x";
            }else{
                if(moves.Count-1 >= currentTurn){
                    cloneNumber.text+="<sprite="+moves[currentTurn].ToString()+">";
                }else{
                    //cloneNumber.text+="<sprite=4>";
                }
            }
            
        }
        if(Services.GameController.state == GameState.LevelSelect){
            cloneNumber.text = "";
        }
        body.Color = index != Services.GameController.currentLoop ? Services.Visuals.cloneColor : Services.Visuals.playerColor;
        head.Color = body.Color;
        legs[0].Color = head.Color;
        legs[1].Color = head.Color;
        body.SortingOrder = -index*5;
        head.SortingOrder = body.SortingOrder+2;
        legs[0].SortingOrder = body.SortingOrder;
        legs[1].SortingOrder = body.SortingOrder;
        cloneNumber.sortingOrder = body.SortingOrder+3;
        nose.SortingOrder = body.SortingOrder+1;
        if(isMoving == false){return;}

        if(changingSitting){
            //you're getting up
            if(sittingDown){
                body.transform.localPosition +=(new Vector3(0,-0.1f,0f)-body.transform.localPosition)*Services.Visuals.lerpSpeed*1.5f;
                legs[0].transform.localPosition +=(new Vector3(-0.1f,-0.2f,0f)-legs[0].transform.localPosition)*Services.Visuals.lerpSpeed*1.5f;
                legs[1].transform.localPosition +=(new Vector3(0.1f,-0.2f,0f)-legs[1].transform.localPosition)*Services.Visuals.lerpSpeed*1.5f;
                if(Vector2.Distance(body.transform.localPosition,new Vector2(0,-0.1f)) < 0.01f){
                    sittingDown = false;
                    changingSitting = false;
                }
            }else{
                body.transform.localPosition +=(new Vector3(0,-0.2f,0f)-body.transform.localPosition)*Services.Visuals.lerpSpeed*1.5f;
                legs[0].transform.localPosition +=(new Vector3(-0.1f,0f,0f)-legs[0].transform.localPosition)*Services.Visuals.lerpSpeed*1.5f;
                legs[1].transform.localPosition +=(new Vector3(0.1f,0f,0f)-legs[1].transform.localPosition)*Services.Visuals.lerpSpeed*1.5f;
                if(Vector2.Distance(body.transform.localPosition,new Vector2(0,-0.2f)) < 0.01f){
                    //changingSitting = false;
                    sittingDown = true;
                }
            }
            return;
        }
        if(waitingStatus == 1 || waitingStatus == 2){
            if(Time.time > timeTurnStarted+0.13f){
                waitingStatus = 0;
            }
            return;
        }
        if(ReferenceEquals(pusher,null) == false){
            if(Vector2.Distance(pusher.gameObject.transform.position,gameObject.transform.position) < 0.25f){
                pusher = null;
            }else{
                return;
            }
        }
        Vector3 targetPosition = (Vector3)(Vector2)position;
        targetPosition+=(Vector3)Vector2.one*0.5f;
        if(failureDirection != -1){
            targetPosition += (Vector3)(Vector2)Services.Grid.directions[failureDirection]*0.5f;
        }
        if(sittingDown == false){
            float t = (Mathf.Sin(Time.time*20f)+1f)/2f;
            legs[0].transform.localPosition =(new Vector3(-0.1f,Mathf.Lerp(-0.2f,-0.05f,t),0f));
            legs[1].transform.localPosition =(new Vector3(0.1f,Mathf.Lerp(-0.05f,-0.2f,t),0f));
        }
        
        if(Vector2.Distance(targetPosition,gameObject.transform.localPosition) < 0.1f){
            gameObject.transform.localPosition = targetPosition;
            isMoving = false;
            if(failureDirection != -1){
                failureDirection = -1;
                isMoving = true;
            }
            if(isMoving == false && sittingDown == false){
                legs[0].transform.localPosition =new Vector3(-0.1f,-0.2f,0f);
                legs[1].transform.localPosition =new Vector3(0.1f,-0.2f,0f);
            }
            
        }
        
        gameObject.transform.localPosition += (targetPosition-gameObject.transform.localPosition)*Services.Visuals.lerpSpeed*1.5f;
    }
    public void FollowMouse(Vector2 pos){
        Vector3 target = (Vector3)(pos);
        target.x += 0.1f*Mathf.Sin(Time.time*5f);
        target.y += 0.1f*Mathf.Cos(Time.time*3f);
        gameObject.transform.position = target;
    }
}
public class PlayerState{
    public Vector2Int position;
    public List<int> moves;
    public bool sittingDown;
    public bool dead;
    public PlayerState(Player player){
        position = player.position;
        sittingDown = player.sittingDown;
        dead = player.dead;
        moves = new List<int>();
        foreach(int i in player.moves){
            moves.Add(i);
        }
    }
}