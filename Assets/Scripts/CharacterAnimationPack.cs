using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterAnimationPack")]
public class CharacterAnimationPack : ScriptableObject
{
    public List<Sprite> introAnimation;
    public List<Sprite> walkAnimationRight;
    public List<Sprite> walkAnimationUp;
    public List<Sprite> walkAnimationDown;
    public float walkAnimSpeed = 5f;
    public List<Sprite> failWalkAnimationUp;
    public float failWalkAnimSpeed = 5f;
    public Sprite standingSpriteRight;
    public Sprite standingSpriteUp;
    public Sprite standingSpriteDown;
    public List<Sprite> idleAnimationDown;
    public List<Sprite> idleAnimationRight;
    public List<Sprite> idleAnimationUp;
    
    public float idleAnimSpeed = 3.5f;
    public List<Sprite> surpriseAnimation;
    public float surpriseAnimSpeed;
    public List<Sprite> sitDownAnimationDown;
    public List<Sprite> sitDownAnimationRight;
    public List<Sprite> sitDownAnimationUp;
    public List<Sprite> standUpAnimationDown;
    public List<Sprite> standUpAnimationRight;
    public List<Sprite> standUpAnimationUp;
    public float standUpAnimSpeed = 20f;
    public List<Sprite> chompAnimationDown;
    public List<Sprite> chompAnimationRight;
    public List<Sprite> chompAnimationUp;
    public float chompAnimSpeed = 5f;
}
