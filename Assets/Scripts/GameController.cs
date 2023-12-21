using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Shapes;
public enum GameState{
    LevelSelect,
    InLevel
}
public class GameController : MonoBehaviour
{
    public GameState state;
    public bool useLevelSheet;
    [HideInInspector]
    public bool useJson = true;
    public bool editMode;
    public bool haveToWaitToEnd;//whether you need to actually press the wait button to end a loop
    public bool endFailedLoop;//will it actually loop if you didnt get a dot?
    public bool cloneCanUseSpawnPortal;
    public List<Player> players => Services.Grid.level.players;
    public Player currentPlayer => players[players.Count-1];
    public GameObject playerPrefab;
    public GameObject tilePrefab;
    public GameObject actionPointPrefab;
    public GameObject pitPrefab;
    public GameObject sporesPrefab;
    public GameObject animalPrefab;
    public GameObject spawnPortalPrefab;
    public GameObject exitPrefab;
    public GameObject linePrefab;
    public GameObject treeTilePrefab;
    public GameObject bushPrefab;
    public GameObject levelPreviewPrefab;
    public GameObject fogPrefab;
    public bool centerTimeLine;
    public Transform turnLimitParent;
    public Transform timelineArrow;//this is for left timeline
    //game logic
    public int currentLevel;//the literal level(map) you are on
    public int currentLoop;//the loop (player) you are on
    public int currentTurn;//the turn you are on
    public int currentPlayerIndex;//the current player that is moving this turn(player can only move when this is equal to currentLoop)
    public int turnLimit;
    public bool doLoopSoon;
    public bool canLoop;
    public bool doClonesMove;
    public bool undoClonesMove;
    public bool skipTurn;//so far only going to be called due to overlapping issues at start of loop
    public float winTime;
    int nextMove;//-2: undo, -1: nope, 0-3: that direction, 4: wait
    public Stack<TurnState> turns = new Stack<TurnState>();
    //end logic
    //display stuff
    [HideInInspector]
    public List<ActionPoint> turnLimitDisplay;
    //end display
    public TextMeshPro instructions;
    public string[] instructionsCopy;//0: level select, 1: inlevel
    public string fakeSave;
    [HideInInspector]
    public List<int> saveCharacters = new List<int>();
    [HideInInspector]
    public bool resetting;
    public bool noLimit => Services.LevelSelect.cursorPosition == new Vector2Int(0,0);
    public GameObject startMarkPrefab;

    //testGameMode
    public bool testLevel;
    public TestMoveNode parentNode;
    public TestMoveNode currentNode;
    public Dictionary<int,int> cloneNum2WinNum = new Dictionary<int, int>();
    public int whichDirection = 0;
    float nextMoveTimeAllowed;
    public Rectangle turnsAmount;
    // Start is called before the first frame update
    void Awake()
    {
        Application.targetFrameRate = 60;
        saveCharacters.Add(45);
        for(var i = 97; i <= 122; i++){
            saveCharacters.Add(i);
        }
        for(var i = 465; i <= 90;i++){
            saveCharacters.Add(i);
        }
        for(var i = 48; i <= 57;i++){
            saveCharacters.Add(i);
        }
        saveCharacters.Add(43);
        turnLimitDisplay = new List<ActionPoint>();
        InitializeServices();
        state = GameState.LevelSelect;
        Services.LevelSelect.Initialize();
        if(Services.LevelSelect.numUnlocked == 1 && !editMode){
            /*Services.Grid.LoadLevel(Services.LevelSelect.cursorPosition);
            Services.LevelSelect.Draw();
            Services.Grid.level.InstantCenter();*/
        }

        //Services.Grid.MakeGrid();
        winTime = -1f;
        for(int i = 0; i < instructionsCopy.Length;i++){
            string s = instructionsCopy[i];
            s = s.Replace("[","\n[");
            instructionsCopy[i] = s;
        }
    }

    void InitializeServices(){
        Services.GameController = this;
        Services.Grid = GetComponentInChildren<Grid>();
        Services.LevelSelect = GetComponentInChildren<LevelSelect>();
        Services.Visuals = GetComponentInChildren<Visuals>();
    }

    // Update is called once per frame
    void Update(){
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if(Input.GetKeyDown(KeyCode.Y)){
            /*if(testLevel == false && state == GameState.InLevel){
                parentNode = new TestMoveNode(-1,0,0,false,null);
                currentNode = parentNode;
                testLevel = true;
            }*/
        }
        if(Input.GetKeyDown(KeyCode.E)){
            SaveSystem.Save();
        }
        if(Input.GetKeyDown(KeyCode.F)){
            SaveSystem.ConvertSaveToLetters();
        }
        if(Input.GetKey(KeyCode.Alpha1)){
            if(Input.GetKeyDown(KeyCode.Alpha0)){
                //todo: unlock all cheat
                /*for(int i = 0; i < Services.LevelSelect.unlocked.Count;i++){
                    if(Services.LevelSelect.levels[i].careAboutBorders){
                        //Services.LevelSelect.unlocked[i] = true;
                        Services.LevelSelect.WinLevel(Services.LevelSelect.levels[i]);
                    }
                }*/
            }
            if(Input.GetKeyDown(KeyCode.Alpha9)){
                //todo: unlock level cheat
                /*if(state == GameState.LevelSelect){
                    if(Services.LevelSelect.v2Level.ContainsKey(Services.LevelSelect.cursorPosition)){
                        Level l = Services.LevelSelect.v2Level[Services.LevelSelect.cursorPosition];
                        for(var i = 0; i < 4; i++){
                            Vector2Int checkPos = l.gridPosition+Services.Grid.directions[i];
                            if(Services.LevelSelect.v2Level.ContainsKey(checkPos)){
                                Level levelToUnlock = Services.LevelSelect.v2Level[checkPos];
                                bool actuallyUnlock = true;
                                if(levelToUnlock.section != l.section){
                                    actuallyUnlock = false;
                                    if(l.sectionExit == i){
                                        actuallyUnlock = true;
                                    }
                                }
                                if(actuallyUnlock){
                                    //Services.LevelSelect.unlocked[levelToUnlock.index] = true;
                                    Services.LevelSelect.sections[levelToUnlock.section].visible = true;
                                    Services.LevelSelect.numUnlocked++;
                                }
                            }
                        }
                    }
                }*/
            }
            if(Input.GetKey(KeyCode.Alpha5) && Input.GetKeyDown(KeyCode.Alpha9)){
                //delete everything
                SaveSystem.Clear();
                SceneManager.LoadScene(0);
            }
        }
        if(editMode == false){
            if(state == GameState.InLevel){
                Services.LevelSelect.levelNameDisplay.text = Services.LevelSelect.PrintLevelMoves();
            }else{
                Services.LevelSelect.levelNameDisplay.text = "";
            }
        }
        switch(state){
            case GameState.LevelSelect:
                LevelSelectUpdate();
                break;
            case GameState.InLevel:
                InLevelUpdate();
                break;
        }
        instructions.text = instructionsCopy[(int)state];
    }

    void LevelSelectUpdate(){
        if(useLevelSheet){
            if(skipTurn == false){
                CheckForLevelSelectInput();
            }

            if(skipTurn){
                nextMove = 4;
            }
            if(nextMove >= 0){
                bool moved = DoTurnLevelSelect();
                if(moved){
                    nextMove = -1;
                }
                if(skipTurn){
                    skipTurn = false;
                }
            }
            //Debug.Log(Services.LevelSelect.levelSelectLevel);
            Services.LevelSelect.levelSelectLevel.DrawLevel();
            Services.LevelSelect.cursorPosition = Services.LevelSelect.levelSelectLevel.players[0].position;
        }else{
            int moveCursor = -1;
            if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)){
                moveCursor = 0;
            }
            if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)){
                moveCursor = 1;
            }
            if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)){
                moveCursor = 2;
            }
            if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)){
                moveCursor = 3;
            }
            if(moveCursor != -1){
                Services.LevelSelect.MoveCursor(moveCursor);
            }
        }
        
        
        if(Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space)){
            if(Services.LevelSelect.CursorOnLevel()){
                Services.Grid.LoadLevel(Services.LevelSelect.v2Preview[Services.LevelSelect.cursorPosition]);
            }else if(editMode){
                #if UNITY_EDITOR
                Services.LevelSelect.CreateLevel(Services.LevelSelect.cursorPosition);
                #endif
            }
        }
        #if UNITY_EDITOR
        /*if(editMode){
            Services.LevelSelect.LevelSelectEditorControls();
        }*/
        #endif
        Services.LevelSelect.Draw();
    }

    void InLevelUpdate()
    {
        if(Services.Grid.level.gameObject.transform.localScale.x > 0.9f){
            Services.LevelSelect.HandleTutorialText();
        }

        if(Input.GetKeyDown(KeyCode.R)){
            resetting = true;
            do{
                if(currentTurn == 0 && currentLoop == 0){break;}
                TurnState lastTurn = turns.Pop();
                UndoTurn(lastTurn);
                if(lastTurn.forced){
                    UndoTurn(lastTurn);
                }

                currentTurn = lastTurn.turn;
                currentLoop = lastTurn.loop;
                //UndoClonesTurn();
                //UndoTurn();
                currentPlayerIndex = currentLoop;
            }while(turns.Count > 0);
            resetting = false;
        }
        if(Services.Grid.levelPreview.won && winTime >= 0f && Time.time >= winTime+0.33f){
            Services.Grid.LeaveLevel();
            return;
        }
        #if UNITY_EDITOR
        if(editMode){
            Services.LevelSelect.LevelEditorControls();
        }
        #endif
        if(Input.GetKeyDown(KeyCode.Escape)){
            Services.Grid.LeaveLevel();
            return;
        }
        canLoop = false;
        if(currentTurn == turnLimit && noLimit == false){
            foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                if(ap.held){
                    canLoop = true;
                    break;
                }
            }
        }
        /*if(Input.GetKeyDown(KeyCode.R)){
            resetting = true;
        }
        if(resetting){
            Services.LevelSelect.DrawLevel
            return;
        }*/
        if(players.Count == 1){doClonesMove = false;}
        //bhandle making your clones go in order
        if(doClonesMove){
            //its clone moving time!
            bool done = players[currentPlayerIndex].isMoving == false;
            for(int i = 0; i < players.Count;i++){
                if(players[i].isMoving){
                    done = false;
                }
            }
            if(done){
                currentPlayerIndex++;
                if(currentPlayerIndex == currentLoop){
                    doClonesMove = false;
                    //ok so its time for the player to move
                    //if the player is overlapping, they have to wait
                    bool overlap = false;
                    for(int i = 0; i <= players.Count-2;i++){
                        if(players[i].position == currentPlayer.position){
                            overlap = true;
                            break;
                        }
                    }
                    if(overlap){
                        skipTurn = true;
                    }
                }
                if(currentPlayerIndex > currentLoop){
                    currentPlayerIndex = 0;
                }
                if(doClonesMove){
                    DoCloneTurn(currentPlayerIndex);
                }


            }
        }
        //handle going into new loop
        if(players[currentPlayerIndex].isMoving == false){
            if(doLoopSoon){
                doLoopSoon = false;
                foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                    if(ap.held){
                        ap.Collect();
                        break;
                    }
                }
                Debug.Log("new loop timebaby");
                NewLoop();
                doClonesMove = true;
                currentPlayerIndex = currentLoop;
                //DoCloneTurn(currentPlayerIndex);
                //DoClonesTurn();
            }
        }
        if(currentPlayerIndex == currentLoop && skipTurn == false){
            if(!testLevel){
                CheckForInput();
            }else{
                FakeInputForTest();
            }

        }

        if(skipTurn){
            nextMove = 4;
        }
        if(nextMove >= 0){
            TurnState potentialSave = new TurnState();
            bool moved = DoTurn();
            if(moved){
                nextMove = -1;
                turns.Push(potentialSave);
            }
            if(skipTurn){
                skipTurn = false;
            }
        }else if(nextMove == -2){
            nextMove = -1;
            //undo

            TurnState lastTurn = turns.Pop();
            UndoTurn(lastTurn);
            if(lastTurn.forced){
                Debug.Log("undo forced");
                lastTurn = turns.Pop();
                UndoTurn(lastTurn);
            }
            currentTurn = lastTurn.turn;
            currentLoop = lastTurn.loop;
            //UndoClonesTurn();
            //UndoTurn();
            currentPlayerIndex = currentLoop;
        }else if(nextMove == -3){
            //undo until the start of this clone
            nextMove = -1;
            TurnState lastTurn;
            while(currentTurn != 0){
                lastTurn = turns.Pop();
                UndoTurn(lastTurn);
                if(lastTurn.forced){
                    Debug.Log("undo forced");
                    lastTurn = turns.Pop();
                    UndoTurn(lastTurn);
                }
                currentTurn = lastTurn.turn;
                currentLoop = lastTurn.loop;
                //UndoClonesTurn();
                //UndoTurn();
                currentPlayerIndex = currentLoop;
            }
            if(currentLoop > 0){
                lastTurn = turns.Pop();
                UndoTurn(lastTurn);
                if(lastTurn.forced){
                    Debug.Log("undo forced");
                    lastTurn = turns.Pop();
                    UndoTurn(lastTurn);
                }
                currentTurn = lastTurn.turn;
                currentLoop = lastTurn.loop;
                //UndoClonesTurn();
                //UndoTurn();
                currentPlayerIndex = currentLoop;

                turns.Push(lastTurn);
                foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                    if(ap.held){
                        ap.Collect();
                        break;
                    }
                }
                NewLoop();
                doClonesMove = true;
                currentPlayerIndex = currentLoop;
            }
        }
        Services.Grid.level.DrawLevel();
        DrawTurnLimit();
        /*if(currentPlayer.position == Services.Grid.exitPosition && currentTurn < turnLimit){
            Services.Grid.exit.transform.GetChild(0).localEulerAngles+= (new Vector3(0,0,45f)-Services.Grid.exit.transform.GetChild(0).localEulerAngles)*Services.Visuals.lerpSpeed;
        }else{
            Services.Grid.exit.transform.GetChild(0).localEulerAngles+= (Vector3.zero-Services.Grid.exit.transform.GetChild(0).localEulerAngles)*Services.Visuals.lerpSpeed;
        }*/
    }

    void CheckForExit(Player player){
        bool allExits = true;
        bool onExit = false;
        if(Services.Grid.level.exits.ContainsKey(player.position)){
            onExit = true;
            Services.Grid.level.exits[player.position].TurnOn();
            foreach(Exit exit in Services.Grid.level.exits.Values){
                if(exit.on == false){
                    allExits = false;
                    break;
                }
            }
        }

        if(onExit && allExits){
            winTime = Time.time;
            player.winning = true;
            if(editMode){
                winTime = -1f;
            }
            if(Services.Grid.levelPreview.won == false){
                Services.LevelSelect.WinLevel(Services.Grid.level);
            }
        }
    }

    //Player stuff
    public void MakePlayer(Vector2Int pos){
        Debug.Log(pos);
        Player p = new Player(pos,Services.Grid.level.gameObject.transform);

        p.index = players.Count;
        players.Add(p);
    }

    void DrawTurnLimit(){
        turnLimitParent.gameObject.SetActive(!noLimit);
        if(noLimit){
            timelineArrow.localScale = Vector3.zero;
            return;
        }
        timelineArrow.localScale = Services.Visuals.LerpVector(timelineArrow.localScale,Vector3.one);
        if(centerTimeLine){
            turnLimitParent.localPosition = Services.Visuals.LerpVector(turnLimitParent.localPosition, new Vector3(-0.25f-(currentTurn*0.5f),turnLimitParent.localPosition.y,turnLimitParent.localPosition.z));
        }else{
            timelineArrow.localPosition = Services.Visuals.LerpVector(timelineArrow.localPosition,new Vector3(currentTurn*0.5f,0f,10f));
        }
        for(var i = 0; i < turnLimit;i++){
            turnLimitDisplay[i].on = i >= currentTurn;
        }
        for(var i = 0; i < turnLimitDisplay.Count;i++){
            turnLimitDisplay[i].Draw();
        }
    }

    //end player stuff
    //game loop
    void FakeInputForTest(){
        if(Time.time < nextMoveTimeAllowed){
            return;
        }
        nextMoveTimeAllowed = Time.time+0.075f;
        nextMove = -1;
        if(editMode && Services.LevelSelect.textEditMode){
            return;
        }
        if(currentPlayer.isMoving){
            return;
        }
        foreach(Player p in players){
            if(p.isMoving){
                return;
            }
        }
        if(currentTurn == turnLimit){
            nextMove = -1;
            //currentNode
            foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                if(ap.held){
                    doLoopSoon = true;
                    TurnState potentialSave = new TurnState();
                    turns.Push(potentialSave);
                    return;
                }
            }
        }
        //check if you're able to move
        if(currentTurn >= turnLimit && noLimit == false){
            //undo!
            nextMove = -2;
            currentNode = currentNode.parent;
            return;
        }
        nextMove = currentNode.nodes.Count;
        if(nextMove > 4){
            //you need to do undo again!
            if(currentNode == parentNode){
                testLevel = false;
                string s = "";
                foreach(int cloneNumber in cloneNum2WinNum.Keys){
                    s+=cloneNumber+" : "+cloneNum2WinNum[cloneNumber];
                    s+="\n";
                }
                nextMove = -1;
                return;
            }
            currentNode = currentNode.parent;
            nextMove = -2;
            return;
        }else{
            currentNode = currentNode.AddNode();
            if(nextMove == 4){
                bool won = false;
                foreach(Player p in players){
                    if(Services.Grid.level.exits.ContainsKey(p.position)){
                        won = true;
                        if(cloneNum2WinNum.ContainsKey(currentLoop)){
                            cloneNum2WinNum[currentLoop]+=1;
                        }else{
                            cloneNum2WinNum.Add(currentLoop,1);
                        }
                        break;
                    }
                }
                currentNode.won = true;
            }
        }

        if(currentTurn >= turnLimit && noLimit == false){
            //undo!
            nextMove = -2;
            currentNode = currentNode.parent;
            return;
        }
    }
    void CheckForLevelSelectInput(){
        nextMove = -1;
        Player player = Services.LevelSelect.levelSelectLevel.players[0];
        if(player.isMoving){
            return;
        }


        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)){
            nextMove = 0;
        }
        if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)){
            nextMove = 1;
        }
        if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)){
            nextMove = 2;
        }
        if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)){
            nextMove = 3;
        }
        if(Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Space)){
            nextMove = 4;
        }
    }
     bool DoTurnLevelSelect(){

        if(nextMove < 0){
            return false;
        }
        Player player = Services.LevelSelect.levelSelectLevel.players[0];
        if(player.dead){
            nextMove = 4;
        }
        
        if(nextMove > 3){
            //its waiting time!
            //player.Wait();
            return true;
        }
        //normal movement turn
        Tile currentTile = Services.LevelSelect.levelSelectLevel.tiles[player.position];

        if(true){
            //you can move there
            Vector2Int newPos = player.position+Services.Grid.directions[nextMove];
            bool cannotMove = Services.LevelSelect.levelSelectLevel.tiles.ContainsKey(newPos) == false;
            //if(cannotMove){return false;}
            player.AddMove(nextMove);
            if(cannotMove){
                player.FailureMove(nextMove);
            }else{
                player.Move(nextMove);
            }

            //now check to see if the player ran into an action point
            bool ranIntoPoint = false;
            if(ranIntoPoint){
                //doLoopSoon = true;
                return true;
            }else{
            }
            return true;
        }else{
            //there is not a tile there
            return false;
        }
    }

    void CheckForInput(){
        nextMove = -1;
        if(editMode && (Services.LevelSelect.textEditMode || Services.LevelSelect.nameEditMode)){
            return;
        }
        if(currentPlayer.isMoving){
            return;
        }
        foreach(Player p in players){
            if(p.isMoving){
                return;
            }
        }
        if(turns.Count > 0){
            if(Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J)){
                nextMove = -2;
                return;
            }
        }


        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)){
            nextMove = 0;
        }
        if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)){
            nextMove = 1;
        }
        if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)){
            nextMove = 2;
        }
        if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)){
            nextMove = 3;
        }
        if(Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Space)){
            nextMove = 4;
        }
        if(currentTurn == turnLimit && noLimit == false){
            if((haveToWaitToEnd == false && nextMove > -1) || (haveToWaitToEnd && nextMove == 4)){//Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.K)){
                nextMove = -1;
                foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                    if(ap.held){
                        //THIS IS WHERE EATING IS SET RN
                        ap.playerHolding.eating = true;
                        ap.playerHolding.isMoving = true;
                        doLoopSoon = true;
                        TurnState potentialSave = new TurnState();
                        turns.Push(potentialSave);
                        return;
                    }
                }
                if(endFailedLoop){
                     nextMove = -3;
                }

                return;
            }
        }
        if(currentTurn >= turnLimit && noLimit == false){
            nextMove = -1;
            return;
        }
    }

    bool DoTurn(){

        if(nextMove < 0){
            return false;
        }
        Debug.Log(nextMove);
        if(currentPlayer.dead){
            nextMove = 4;
        }
        if(Services.Grid.tiles.ContainsKey(currentPlayer.position) && Services.Grid.tiles[currentPlayer.position].hasSpores){
            nextMove = currentPlayer.moves[currentPlayer.moves.Count-1];
        }
        if(nextMove > 3){
            //its waiting time!
            currentPlayer.Wait();
            if(Services.Grid.tiles[currentPlayer.position].hasSpawnPortal){
                ActionPoint newActionPoint = new ActionPoint(gameObject.transform.parent);
                newActionPoint.Collect();
                NewLoop(currentPlayer);
                doClonesMove = true;
                currentPlayerIndex = currentLoop;
                return true;
            }else{
                CheckForExit(currentPlayer);
                currentPlayer.AddMove(nextMove);
                currentTurn++;
                CheckForGrab();
                doClonesMove = true;
                return true;
            }

        }
        //normal movement turn
        Tile currentTile = Services.Grid.tiles[currentPlayer.position];

        if(true){
            //you can move there
            Vector2Int newPos = currentPlayer.position+Services.Grid.directions[nextMove];
            bool cannotMove = false;
            bool canPush = currentPlayer.CanPush(nextMove,0);
            if(canPush){
                currentPlayer.Push(nextMove);
            }else{
                cannotMove = true;
            }
            if(cannotMove){
                if(Services.Grid.tiles[currentPlayer.position].spikes[nextMove]){
                    currentPlayer.dead = true;
                }
            }
            //if(cannotMove){return false;}
            currentPlayer.AddMove(nextMove);
            if(cannotMove){
                currentPlayer.FailureMove(nextMove);
            }else{
                currentPlayer.Move(nextMove);
            }

            currentTurn++;
            //now check to see if the player ran into an action point
            bool ranIntoPoint = false;
            CheckForGrab();
            if(ranIntoPoint){
                //doLoopSoon = true;
                return true;
            }else{
                doClonesMove = true;
            }
            return true;
        }else{
            //there is not a tile there
            return false;
        }
    }

    void UndoTurn(TurnState lastTurn){

        for(var i = 0; i < lastTurn.playerStates.Count;i++){

            //players[i].SetPosition(lastTurn.playerPositions[i]);
            if(players[i].moves[players[i].moves.Count-1] >= 4 && players[i].winning == false){
                players[i].waitingStatus = 2;
                //players[i].changingSitting = true;
            }
            players[i].position = lastTurn.playerStates[i].position;
            players[i].spawnPos = lastTurn.playerStates[i].spawnPos;
            players[i].dead = lastTurn.playerStates[i].dead;
            players[i].moves = lastTurn.playerStates[i].moves;
            if(players[i].sittingDown != lastTurn.playerStates[i].sittingDown){
                players[i].waitToMoveToUndoSitting = true;
                players[i].animIndex = 0;
            }
            //players[i].sittingDown = lastTurn.playerStates[i].sittingDown;
            players[i].isMoving = true;
            if(editMode && players[i].winning){
                players[i].winning = false;
            }

        }
        if(lastTurn.loop == currentLoop-1){
            //you're going back a loop!
            //turnLimit--;
            currentPlayer.Destroy();
            players.RemoveAt(players.Count-1);
            if(Services.Grid.tiles[players[players.Count-1].position].hasSpawnPortal == false){
                while(turnLimitDisplay[turnLimitDisplay.Count-1].createdMidGame){
                    turnLimitDisplay[turnLimitDisplay.Count-1].UndoCollect();
                }
            }

            turnLimitDisplay[turnLimitDisplay.Count-1].UndoCollect();

        }
        foreach(ActionPoint ap in lastTurn.actionPointStates.Keys){
            if(ap.held != lastTurn.actionPointStates[ap].held){
                //currently holding, but you weren't before
                if(ap.held == true){
                    ap.UnGrab();
                }else{
                    //not holding, but you were!
                    ap.Grab(players[lastTurn.actionPointStates[ap].playerHoldingIndex]);
                }
            }
        }
        foreach(Exit exit in lastTurn.exitStates.Keys){
            if(exit.on != lastTurn.exitStates[exit]){
                if(exit.on){
                    //it used to be turned off
                    exit.TurnOff();
                }else{
                    //it used to be on
                    exit.TurnOn();
                }
            }
        }
        foreach(Vector2Int pos in lastTurn.pitStates.Keys){
            Services.Grid.level.tiles[pos].pitFilled = lastTurn.pitStates[pos];
        }
    }

    void DoCloneTurn(int index){
        if(players[index].moves.Count > currentTurn){
            if(players[index].inPit){
                return;
            }
            int thisMove = players[index].moves[currentTurn];
            if(players[index].dead){
                thisMove = Services.Grid.directions.Length;
            }
            if(Services.Grid.tiles[players[index].position].hasSpores){
                thisMove = players[index].moves[currentTurn-1];
            }
            if(thisMove >= Services.Grid.directions.Length){
                //wait
                players[index].Wait();
                if(Services.Grid.tiles[players[index].position].hasSpawnPortal){
                    if(cloneCanUseSpawnPortal){
                        players[index].spawnPos = players[index].position;
                    }
                    players[index].inPit = true;
                }
                CheckForGrab();
                CheckForExit(players[index]);
                return;
            }
            if(Services.Grid.tiles[players[index].position].canMove[thisMove] == false){
                if(Services.Grid.tiles[players[index].position].spikes[thisMove]){
                    players[index].dead = true;
                }
                players[index].FailureMove(thisMove);
                return;
            }
            Vector2Int newPos = players[index].position+Services.Grid.directions[thisMove];
            bool cannotMove = false;
            bool canPush = players[index].CanPush(thisMove,0);
            if(canPush){
                players[index].Push(thisMove);
            }else{
                cannotMove = true;
                players[index].FailureMove(thisMove);
            }
            if(cannotMove){return;}
            players[index].Move(players[index].moves[currentTurn]);
            CheckForGrab();
        }
    }

    void CheckForGrab(){
        foreach(Player p in players){
            if(Services.Grid.actionPoints.ContainsKey(p.position)){
                //doLoopSoon = true;
                bool doNotGrab = false;
                if(Services.Grid.actionPoints[p.position].CanGrab()){
                    foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                        if(ap.gridPosition == p.position){
                            if(ap.held && ap.playerHolding != p){
                                doNotGrab = true;
                            }
                            continue;
                        }
                        if(ap.held){
                            if(ap.playerHolding == p){
                                //player is already holding
                                ap.UnGrab();
                            }
                        }
                    }
                }else{
                    doNotGrab = true;
                }
                if(doNotGrab ==  false){
                    Services.Grid.actionPoints[p.position].Grab(p);
                }
                break;
            }
        }
    }

    void NewLoop(){
        currentLoop++;
        currentTurn = 0;
        //turnLimit++;
        foreach(Player player in players){
            player.Reset();
        }
        foreach(Exit exit in Services.Grid.level.exits.Values){
            exit.TurnOff();
        }
        foreach(Tile tile in Services.Grid.level.tiles.Values){
            if(tile.hasPit){
                tile.UnFillPit();
            }
        }
        foreach(ActionPoint point in Services.Grid.level.actionPoints.Values){
            point.flowerIndex = 0;
        }
        MakePlayer(Services.Grid.playerStartPosition);

    }

    void NewLoop(Player _player){
        Vector2Int pos = _player.position;
        currentLoop++;
        currentTurn = 0;
        //turnLimit++;
        foreach(Player player in players){
            player.Reset();
        }
        foreach(Exit exit in Services.Grid.level.exits.Values){
            exit.TurnOff();
        }
        foreach(Tile tile in Services.Grid.level.tiles.Values){
            if(tile.hasPit){
                tile.UnFillPit();
            }
        }
        MakePlayer(pos);
    }
    //end
}

//so this is exactly what the board and state of the game is on one specific turn
public class TurnState
{
    public int turn;
    public int loop;
    public bool forced;
    public List<PlayerState> playerStates;
    public Dictionary<ActionPoint,ActionPointState> actionPointStates;
    public Dictionary<Exit,bool> exitStates;
    public Dictionary<Vector2Int,bool> pitStates;
    public TurnState(){
        playerStates = new List<PlayerState>();
        foreach(Player p in Services.GameController.players){
            playerStates.Add(new PlayerState(p));
        }
        actionPointStates = new Dictionary<ActionPoint, ActionPointState>();
        foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
            actionPointStates.Add(ap,new ActionPointState(ap));
        }
        exitStates = new Dictionary<Exit, bool>();
        foreach(Exit exit in Services.Grid.level.exits.Values){
            exitStates.Add(exit,exit.on);
        }
        pitStates = new Dictionary<Vector2Int, bool>();
        foreach(Tile tile in Services.Grid.level.tiles.Values){
            if(tile.hasPit == false){continue;}
            pitStates.Add(tile.position,tile.pitFilled);
        }
        turn = Services.GameController.currentTurn;
        loop = Services.GameController.currentLoop;
        forced = Services.GameController.skipTurn;
        if(forced){
            Debug.Log("FORCED CREATED");
        }
    }
}

public class TestMoveNode 
{
    public int move;
    public bool won;//did you win with the above move?
    public int depth;//from node 0
    public int cloneNumber;
    public TestMoveNode parent;
    public List<TestMoveNode> nodes;
    public TestMoveNode(int move, int depth, int cloneNumber, bool won, TestMoveNode parent){
        this.move = move;
        this.depth = depth;
        this.cloneNumber = cloneNumber;
        this.won = won;
        this.parent = parent;
        nodes = new List<TestMoveNode>();
    }
    public TestMoveNode AddNode(){
        TestMoveNode newNode = new TestMoveNode(nodes.Count,depth+1,Services.GameController.currentLoop,false,this);
        nodes.Add(newNode);
        return newNode;
    }
}