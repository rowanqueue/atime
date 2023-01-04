using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Section
{
    public int index;
    public bool visible = true;
    public List<Level> levels;
    public List<LineRenderer> edges;
    public Dictionary<Vector2Int,int> sectionExits;//points to a wall where you can slip to another section for unlocking
    public Section(Level level){
        levels = new List<Level>();
        levels.Add(level);
        index = level.section;
        edges = new List<LineRenderer>();
        sectionExits = new Dictionary<Vector2Int, int>();
    }
    public void Draw(){
        foreach(LineRenderer edge in edges){
            edge.enabled = visible;
        }
    }
    public void ThinkAboutBorders(){
        foreach(Level l in levels){
            l.ThinkAboutBorders();
        }
    }
    
    public void AddLevel(Level l){
        
        l.section = index;
        levels.Add(l);
        if(l.sectionExit != -1){
            sectionExits.Add(l.gridPosition,l.sectionExit);
        }
        #if UNITY_EDITOR
        if(edges.Count > 0){
            l.changed = true;
            ReThinkBorders();
        }
        #endif

    }
    //this is only for level editing purposes
    #if UNITY_EDITOR
    public void Lower(){
        index--;
        foreach(Level l in levels){
            l.section = index;
        }
    }
    public void RemoveLevel(Level l){
        l.changed = true;
        if(sectionExits.ContainsKey(l.gridPosition)){
            sectionExits.Remove(l.gridPosition);
        }
        l.section = -1;
        levels.Remove(l);
        //check here to see if we need to get rid of this whole section
        if(levels.Count == 0){
            //you've destroyed this section lol
            foreach(LineRenderer line in edges){
                GameObject.Destroy(line.gameObject);
            }
            Services.LevelSelect.RemoveSection(this);
            return;
        }
        ReThinkBorders();
    }
    public void ReThinkBorders(){
        foreach(LineRenderer line in edges){
            GameObject.Destroy(line.gameObject);
        }
        edges.Clear();
        ThinkAboutBorders();
    }
    #endif
    public static int SortSection(Section a, Section b){
        return a.index.CompareTo(b.index);
    }
}

