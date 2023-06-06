using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Made a seperate animator function from Unity to give more functionality
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] List<Sprite> walkDownSprites;
    [SerializeField] List<Sprite> walkUpSprites;
    [SerializeField] List<Sprite> walkRightSprites;
    [SerializeField] List<Sprite> walkLeftSprites;
    [SerializeField] FacingDirection defaultDirection = FacingDirection.Down;

    //Parameters
    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool IsMoving { get; set; }

    //States
    public SpriteAnimator walkDownAnim;
    public SpriteAnimator walkUpAnim;
    public SpriteAnimator walkRightAnim;
    public SpriteAnimator walkLeftAnim;

    public SpriteAnimator currentAnim;

    bool wasPreviouslyMoving;

    //References
    SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        walkDownAnim = new SpriteAnimator(walkDownSprites, spriteRenderer);
        walkUpAnim = new SpriteAnimator(walkUpSprites, spriteRenderer);
        walkRightAnim = new SpriteAnimator(walkRightSprites, spriteRenderer);
        walkLeftAnim = new SpriteAnimator(walkLeftSprites, spriteRenderer);
        SetFacingDirection(defaultDirection);

        currentAnim = walkDownAnim; //default
    }

    private void Update()
    {
        var prevAnim = currentAnim;

        if (MoveX == 1 && MoveY == 0)
            currentAnim = walkRightAnim;
        else if (MoveX == -1 && MoveY == 0)
            currentAnim = walkLeftAnim;
        else if (MoveY == 1 && MoveX == 0)
            currentAnim = walkUpAnim;
        else if (MoveY == -1 && MoveX == 0)
            currentAnim = walkDownAnim;

        if (currentAnim != prevAnim || IsMoving != wasPreviouslyMoving) //fixes bug that allowed movement w/o animation
            currentAnim.Start();

        if (IsMoving)
            currentAnim.HandleUpdate();
        else
            spriteRenderer.sprite = currentAnim.Frames[0];

        wasPreviouslyMoving = IsMoving;
    }

    public void SetFacingDirection(FacingDirection dir)
    {
        if (dir == FacingDirection.Right)
        {
            MoveX = 1;
            MoveY = 0;
        }
            
        else if (dir == FacingDirection.Left)
        {
            MoveX = -1;
            MoveY = 0;
        }
        else if (dir == FacingDirection.Up)
        {
            MoveX = 0;
            MoveY = 1;
        }
        else if (dir == FacingDirection.Down)
        {
            MoveX = 0;
            MoveY = -1;
        }
    }

    public FacingDirection DefaultDirection
    {
        get => defaultDirection;
    }
}

public enum FacingDirection { Up, Down, Left, Right }
