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
    /*
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
            CollapseAllColumns();
            SpawnNewTiles();
        }
        else
        {
            // Eşleşme yok, geri al swap
            SwapTilesBack(a, b, aX, aY, bX, bY);
        }
    }*/
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
            // Artık direkt patlatma ve düşme yerine coroutine çağırıyoruz:
            StartCoroutine(HandleMatchesRoutine());
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

    void CollapseColumn(int x)
    {
        for (int y = 0; y < height - 1; y++)
        {
            if (grid[x, y] == null)
            {
                // Yukarıdan taş arıyoruz
                for (int aboveY = y + 1; aboveY < height; aboveY++)
                {
                    if (grid[x, aboveY] != null)
                    {
                        Tile fallingTile = grid[x, aboveY];
                        grid[x, y] = fallingTile;
                        grid[x, aboveY] = null;

                        fallingTile.SetPosition(x, y);
                        break;
                    }
                }
            }
        }
    }


    void CollapseAllColumns()
    {
        for (int x = 0; x < width; x++)
        {
            CollapseColumn(x);
        }
    }


    void SpawnNewTiles()
    {
        Vector2 offset = GetGridOffset();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    int tileType = Random.Range(0, tilePrefabs.Length);

                    // Spawn pozisyonunu biraz yukarıda başlatalım (animasyon için güzel durur)
                    Vector2 spawnPos = new Vector2(x * tileSize, (y + 1f) * tileSize) + offset;

                    GameObject tileGO = Instantiate(tilePrefabs[tileType], spawnPos, Quaternion.identity, gridParent);
                    Tile newTile = tileGO.GetComponent<Tile>();
                    newTile.Initialize(x, y, tileType, this);
                    newTile.SetPosition(x, y); // Hedef konuma anında ışınla (ileride animasyonlu yaparız)

                    grid[x, y] = newTile;
                }
            }
        }
    }


    private IEnumerator HandleMatchesRoutine()
    {
        bool matchesFound = true;

        while (matchesFound)
        {
            yield return new WaitForSeconds(0.1f); // küçük gecikme, animasyon için

            HashSet<Tile> allMatches = new HashSet<Tile>();

            // Tüm grid'i tara eşleşmeleri bul
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = grid[x, y];
                    if (tile == null) continue;

                    List<Tile> matches = GetMatches(tile);
                    if (matches.Count >= 3)
                    {
                        foreach (Tile t in matches)
                        {
                            allMatches.Add(t);
                        }
                    }
                }
            }

            if (allMatches.Count == 0)
            {
                // Eşleşme kalmadı
                matchesFound = false;
                yield break; // Coroutine sonlanır
            }

            // Eşleşmeleri patlat
            foreach (Tile t in allMatches)
            {
                if (t != null)
                {
                    grid[t.x, t.y] = null;
                    Destroy(t.gameObject);
                }
            }

            yield return new WaitForSeconds(0.2f); // Patlama animasyonu için bekle

            CollapseAllColumns();

            yield return new WaitForSeconds(0.2f); // Düşme animasyonu için bekle

            SpawnNewTiles();

            yield return new WaitForSeconds(0.2f); // Yeni taşların gelmesi için bekle
        }
    }
}
