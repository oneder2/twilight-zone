using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region Components
    public Animator anim {get; private set;}
    public Rigidbody2D rb {get; private set;}
    #endregion

    #region State Machine
    public PlayerStateMachine stateMachine {get; private set;}
    public PlayerIdleState idleState {get; private set;}
    public PlayerWalkState walkState {get; private set;}
    public PlayerRunState runState {get; private set;}
    #endregion

    #region Flipping
    private bool facingRight = true;
    private int facingDir = 1;
    #endregion

    [Header("Move info")] 
    public float walkSpeed = 1;
    public float accelerate = 1.5f;

    // 静态实例，作为全局访问点
    public static Player Instance;

    void Awake()
    {
        // 如果 instance 还未赋值，当前对象成为单例实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 标记为在场景切换时不销毁
        }
        else
        {
            Destroy(gameObject); // 如果已有实例，销毁当前对象
        }
        
        stateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        walkState = new PlayerWalkState(this, stateMachine, "Walk");
        runState = new PlayerRunState(this, stateMachine, "Run");
    }

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        stateMachine.currentState.Update();

        FlipController();
    }

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.linearVelocity = new Vector2(_xVelocity, _yVelocity);
    }


    private void Flip()
    {
        facingDir *= -1;
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void FlipController()
    {
        if (rb.linearVelocity.x > 0 && !facingRight)
            Flip();
        else if (rb.linearVelocity.x < 0 && facingRight)
            Flip();
    }
}