using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public static class SaveSystem
{
    //todo: redo savesystem
    public static void Save(){
        if(Services.GameController.editMode){return;}
        Save save = new Save();
        string saveJson = JsonUtility.ToJson(save);
        PlayerPrefs.SetString("save",saveJson);
    }
    public static void Load(){
        /*if(PlayerPrefs.HasKey("save") == false){return;}
        string saveJson = PlayerPrefs.GetString("save");
        Save save = JsonUtility.FromJson<Save>(saveJson);
        int numWon = 0;
        for(int i = 0; i < save.levelNames.Count;i++){
            if(Services.LevelSelect.name2Level.ContainsKey(save.levelNames[i])){
                Services.LevelSelect.unlocked[Services.LevelSelect.name2Level[save.levelNames[i]].index] = true;
                if(save.levelWon[i] > 0){
                    numWon++;
                    Services.LevelSelect.won[Services.LevelSelect.name2Level[save.levelNames[i]].index] = true;
                }
            }
        }
        Services.LevelSelect.cursorPosition = save.cursorPosition;
        Services.LevelSelect.cursor.gameObject.transform.position = (Vector2)save.cursorPosition;
        Services.LevelSelect.numWon = numWon;
        Services.LevelSelect.numUnlocked = save.levelNames.Count;
        Debug.Log("loaded save with "+numWon+"/"+save.levelNames.Count);*/
    }
    #if UNITY_EDITOR
    [MenuItem("Save/Clear")]
    #endif
    public static void Clear(){
        PlayerPrefs.DeleteKey("save");
    }
    public static void ConvertSaveToLetters(){
        /*string letters = "";
        int index = 0;
        List<bool> collected = new List<bool>();
        while(index < Services.LevelSelect.won.Count){
            collected.Add(Services.LevelSelect.won[index]);
            index++;
            if(collected.Count >= 5){
                string binary = "";
                foreach(bool b in collected){
                    binary+= (b ? '1' : '0');
                }
                Debug.Log(binary+" - "+System.Convert.ToInt32(binary,2));
                collected.Clear();
                letters += (char)Services.GameController.saveCharacters[System.Convert.ToInt32(binary,2)];
            }
        }
        string bonary = "";
        foreach(bool b in collected){
            bonary+=(b ? '1' : '0');
        }
        collected.Clear();
        letters += (char)Services.GameController.saveCharacters[System.Convert.ToInt32(bonary,2)];
        Debug.Log(letters);*/

    }
}
[System.Serializable]
public class Save
{
    public Vector2Int cursorPosition;
    public List<string> levelNames = new List<string>();
    public List<int> levelWon = new List<int>();//0: false, 1: true
    public Save(){
        cursorPosition = Services.LevelSelect.cursorPosition;
        /*for(int i = 0; i < Services.LevelSelect.levels.Count;i++){
            if(Services.LevelSelect.unlocked[i]){

                int state = 1;
                if(Services.LevelSelect.won[i]){
                    state = 2;
                }
                levelNames.Add(Services.LevelSelect.levels[i].internal_name);
                levelWon.Add(Services.LevelSelect.won[i] ? 1 : 0);
            }
        }*/
    }
}
