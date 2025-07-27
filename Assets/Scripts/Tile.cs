using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, y;
    public int tileType;
    private GridManager gridManager;

    public void Initialize(int x, int y, int type, GridManager manager)
    {
        this.x = x;
        this.y = y;
        this.tileType = type;
        this.gridManager = manager;
    }

    public void SetPosition(int newX, int newY)
    {
        x = newX;
        y = newY;
        Vector2 offset = gridManager.GetGridOffset();
        transform.position = new Vector2(x * gridManager.tileSize, y * gridManager.tileSize) + offset;
    }

    private void OnMouseDown()
    {
        gridManager.SelectTile(this);
    }
}