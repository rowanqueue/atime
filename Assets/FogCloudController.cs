using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogCloudController : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private SpriteRenderer fogSprite;
    [SerializeField] private float speedMin;
    [SerializeField] private float speedMax;
    [SerializeField] private Color frontalFogColor;
    [SerializeField] private Color lightFogColor;
    [SerializeField] private Color midFogColor;
    [SerializeField] private Color heavyFogColor;

    // Not parameters just used for debugging
    [Header("Info")]
    [SerializeField] private FogType fogType;
    [SerializeField] private int screenDepth;
    [SerializeField] private int levelDepth;
    [SerializeField] private float screenStart;
    [SerializeField] private float screenEnd;
    [SerializeField] private float speed;

    private enum FogType {frontal, light, mid, heavy };

    public void Initialize(int screenDepth, float screenStartXPos, float screenEndXPos, int levelDepth = 0)
    {
        // the additional 0.5 puts the fog in the middle of the tile instead of the bottom left edge 
        transform.position += Vector3.right * 0.5f;
        this.screenDepth = screenDepth;
        this.levelDepth = levelDepth;
        screenStart = screenStartXPos;
        screenEnd = screenEndXPos;
        fogSprite.sortingOrder = screenDepth;
        speed = 0;
        if (levelDepth == -1)
        {
            fogType = FogType.frontal;
        }
        else if (levelDepth < 1)
        {
            fogType = FogType.light;
        }
        else if (levelDepth < 2)
        {
            fogType = FogType.mid;
        }
        else
        {
            fogType = FogType.heavy;
        }
        SetFogVisuals();
    }

    private void SetFogVisuals()
    {
        switch (fogType)
        {
            case FogType.frontal:
                fogSprite.color = frontalFogColor;
                speed = Random.Range(speedMin / 1000, speedMax / 1000);
                break;
            case FogType.light:
                fogSprite.color = lightFogColor;
                break;
            case FogType.mid:
                fogSprite.color = midFogColor;
                break;
            case FogType.heavy:
                fogSprite.color = heavyFogColor;
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        transform.position += Vector3.right * speed;
        if (transform.position.x > screenEnd + fogSprite.sprite.bounds.extents.x)
        {
            transform.position = new Vector3(screenStart - fogSprite.sprite.bounds.extents.x, transform.position.y, transform.position.z);
        }
    }
}
