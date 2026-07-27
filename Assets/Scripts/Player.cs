using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IPlayable
{
    [SerializeField] private Rigidbody2D rigid;
    [SerializeField] private Animator anim;

    [Space(20f)]
    [SerializeField] private float health = 100f;
    [SerializeField] private float speed = 1f;

    private bool isAttack;
    private Vector2 input;
    private Action attack;

    public Vector2 CurrentVelocity => rigid.linearVelocity;

#if UNITY_EDITOR
    private void Reset()
    {
        anim = this.GetComponent<Animator>();
        rigid = this.GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
    }
#endif

    private void Update()
    {
        if (health < 0f) return;

        GetInput();
        Move();
        Click();
        Test();
    }

    private void GetInput()
    {
        if (Input.GetKey(KeyCode.A)) input.x = -1f;
        else if (Input.GetKey(KeyCode.D)) input.x = 1f;
        else input.x = 0f;

        if (Input.GetKey(KeyCode.W)) input.y = 1f;
        else if (Input.GetKey(KeyCode.S)) input.y = -1f;
        else input.y = 0f;
    }

    private void Move()
    {
        if (!isAttack)
        { 
            if(input != Vector2.zero) anim.Play("Run", 0);
            else anim.Play("Idle", 0);
        }


        rigid.linearVelocity = input.normalized * speed;
    }

    private void Click()
    {
        if (!isAttack && Input.GetKeyDown(KeyCode.Mouse0))
        {
            isAttack = true;
            attack?.Invoke();

            anim.Play("AttackSlash", 0);
        }

        else if (1f <= anim.GetCurrentAnimatorStateInfo(0).normalizedTime)
        {
            isAttack = false;
        }
    }

    public void Hit(float dmg)
    {
        if (health < 0f) return;
        health -= dmg;

        if (health < 0f) anim.Play("Death", 0);
        else anim.Play("Hit", 0);
    }

    /// <summary>
    /// 무기 추가 (죽을 경우 이벤트 구독 해제 해줘야함)
    /// </summary>
    /// <param name="attackEvent"></param>
    public void AddWeapon(Action attackEvent)
    {
        attack += attackEvent;
    }

    private void Test()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Hit(20f);
        }
    }
}
