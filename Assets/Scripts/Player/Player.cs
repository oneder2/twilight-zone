using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    public PlayerExhaustedState exhaustedState {get; private set;}
    #endregion

    #region Flipping
    private bool facingRight = true;
    private int facingDir = 1;
    #endregion

    #region Stamina
    public float maxStamina = 100f;
    public float currentStamina;
    public float regenerationRate = 10f;
    public float fastRegenerationFactor = 3;
    public float depletionRate = 20f;
    public bool exhaustMarker = false;
    public float exhaustedFactor = 0.8f;
    public Slider staminaBar;
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

        currentStamina = maxStamina;
        stateMachine = PlayerStateMachine.Instance;
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        walkState = new PlayerWalkState(this, stateMachine, "Walk");
        exhaustedState = new PlayerExhaustedState(this, stateMachine, "Exhausted");
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
        if (GameManager.Instance.isInDialogue) return; // 对话期间暂停移动逻辑
        
        stateMachine.currentState.Update();

        // 当不在 RunState 时恢复体力
        if (stateMachine.currentState != runState)
        {
            if (IsMoving())
            {
                currentStamina += regenerationRate * Time.deltaTime;
            }
            else
            {
                currentStamina += regenerationRate * fastRegenerationFactor * Time.deltaTime;
            }
            
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            if (currentStamina == maxStamina)
            {
                exhaustMarker = false;
            }
        }

        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;
        }

        FlipController();
    }

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.linearVelocity = new Vector2(_xVelocity, _yVelocity);
    }

    public void ZeroVelocity()
    {
        rb.linearVelocity = new Vector2(0, 0);
    }

    public void ChangeStateTo(PlayerState targetState)
    {
        stateMachine.ChangeState(targetState);
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

    // 新增：检查是否可以奔跑
    public bool CanRun()
    {
        return currentStamina > 0 && !exhaustMarker;
    }

    public bool IsMoving()
    {
        return rb.linearVelocity != Vector2.zero;
    }
}