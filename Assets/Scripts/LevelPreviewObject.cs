using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelPreviewObject: MonoBehaviour
{
    public LevelSelect.LevelPreview lp;
    public SpriteRenderer spiral;
    bool on = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        on = true;
        on = lp.unlocked;
        if(on){
            spiral.transform.eulerAngles += new Vector3(0,0,0.5f*(Time.deltaTime/0.016f));
            spiral.color = Color.white;
        }else{
            spiral.color = Services.Visuals.tileColor;
        }
        spiral.transform.localScale = Services.Visuals.LerpVector(spiral.transform.localScale,Vector3.one*(on ? 0.75f:0.4f));
        
    }
}
