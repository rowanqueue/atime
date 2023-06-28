using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using TMPro;

public class LevelSelect : MonoBehaviour
{
    public LevelDatabase database;
    public Vector2Int startPosition;
    
    public List<LevelPreview> levelPreviews;
    public List<Level> levels;
    public List<bool> unlocked; public int numUnlocked = 1;
    public List<bool> won; public int numWon = 0;
    public GameObject cursor;
    public List<LineRenderer> cursorLines = new List<LineRenderer>();
    public Vector2Int cursorPosition;
    public Dictionary<Vector2Int,Level> v2Level = new Dictionary<Vector2Int, Level>();
    public Dictionary<string,Level> name2Level = new Dictionary<string, Level>();
    public TextMeshPro levelNameDisplay;
    bool levelMovesCursorOn;
    float levelMovesCursorCountdown;
    public Transform borderParent;
    public TextMeshPro tutorialTextDisplay;
    int tutorialIndex;
    float nextLetterTime;
    public List<Section> sections;

    //level editing stuff
    public bool movingStartPosition;
    public bool movingExit;
    public Exit exitMoving; bool newExit;
    public bool holdingLevelDown;
    public Vector2Int levelHolding;
    public float timeHoldStarted;
    public bool rightHold;
    public bool textEditMode;
    public bool nameEditMode;
    //level editing sections
    public int sectionSelected = -1;//-1 is none...
    public class LevelPreview{
        public LevelJson data;
        public GameObject gameObject;
        public string name = "";
        public bool won = false;
        public bool unlocked = false;
        
    }
    public void Initialize(){
        #if UNITY_EDITOR
        FindAllLevelAssets();
        #endif
        sectionSelected = -1;
        sections = new List<Section>();
        levels = new List<Level>();
        unlocked = new List<bool>();
        won = new List<bool>();
        Vector2 averageExtent = Vector2.zero;
        Vector2Int maxExtent = Vector2Int.zero;
        int wipLevels = 0;
        levelPreviews = new List<LevelPreview>();
        int highestTurnLimit = 0;
        foreach(TextAsset levelAsset in  database.levelTexts){
            Level l;
            if(Services.GameController.useJson){
                Debug.Log(levelAsset.text);
                LevelPreview lp = new LevelPreview();
                lp.data = JsonUtility.FromJson<LevelJson>(levelAsset.text);
                Vector2Int gridPosition = new Vector2Int(lp.data.map_pos.x,lp.data.map_pos.y);
                lp.gameObject = GameObject.Instantiate(Services.GameController.levelPreviewPrefab,(Vector2)gridPosition,Quaternion.identity,Services.Grid.transform);
                lp.name = lp.data.name;
                l = new Level(lp.data);
            }else{
                l = new Level(levelAsset.text,levelAsset.name);
            }
            var turnLimit = l.turnLimit+l.actionPoints.Count;
            if(turnLimit>highestTurnLimit){
                highestTurnLimit = turnLimit;
            }
            l.index = levels.Count;
            unlocked.Add(Services.GameController.editMode);
            won.Add(false);
            levels.Add(l);
            v2Level.Add(l.gridPosition,l);
            name2Level.Add(l.name,l);
            l.InstantShrink();
            averageExtent+=l.extent;
            if(l.extent.x > maxExtent.x){
                maxExtent.x = l.extent.x;
            }
            if(l.extent.y > maxExtent.y){
                maxExtent.y = l.extent.y;
            }
            //section stuff
            if(l.section < 0){
                wipLevels++;
                continue;
            }
            bool newSection = true;
            foreach(Section section in sections){
                if(section.index == l.section){
                    section.AddLevel(l);
                    newSection = false;
                    break;
                }
            }
            if(newSection){
                sections.Add(new Section(l));
            }
        }
        Debug.Log("there are "+(database.levelTexts.Count-wipLevels)+" levels");
        Debug.Log("max extent is "+maxExtent);
        averageExtent/=levels.Count;
        Debug.Log("average extent is "+averageExtent);
        Debug.Log("highest turn limit is: "+highestTurnLimit);
        cursorPosition = startPosition;
        cursor.transform.position = (Vector2)startPosition;
        unlocked[v2Level[startPosition].index] = true;
        sections[v2Level[startPosition].section].visible = true;
        sections.Sort(Section.SortSection);
        foreach(Section section in sections){
            section.ThinkAboutBorders();
        }
        SaveSystem.Load();
        /*TraverseLevelsToFindConnectionsToOrigin(startPosition);
        foreach(Level level in levels){
            level.ThinkAboutBorders();
        }*/
        foreach(Level l in levels){
            l.ConvertLevelToJson();
        }
    }
    /*void TraverseLevelsToFindConnectionsToOrigin(Vector2Int pos){
        if(v2Level.ContainsKey(pos) == false){return;}
        for(var i = 0; i < 4; i++){
            Vector2Int newPos = pos+Services.Grid.directions[i];
            if(v2Level.ContainsKey(newPos)){
                if(v2Level[newPos].careAboutBorders){continue;}
                TraverseLevelsToFindConnectionsToOrigin(newPos);
            }
        }
    }*/
    public void MoveCursor(int direction){
        Vector2Int newPosition = cursorPosition+Services.Grid.directions[direction];
        if(!Services.GameController.editMode){
            /*if(v2Level.ContainsKey(newPosition) == false){
                return;
            }else{
                if(unlocked[v2Level[newPosition].index] ==false){
                    return;
                }
            }*/
        }
        cursorPosition = newPosition;
    }
    public void WinLevel(Level le){
        won[le.index] = true;
        //le.exit.TurnOn();
        numWon++;
        for(var i = 0; i < 4; i++){
            Vector2Int checkPos = le.gridPosition+Services.Grid.directions[i];
            if(v2Level.ContainsKey(checkPos)){
                Level levelToUnlock = v2Level[checkPos];
                bool actuallyUnlock = true;
                if(levelToUnlock.section != le.section){
                    actuallyUnlock = false;
                    if(le.sectionExit == i){
                        actuallyUnlock = true;
                    }
                }
                if(actuallyUnlock){
                    unlocked[levelToUnlock.index] = true;
                    sections[levelToUnlock.section].visible = true;
                    numUnlocked++;
                }
            }
        }
        SaveSystem.Save();
    }
    public bool CursorOnLevel(){
        if(v2Level.ContainsKey(cursorPosition) == false){
            return false;
        }
        return true;

    }
    public void HandleTutorialText(){
        tutorialTextDisplay.text = "";
        if(Services.Grid.level.spoken == ""){return;}
        Dictionary<string,string> emojis = new Dictionary<string, string>(){
            {"~n","\n"},
            {"~a","<sprite=10>"},
            {"~t","<voffset=-0.1em><size=6><sprite=11></size></voffset>"},
            {"~p","<voffset=-0.1em><size=6><sprite=12></size></voffset>"},
            {"~w","<sprite=4>"},
            {"~u","<sprite=0>"},
            {"~r","<sprite=1>"},
            {"~d","<sprite=2>"},
            {"~l","<sprite=3>"}
        };
        string displayText  = Services.Grid.level.spoken;
        foreach(string emojiThing in emojis.Keys){
            displayText = displayText.Replace(emojiThing,emojis[emojiThing]);
        }
        if(textEditMode){
            displayText+="|";
        }
        /*foreach(TutorialString ts in tutorialStrings){
            if(ts.gridPosition == Services.Grid.level.gridPosition){
                displayText = ts.text;
            }
        }*/
        if(tutorialIndex >= displayText.Length-1){
            tutorialIndex = displayText.Length-1;
        }
        tutorialIndex = displayText.Length-1;
        if(tutorialIndex < displayText.Length-1 && Time.time > nextLetterTime){
            tutorialIndex++;
            nextLetterTime = Time.time+(0.33f*0.5f);
            if(Input.anyKey){
                nextLetterTime = Time.time;
            }
            if(displayText[tutorialIndex] == '<'){
                while(displayText[tutorialIndex] != '>'){
                    tutorialIndex++;
                    if(displayText[tutorialIndex] == '>' && tutorialIndex < displayText.Length-1 && displayText[tutorialIndex+1] == '<'){
                        tutorialIndex++;
                    }
                }
            }
        }
        
        string[] displaySplit = displayText.Split('\n');
        int lengthOfPreviousSections = 0;
        tutorialTextDisplay.text+="<line-height=100%>";
        for(int i = 0; i < displaySplit.Length;i++){
            if(i != 0){
                tutorialTextDisplay.text+='\n';
            }
            if(lengthOfPreviousSections > tutorialIndex){
                continue;
            };
            tutorialTextDisplay.text += "["+displaySplit[i].Substring(0,Mathf.Clamp(tutorialIndex+1-lengthOfPreviousSections,0,displaySplit[i].Length)).Trim()+"]";
            if(i < displaySplit.Length-1){
                //tutorialTextDisplay.text+="<sprite=9>";
            }
            lengthOfPreviousSections+=displaySplit[i].Length+1;
        }
        
        //tutorialTextDisplay.text = "["+displayText.Substring(0,Mathf.Clamp(tutorialIndex,0,displayText.Length))+"]";
        float y = 0.75f+(Services.Grid.level.extent.y*0.5f);
        tutorialTextDisplay.transform.localPosition = new Vector3(tutorialTextDisplay.transform.localPosition.x,y,tutorialTextDisplay.transform.localPosition.z);
    }
    
    // Update is called once per frame
    public void Draw()
    {
        tutorialIndex = -1;
        tutorialTextDisplay.text = "";
        foreach(Level level in levels){
            level.DrawLevel();
        }
        foreach(Section section in sections){
            section.Draw();
        }
        foreach(LineRenderer line in cursorLines){
            line.startColor = Services.Visuals.playerColor;
            line.endColor = line.startColor;
        }
        cursor.transform.position = Services.Visuals.LerpVector(cursor.transform.position,cursorPosition);
        cursor.gameObject.SetActive(Services.GameController.state == GameState.LevelSelect);
        
    }
    public string PrintLevelMoves(){
        //return "";
        bool keepEndButton = false;//whether or not it should symbolize the button to restart the loop
        string s = "<mspace=0.7em>";
        for(int i = 0; i < Services.GameController.players.Count;i++){
            Player p = Services.GameController.players[i];
            if(p == Services.GameController.currentPlayer){
                s+="<sprite=6>";
            }else{
                s+=(p.index+1).ToString();
            }
            for(var j = 0; j < p.moves.Count;j++){
                /*if(j < Services.GameController.currentTurn || (j == Services.GameController.currentTurn && p.index <= Services.GameController.currentPlayerIndex)){
                    s+="<u>";
                }*/
                bool currentMove = false;
                if(j == Services.GameController.currentTurn && p.index == Services.GameController.currentPlayerIndex){
                    currentMove = true;
                }
                if(currentMove){
                    s+="<u>";
                }
                s+="<sprite="+p.moves[j]+">";
                if(currentMove){
                    s+="</u>";
                }
            }
            if(i != Services.GameController.players.Count-1){
                if(keepEndButton){
                    s+="<sprite=6>\n";
                }else{
                     s+="\n";
                }
               
                /*if(p.moves.Count == Services.GameController.currentTurn && p.index == Services.GameController.currentPlayerIndex){
                     s+="<u>";
                }
                s+="<sprite=4>\n";
                if(p.moves.Count == Services.GameController.currentTurn && p.index == Services.GameController.currentPlayerIndex){
                     s+="</u>";
                }*/
            }
            if(p == Services.GameController.currentPlayer && p.isMoving){
                levelMovesCursorCountdown = 0.75f*0.5f;
                levelMovesCursorOn = true;
            }
            //if(won[Services.GameController.currentLevel]){continue;}
            if(p == Services.GameController.currentPlayer && p.isMoving == false && Services.GameController.winTime < 0f){
                levelMovesCursorCountdown-=Time.deltaTime;
                if(levelMovesCursorCountdown <= 0f){
                    levelMovesCursorOn = !levelMovesCursorOn;
                    levelMovesCursorCountdown = 0.75f;
                }
                if(p.index == Services.GameController.currentPlayerIndex && Services.GameController.doClonesMove == false){
                    s+="<u>";
                }
                if(levelMovesCursorOn){
                    if(Services.Grid.level.exits.ContainsKey(p.position) && Services.GameController.currentTurn < Services.GameController.turnLimit){
                        s+="<sprite=5>";
                    }else{
                        if(Services.GameController.currentTurn == Services.GameController.turnLimit){
                            bool canFinishLoop = false;
                            foreach(ActionPoint ap in Services.Grid.actionPoints.Values){
                                if(ap.held){
                                    canFinishLoop = true;
                                    break;
                                }
                            }
                            s+=(canFinishLoop ? "<sprite=5>" : "<sprite=9>");
                        }else{
                            s+="<sprite=9>";
                        }
                    }
                }else{
                    s+="<sprite=9>";
                }
                
                
                
                if(p.index == Services.GameController.currentPlayerIndex && Services.GameController.doClonesMove == false){
                    s+="</u>";
                }
            }
        }
        return s;
    }
    #if UNITY_EDITOR
    public void CreateLevel(Vector2Int pos){
        //create a level at the cursor
        string name = RandomName();
        int counter = 0;
        while(name2Level.ContainsKey(name)){
            name = RandomName();
            counter++;
            if(counter > 10){
                Debug.Log("Something wrong with generating names");
                return;
            }
        }
        string newLevelString = name+'\n';
        newLevelString+=pos.x.ToString()+","+pos.y.ToString()+",-1"+'\n';
        newLevelString+="1\n";
        newLevelString+="@,x";
        Level l = new Level(newLevelString,name);
        levels.Add(l);
        v2Level.Add(l.gridPosition,l);
        name2Level.Add(l.name,l);
        newLevelString = newLevelString.Trim();
        string path = Application.dataPath+"/Levels/"+name+".txt";
        StreamWriter writer = new StreamWriter(path, false);
        writer.NewLine = "";
        writer.WriteLine(newLevelString);
        
        writer.Close();
        AssetDatabase.Refresh();
    }
    public void DeleteLevel(Vector2Int pos){
        string name = v2Level[pos].name;
        string path = Application.dataPath+"/Levels/"+name+".txt";
        System.IO.File.Delete(path);
        AssetDatabase.Refresh();
        v2Level[pos].Destroy();
        levels.Remove(v2Level[pos]);
        v2Level.Remove(pos);
        name2Level.Remove(name);
    }
    public string RandomName(){
        string s = "_";
        for(var i = 0; i < 5;i++){
            s+= (char)Random.Range(97,123);
        }
        return s;
    }
    void FindAllLevelAssets(){
        database.levelTexts.Clear();
        List<DirectoryInfo> directories = new List<DirectoryInfo>();
        string folder = "Levels";
        if(Services.GameController.useJson){
            folder = "_Levels";
        }
        directories.Add(new DirectoryInfo(Application.dataPath+"/"+folder));
        DirectoryInfo[] subDirectories = directories[0].GetDirectories();
        foreach(DirectoryInfo d in subDirectories){
            //Debug.Log("DIRECTORIES");
            directories.Add(d);
        }
        //TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(info[0].ToString());
        //Debug.Log(asset.name);
        string mainPath = "Assets/"+folder+"/";
        string extension = (Services.GameController.useJson ? ".json" : ".txt");
        foreach(DirectoryInfo d in directories){
            string directoryPath = "";
            if(d.Name.Equals(folder)){
            }else{
                directoryPath = d.Name+"/";
            }
            FileInfo[] info = d.GetFiles("*.*");
            foreach(FileInfo f in info){
                if(f.Extension == extension){
                    string path = mainPath+directoryPath+f.Name;
                    //Debug.Log(f.Name);
                    TextAsset asset = AssetDatabase.LoadAssetAtPath(path,typeof(TextAsset)) as TextAsset;
                    if(asset == null){continue;}
                    database.levelTexts.Add(asset);
                    //Debug.Log("added");
                }
            }
        }
    }
    //editing stuff in the level select
    public void RemoveSection(Section section){
        int index = section.index;
        //so basically we need to go to every section after this one and lower their numbers down one including the levels
        for(var i = index; i < sections.Count;i++){
            sections[i].Lower();
        }
        sections.RemoveAt(index);
    }
    public void LevelSelectEditorControls(){
        if(sectionSelected > -1){
            if(Input.GetMouseButton(0) == false && Input.GetMouseButton(1) == false){
                Debug.Log("DONE");
                sectionSelected = -1;
            }
        }
        if(Input.GetKey(KeyCode.LeftControl)){
            if(Input.GetKeyDown(KeyCode.Q)){
                foreach(Level l in levels){
                    if(l.changed){
                        SaveLevelChanges(l);
                    }
                }
            }
            //section controls!
            Vector2Int mouseTilePos = GetMouseTile();
            if(sectionSelected > -1){
                //holding something down
                if(Input.GetMouseButton(0)){
                    //dragging!
                    if(v2Level.ContainsKey(mouseTilePos) == false){return;}
                    Level l = v2Level[mouseTilePos];
                    Debug.Log("hovering over "+l.section);
                    if(l.section != sectionSelected){
                        if(l.section > -1){
                            sections[l.section].RemoveLevel(l);
                        }
                        sections[sectionSelected].AddLevel(l);
                    }
                }
                if(Input.GetMouseButton(1)){
                    //erasing!
                    if(v2Level.ContainsKey(mouseTilePos) == false){return;}
                    Level l = v2Level[mouseTilePos];
                    if(l.section == -1){return;}
                    l.sectionExit = -1;
                    sections[l.section].RemoveLevel(l);
                }
            }else{
                int clickedWall = -1;//did not click wall
                clickedWall = CheckClickWall(mouseTilePos);
                if(Input.GetMouseButtonDown(0)){
                    if(v2Level.ContainsKey(mouseTilePos) == false){return;}
                    Level l = v2Level[mouseTilePos];
                    if(clickedWall == -1){
                        if(l.section < 0){
                            //make a new section
                            l.section = sections.Count;
                            sections.Add(new Section(l));
                            sections[l.section].ThinkAboutBorders();
                        }
                        sectionSelected = l.section;
                    }else{
                        //try to make a section exit
                        Vector2Int checkTilePos = mouseTilePos+Services.Grid.directions[clickedWall];
                        if(v2Level.ContainsKey(checkTilePos) && v2Level[checkTilePos].section != l.section){
                            //there is a level that way and its a different section
                            if(l.sectionExit == -1){
                                l.changed = true;
                                v2Level[checkTilePos].changed = true;
                                l.sectionExit = clickedWall;
                                v2Level[checkTilePos].sectionExit = Services.Grid.opposite(clickedWall);
                                sections[l.section].ReThinkBorders();
                                sections[v2Level[checkTilePos].section].ReThinkBorders();
                            }
                        }
                    }
                    return;
                }
                if(Input.GetMouseButtonDown(1)){
                    if(v2Level.ContainsKey(mouseTilePos) == false){return;}
                    Level l = v2Level[mouseTilePos];
                    if(clickedWall == -1){
                        if(l.section > -1){
                            sectionSelected = l.section;
                            sections[l.section].RemoveLevel(l);
                            
                        }
                    }else{
                        //try to remove a section exit
                        if(l.sectionExit != clickedWall){return;}
                        Vector2Int checkTilePos = mouseTilePos+Services.Grid.directions[clickedWall];
                        if(v2Level.ContainsKey(checkTilePos)){
                            //there is a level that way
                            l.sectionExit = -1;
                            
                            v2Level[checkTilePos].sectionExit = -1;
                            l.changed = true;
                            v2Level[checkTilePos].changed = true;
                            sections[l.section].ReThinkBorders();
                            sections[v2Level[checkTilePos].section].ReThinkBorders();
                        }
                    }
                    return;
                }
            }  
        }else{
            sectionSelected = -1;
        }
        if(holdingLevelDown){
            if(Time.time > timeHoldStarted+0.33f){
                //you are carrying this thing
                if(rightHold){
                    if(v2Level.ContainsKey(levelHolding)){
                        DeleteLevel(levelHolding);
                    }
                    rightHold = false;
                    holdingLevelDown = false;
                }else{
                    if(v2Level.ContainsKey(levelHolding)){
                        v2Level[levelHolding].beingCarried = true;
                        levelNameDisplay.text = v2Level[levelHolding].name;
                        cursorPosition = GetMouseTile();
                    }else{
                        CreateLevel(levelHolding);
                        holdingLevelDown = false;
                    }
                }
                
            }
            if(rightHold && Input.GetMouseButtonUp(1)){
                rightHold = false;
                holdingLevelDown = false;
            }
            if(rightHold == false && Input.GetMouseButtonUp(0)){
                if(Time.time > timeHoldStarted+0.33f){
                    //you're already carrying it, drop it! (swap if something is laready there)
                    Vector2Int mouseTilePos = GetMouseTile();
                    Level carriedLevel = v2Level[levelHolding];
                    int oldSection = carriedLevel.section;
                    int newSection = -1;
                    carriedLevel.beingCarried = false;
                    if(mouseTilePos == levelHolding){
                        holdingLevelDown = false;
                        return;
                    }
                    if(carriedLevel.section != -1){
                        sections[carriedLevel.section].RemoveLevel(carriedLevel);
                    }
                    
                    if(v2Level.ContainsKey(mouseTilePos)){
                        //level at new position's new position is the swapped one's
                        v2Level[mouseTilePos].gridPosition = levelHolding;
                        if(v2Level[mouseTilePos].section != -1){
                            newSection = v2Level[mouseTilePos].section;
                            sections[v2Level[mouseTilePos].section].RemoveLevel(v2Level[mouseTilePos]);
                            int carriedExit = carriedLevel.sectionExit;
                            carriedLevel.sectionExit = v2Level[mouseTilePos].sectionExit;
                            v2Level[mouseTilePos].sectionExit = carriedExit;
                        }
                        if(oldSection != -1){
                            sections[oldSection].AddLevel(v2Level[mouseTilePos]);
                        }
                        
                        //old one's spot in dictionary is now the new one
                        v2Level[levelHolding] = v2Level[mouseTilePos];
                        SaveLevelChanges(v2Level[levelHolding]);
                    }else{
                        v2Level.Remove(levelHolding);
                    }
                    carriedLevel.gridPosition = mouseTilePos;
                    if(newSection != -1){
                        sections[newSection].AddLevel(carriedLevel);
                        sections[newSection].ReThinkBorders();
                    }
                    if(oldSection != -1){
                        sections[oldSection].ReThinkBorders();
                    }
                    
                    v2Level[mouseTilePos] = carriedLevel;
                    SaveLevelChanges(v2Level[mouseTilePos]);
                    holdingLevelDown = false;
                }else{
                    //enter
                    if(v2Level.ContainsKey(levelHolding)){
                        cursorPosition = levelHolding;
                        Services.Grid.LoadLevel(v2Level[levelHolding]);
                    }
                }
                holdingLevelDown = false;

            }
        }else{
            Vector2Int mouseTilePos = GetMouseTile();
            cursorPosition = mouseTilePos;
            if(v2Level.ContainsKey(cursorPosition)){
                levelNameDisplay.text = v2Level[cursorPosition].name;
            }else{
                levelNameDisplay.text = "";
            }
            if(Input.GetMouseButtonDown(0)){
                holdingLevelDown = true;
                
                levelHolding = mouseTilePos;
                timeHoldStarted = Time.time;
                rightHold = false;
            }
            if(Input.GetMouseButtonDown(1)){
                holdingLevelDown = true;
                levelHolding = mouseTilePos;
                timeHoldStarted = Time.time;
                rightHold = true;
            }
        }
        
    }
    //this is for when you're in a level!
    public void LevelEditorControls(){
        if(Services.GameController.editMode == false){return;}
        if(Input.GetKey(KeyCode.Alpha1) && Input.GetKeyDown(KeyCode.Alpha2)){
            textEditMode = !textEditMode;
        }
        if(Input.GetKeyDown(KeyCode.Tab)){
            nameEditMode = !nameEditMode;
        }
        Level l = Services.Grid.level;
        if(textEditMode){
            foreach(char c in Input.inputString){
                if(c == '1' || c=='2'){continue;}
                l.changed = true;
                if(c=='\b'){
                    if(l.spoken.Length > 0){
                        l.spoken = l.spoken.Substring(0,l.spoken.Length-1);
                        if(l.spoken.Length > 0 && l.spoken[l.spoken.Length-1] == '~'){
                            l.spoken = l.spoken.Substring(0,l.spoken.Length-1);
                        }
                    }
                }else if(c=='\n' || c=='\r'){
                    l.spoken+="~n";
                }
                else{
                    l.spoken+=c;
                }
        }
        }
        if(nameEditMode){
            foreach(char c in Input.inputString){
                if(c == '1' || c=='2'){continue;}
                l.changed = true;
                if(c=='\b'){
                    if(l.name.Length > 0){
                        l.name = l.name.Substring(0,l.name.Length-1);
                        /*if(l.spoken.Length > 0 && l.spoken[l.spoken.Length-1] == '~'){
                            l.spoken = l.spoken.Substring(0,l.spoken.Length-1);
                        }*/
                    }
                }
                else{
                    l.name+=c;
                }
        }
        }
        Vector2 floatMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        
        levelNameDisplay.text = "";
        if(l.changed){
            levelNameDisplay.text+="**";
        }
        levelNameDisplay.text += l.name;
        if(nameEditMode){
            levelNameDisplay.text+="|";
        }
        if(l.changed){
            levelNameDisplay.text+="**";
        }
        levelNameDisplay.text+='\n'+PrintLevelMoves();
        
        if(Services.GameController.turns.Count > 0){return;}
        if(Input.GetKeyDown(KeyCode.Q)){
            SaveLevelChanges(l);
        }
        //actual editing happens under here
        if(Input.GetKeyDown(KeyCode.LeftBracket)){
            if(l.turnLimit > 1){
                l.LowerTurnLimit();
            }
        }
        if(Input.GetKeyDown(KeyCode.RightBracket)){
            l.RaiseTurnLimit();
        }
        if(movingExit){
            exitMoving.FollowMouse(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
        if(movingStartPosition){
            l.players[0].FollowMouse(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
        if(Input.GetMouseButtonDown(0)){
            Vector2Int mouseTilePos = GetMouseTile();
            if(movingExit){
                if(l.tiles.ContainsKey(mouseTilePos) && l.actionPoints.ContainsKey(mouseTilePos) == false && l.startPosition != mouseTilePos){
                    if(newExit){
                        exitMoving.SetPosition(mouseTilePos);
                        l.AddExit(exitMoving);
                    }else{
                        l.MoveExit(exitMoving,mouseTilePos);
                    }
                    
                    newExit = false;
                    movingExit = false;
                }
                return;
            }
            if(movingStartPosition){
                if(l.tiles.ContainsKey(mouseTilePos) && l.actionPoints.ContainsKey(mouseTilePos) == false && l.exits.ContainsKey(mouseTilePos) == false){
                    l.MoveStart(mouseTilePos);
                    movingStartPosition = false;
                }
                return;
            }
            int clickedWall = -1;//did not click wall
            clickedWall = CheckClickWall(mouseTilePos);
            if(clickedWall == -1 || l.tiles.ContainsKey(mouseTilePos) == false){
                if(l.tiles.ContainsKey(mouseTilePos)){
                    if(mouseTilePos == l.startPosition){
                        movingStartPosition = true;
                        return;
                    }
                    if(l.exits.ContainsKey(mouseTilePos)){
                        movingExit = true;
                        if(Input.GetKey(KeyCode.LeftShift)){
                            exitMoving = new Exit(new Vector2Int(-1,-1),l.gameObject.transform);
                            newExit = true;
                        }else{
                            newExit = false;
                            exitMoving = l.exits[mouseTilePos];
                        }
                        return;
                    }
                    if(l.actionPoints.ContainsKey(mouseTilePos) == false){
                        if(mouseTilePos == l.startPosition || l.exits.ContainsKey(mouseTilePos)){
                            return;
                        }
                        if(Input.GetKey(KeyCode.LeftControl)){
                            l.AddSpawnPortal(mouseTilePos);
                        }else if(Input.GetKey(KeyCode.LeftShift)){
                            l.AddSpores(mouseTilePos);
                        }else{
                            l.AddActionPoint(mouseTilePos);
                        }
                    }else{
                        if(Input.GetKey(KeyCode.LeftControl)){
                            l.AddToActionPointTimer(mouseTilePos);
                        }else{
                            l.AddToActionPoint(mouseTilePos);
                        }
                        
                    }
                }else{
                    l.AddTile(mouseTilePos);
                }
            }else{
                if(l.tiles.ContainsKey(mouseTilePos)){
                    if(Input.GetKey(KeyCode.LeftControl)){
                        l.AddWallSpike(mouseTilePos,clickedWall);
                    }else{
                        l.AddWall(mouseTilePos,clickedWall);
                    }
                    
                }
            }
            
        }
        if(Input.GetMouseButtonDown(1)){
            if(movingExit){
                movingExit = false;
                exitMoving.SetPosition(new Vector2Int(-1,-1));
                return;
            }
            if(movingStartPosition){
                movingStartPosition = false;
                l.players[0].SetPosition(l.startPosition);
                return;
            }
            Vector2Int mouseTilePos = GetMouseTile();
            int clickedWall = -1;//did not click wall
            clickedWall = CheckClickWall(mouseTilePos);
            if(clickedWall == -1){
                if(l.tiles.ContainsKey(mouseTilePos)){
                    if(l.actionPoints.ContainsKey(mouseTilePos)){
                        if(Input.GetKey(KeyCode.LeftControl)){
                            l.RemoveActionPointTimer(mouseTilePos);
                        }else{
                            l.RemoveActionPoint(mouseTilePos);
                        }
                        
                        return;
                    }
                    if(mouseTilePos == l.startPosition){
                        return;
                    }
                    if(l.exits.ContainsKey(mouseTilePos)){
                        if(l.exits.Count > 1){
                            l.RemoveExit(mouseTilePos);
                        }
                        return;
                    }
                    if(l.tiles[mouseTilePos].hasSpawnPortal){
                        if(Input.GetKey(KeyCode.LeftControl)){
                            l.RemoveSpawnPortal(mouseTilePos);
                            return;
                        }
                    }
                    if(l.tiles[mouseTilePos].hasSpores){
                        if(Input.GetKey(KeyCode.LeftShift)){
                            l.RemoveSpores(mouseTilePos);
                            return;
                        }
                    }
                    l.RemoveTile(mouseTilePos);
                }
            }else{
                if(l.tiles.ContainsKey(mouseTilePos)){
                    if(Input.GetKey(KeyCode.LeftControl)){
                        l.RemoveWallSpike(mouseTilePos,clickedWall);
                    }else{
                        l.RemoveWall(mouseTilePos,clickedWall);
                    }
                }
            }
            
        }

    }
    Vector2Int GetMouseTile(){
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if(Services.GameController.state == GameState.InLevel){
            mousePos-=(Vector2)Services.Grid.level.gameObject.transform.position;
        }
        
        return new Vector2Int(Mathf.FloorToInt(mousePos.x),Mathf.FloorToInt(mousePos.y));
    }
    int CheckClickWall(Vector2Int mouseTilePos){
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if(Services.GameController.state == GameState.InLevel){
            mousePos-=(Vector2)Services.Grid.level.gameObject.transform.position;
        }
        float closeness = 0.1f;
        mousePos-=mouseTilePos;//now its just the interior values
        if(mousePos.x < closeness){
            return 3;
        }
        if(1f-mousePos.x < closeness){
            return 1;
        }
        if(mousePos.y < closeness){
            return 2;
        }
        if(1f-mousePos.y < closeness){
            return 0;
        }
        return -1;
    }
    public void SaveLevelChanges(Level l){
        l.changed = false;
        string levelString = l.name+'\n';
        levelString+=l.gridPosition.x.ToString()+","+l.gridPosition.y.ToString()+","+l.section.ToString();
        if(l.sectionExit != -1){
            levelString+=","+l.sectionExit.ToString();
        }
        levelString+='\n';
        levelString+=l.turnLimit.ToString()+','+l.spoken+"\n";
        levelString+=l.ConvertLevelToString();
        l.ConvertLevelToJson();

        levelString = levelString.Trim();
        l.levelData = levelString;
        string path = Application.dataPath+"/Levels/"+l.internal_name+".txt";
        if(!File.Exists(path)){
            System.IO.File.WriteAllText(path,levelString);
        }else{
            StreamWriter writer = new StreamWriter(path, false);
            writer.NewLine = "";
            writer.WriteLine(levelString);
            writer.Close();
        }
        
        
        
        AssetDatabase.Refresh();
    }
    public void ResetLevelChanges(Level l){
        string levelData = l.levelData;
        l.Destroy();
        levels.Remove(l);
        v2Level.Remove(l.gridPosition);
        Level recreatedLevel = new Level(levelData,l.internal_name);
        recreatedLevel.InstantShrink();
        levels.Add(recreatedLevel);
        v2Level.Add(recreatedLevel.gridPosition,recreatedLevel);
    }
    #endif
}
[System.Serializable]
public class LevelData
{
    public string name = "";
    public int x;
    public int y;
    public int section = -1;
    public int sectionExit = -1;
    public int turnLimit = 1;
    public string text;
    public List<List<string>> grid;
}