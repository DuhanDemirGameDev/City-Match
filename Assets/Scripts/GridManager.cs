using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public float tileSize = 0.6f;

    public GameObject[] tilePrefabs;
    public Transform gridParent;

    public Tile[,] grid;

    private Tile selectedTile;

    void Start()
    {
        grid = new Tile[width, height];
        GenerateGrid();
        StartCoroutine(ClearInitialMatches());
    }

    // Grid'in ortasını hesaplayan offset fonksiyonu
    public Vector2 GetGridOffset()
    {
        float offsetX = -width / 2f * tileSize + tileSize / 2f;
        float offsetY = -height / 2f * tileSize + tileSize / 2f;
        return new Vector2(offsetX, offsetY);
    }

    void GenerateGrid()
    {
        Vector2 offset = GetGridOffset();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 spawnPos = new Vector2(x * tileSize, y * tileSize) + offset;
                int safeType = GetSafeTileType(x, y);

                GameObject tileGO = Instantiate(tilePrefabs[safeType], spawnPos, Quaternion.identity, gridParent);
                Tile tile = tileGO.GetComponent<Tile>();
                tile.Initialize(x, y, safeType, this);

                grid[x, y] = tile;
            }
        }
    }

    int GetSafeTileType(int x, int y)
    {
        List<int> possibleTypes = new List<int>();
        for (int i = 0; i < tilePrefabs.Length; i++)
            possibleTypes.Add(i);

        // Sol kontrolü
        if (x >= 2 &&
            grid[x - 1, y] != null &&
            grid[x - 2, y] != null &&
            grid[x - 1, y].tileType == grid[x - 2, y].tileType)
        {
            possibleTypes.Remove(grid[x - 1, y].tileType);
        }

        // Üst kontrolü
        if (y >= 2 &&
            grid[x, y - 1] != null &&
            grid[x, y - 2] != null &&
            grid[x, y - 1].tileType == grid[x, y - 2].tileType)
        {
            possibleTypes.Remove(grid[x, y - 1].tileType);
        }

        if (possibleTypes.Count == 0) return 0;
        return possibleTypes[Random.Range(0, possibleTypes.Count)];
    }

    public void SelectTile(Tile tile)
    {
        if (tile == null) return;

        if (selectedTile == null)
        {
            selectedTile = tile;
            return;
        }

        if (selectedTile == tile)
        {
            // Aynı taşı tekrar seçerse seçimi iptal et
            selectedTile = null;
            return;
        }

        if (AreAdjacent(selectedTile, tile))
        {
            SwapTiles(selectedTile, tile);
            selectedTile = null; // Swap sonrası seçimi sıfırla
        }
        else
        {
            // Yan yana değilse, sadece seçimi değiştir (silme yok)
            selectedTile = tile;
        }
    }

    bool AreAdjacent(Tile a, Tile b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    public void SwapTiles(Tile a, Tile b)
    {
        if (a == null || b == null) return;

        int aX = a.x;
        int aY = a.y;
        int bX = b.x;
        int bY = b.y;

        // Swap grid referansları önce
        grid[a.x, a.y] = b;
        grid[b.x, b.y] = a;

        // Swap pozisyonları
        a.SetPosition(bX, bY);
        b.SetPosition(aX, aY);

        // Match kontrolü
        List<Tile> matches = GetMatches(a);
        matches.AddRange(GetMatches(b));

        if (matches.Count >= 3)
        {
            foreach (Tile t in matches)
            {
                if (t != null)
                {
                    grid[t.x, t.y] = null;
                    Destroy(t.gameObject);
                }
            }
        }
        else
        {
            // Eşleşme yok, geri al swap
            SwapTilesBack(a, b, aX, aY, bX, bY);
        }
    }

    void SwapTilesBack(Tile a, Tile b, int aOldX, int aOldY, int bOldX, int bOldY)
    {
        // Grid referanslarını eski haline getir
        grid[a.x, a.y] = a;
        grid[b.x, b.y] = b;

        // Pozisyonları eski haline getir
        a.SetPosition(aOldX, aOldY);
        b.SetPosition(bOldX, bOldY);
    }

    List<Tile> GetMatches(Tile tile)
    {
        List<Tile> matchingTiles = new List<Tile>();

        // Horizontal
        List<Tile> horizontal = new List<Tile> { tile };
        int x = tile.x;
        int y = tile.y;

        for (int i = x - 1; i >= 0; i--)
        {
            if (grid[i, y] != null && grid[i, y].tileType == tile.tileType)
                horizontal.Add(grid[i, y]);
            else break;
        }
        for (int i = x + 1; i < width; i++)
        {
            if (grid[i, y] != null && grid[i, y].tileType == tile.tileType)
                horizontal.Add(grid[i, y]);
            else break;
        }

        if (horizontal.Count >= 3)
            matchingTiles.AddRange(horizontal);

        // Vertical
        List<Tile> vertical = new List<Tile> { tile };
        for (int i = y - 1; i >= 0; i--)
        {
            if (grid[x, i] != null && grid[x, i].tileType == tile.tileType)
                vertical.Add(grid[x, i]);
            else break;
        }
        for (int i = y + 1; i < height; i++)
        {
            if (grid[x, i] != null && grid[x, i].tileType == tile.tileType)
                vertical.Add(grid[x, i]);
            else break;
        }

        if (vertical.Count >= 3)
            matchingTiles.AddRange(vertical);

        // Aynı taşın iki kere eklenmesini önle
        matchingTiles = new List<Tile>(new HashSet<Tile>(matchingTiles));

        return matchingTiles;
    }

    IEnumerator ClearInitialMatches()
    {
        yield return new WaitForSeconds(0.1f);

        bool matchFound = true;

        while (matchFound)
        {
            matchFound = false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = grid[x, y];
                    if (tile == null) continue;

                    List<Tile> match = GetMatches(tile);
                    if (match.Count >= 3)
                    {
                        matchFound = true;
                        foreach (Tile t in match)
                        {
                            if (t != null)
                            {
                                grid[t.x, t.y] = null;
                                Destroy(t.gameObject);
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}
