using UnityEngine;

public class EnemyNPC : Deadly
{
    private Transform player;
    public float speed = 2f;

    // override KillPlayer Method, and kill the player.
    override public void KillPLayer()
    {
        // change to dead state
        GameRunManager.Instance.ChangeGameStatus(GameStatus.GameOver);
    }

    // If the Enemy touches the player, player will dead.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (LayerMask.LayerToName(collision.gameObject.layer) == "Player")
        {
            KillPLayer();
        }
    }

    void Start()
    {
        player = Player.Instance.transform;
    }

    void Update()
    {
        // Enemy approach player with time.
        if (player != null)
        {
            // calculate direction from current GameObject to player
            Vector2 direction = (player.position - transform.position).normalized;

            //  move GameObject within the time
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        }
    }
}