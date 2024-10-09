using MajdataPlay.Types;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class XxlbAnimationController : MonoBehaviour
{
    Image image;
    Animator animator;
    public Sprite[] sprites;

    public static XxlbAnimationController instance;

    short dir=0;

    private void Awake()
    {
        instance = this; 
    }

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        animator = GetComponent<Animator>();
    }

    public void Stepping()
    {
        animator.SetTrigger("dance");
        image.sprite = sprites[1];
    }

    public void Dance(JudgeType result)
    {
        switch (result)
        {
            case JudgeType.Perfect:
            case JudgeType.FastPerfect1:
            case JudgeType.FastPerfect2:
            case JudgeType.LatePerfect1:
            case JudgeType.LatePerfect2:
                animator.SetTrigger("dance");
                if (dir == 0)//left
                {
                    image.sprite = sprites[0];
                }
                if (dir == 1)//center
                {
                    image.sprite = sprites[1];
                }
                if (dir == 2)//right
                {
                    image.sprite = sprites[2];
                }
                if (dir == 3)//center
                {
                    image.sprite = sprites[1];
                }
                dir++;
                if(dir>3)dir = 0;
                break;
            default:
                animator.SetTrigger("dance");
                if (dir == 0)//left
                {
                    image.sprite = sprites[3];
                }
                if (dir == 1)//center
                {
                    image.sprite = sprites[1];
                }
                if (dir == 2)//right
                {
                    image.sprite = sprites[4];
                }
                if (dir == 3)//center
                {
                    image.sprite = sprites[1];
                }
                dir++;
                if (dir > 3) dir = 0;
                break;
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }
}