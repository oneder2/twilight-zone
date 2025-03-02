using UnityEngine;

public class Door : Interactable
{
    public bool isOpen = false;         // 门是否打开
    public Sprite openSprite;           // 打开状态的精灵图
    public Sprite closedSprite;         // 关闭状态的精灵图
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    public override void Interact()
    {
        isOpen = !isOpen;  // 切换门的状态
        UpdateSprite();
        Debug.Log("门现在是 " + (isOpen ? "打开" : "关闭"));
    }

    void UpdateSprite()
    {
        spriteRenderer.sprite = isOpen ? openSprite : closedSprite;
    }
}