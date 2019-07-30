using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [SerializeField]
    private GameObject canvas;

    [SerializeField]
    private GameObject panelGridPrefab;

    [SerializeField]
    private GameObject cellPrefab;

    [SerializeField]
    private Text textTurn;

    [SerializeField]
    private GameObject playAgainObj;

    [SyncVar]
    private int playerID;

    private GameObject[,] myCells = new GameObject[GameManager.N_ROWS, GameManager.N_COLS];
    private GameObject[,] enemyCells = new GameObject[GameManager.N_ROWS, GameManager.N_COLS];

    public static Color interactableCellColor;
    private Color colorShip = Color.grey;
    private Color colorWater = Color.cyan;
    private Color colorTouched = Color.red;
    private Color colorSunken = Color.black;

    private GameObject myGridObj, enemyGridObj;

    private Vector3 offset = new Vector3(3f, 0f, 0f);
    public static float perc = 0.95f;

    public bool IsReady { get; set; }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            canvas.SetActive(false);
        }
        else
        {
            interactableCellColor = cellPrefab.GetComponent<Image>().color;
            GenerateGrid();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        GameManager.RegisterPlayer(this);
    }

    public void SetPlayerID(int id)
    {
        if (!isServer)
        {
            return;
        }

        playerID = id;
    }

    private void GenerateGrid(bool isMine = true)
    {
        int rows = GameManager.N_ROWS;
        int cols = GameManager.N_COLS;
        GameObject panelGrid = Instantiate(panelGridPrefab, canvas.transform);
        GridLayoutGroup gridLayoutGroup = panelGrid.GetComponent<GridLayoutGroup>();
        float width = panelGrid.GetComponent<RectTransform>().sizeDelta.x / cols * perc;
        float height = panelGrid.GetComponent<RectTransform>().sizeDelta.y / rows * perc;
        gridLayoutGroup.cellSize = new Vector2(width, height);
        gridLayoutGroup.constraintCount = cols;

        if (isMine)
        {
            myGridObj = panelGrid;
        }
        else
        {
            enemyGridObj = panelGrid;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject obj = Instantiate(cellPrefab, panelGrid.transform);
                obj.GetComponent<Cell>().Coords = new Vector2(row, col);
                obj.GetComponent<BoxCollider2D>().size = new Vector2(width, height);

                if (isMine)
                {
                    obj.GetComponent<Button>().onClick.AddListener(() => ClickPlayerCell(obj.GetComponent<Cell>()));
                    myCells[row, col] = obj;
                }
                else
                {
                    obj.GetComponent<Button>().onClick.AddListener(() => ClickEnemyCell(obj.GetComponent<Cell>()));
                    enemyCells[row, col] = obj;
                }
            }
        }
    }

    // Put ships
    private void ClickPlayerCell(Cell cell)
    {
        if (!GetComponent<ShipPlacement>().GetSelectedFlag() || cell.gameObject.GetComponent<Image>().color == interactableCellColor)
        {
            return;
        }

        Ship ship = GetComponent<ShipPlacement>().PutShip();
        foreach (Vector2 coords in ship.GetCoords(cell))
        {
            CmdPutShip(coords);

            int x = (int)coords.x;
            int y = (int)coords.y;
            myCells[x, y].GetComponent<Image>().color = colorShip;
        
            // We cannot put ships in the adjacent cells
            for (int row = x - 1; row <= x + 1; row++)
            {
                for (int col = y - 1; col <= y + 1; col++)
                {
                    if (row >= 0 && col >= 0 && row < GameManager.N_ROWS && col < GameManager.N_COLS)
                    {
                        myCells[row, col].GetComponent<Button>().interactable = false;
                    }
                }
            }
        }
    }

    [Command]
    private void CmdPutShip(Vector2 coords)
    {
        GameManager.PutShip(playerID, coords);
    }

    // Check enemy cell
    private void ClickEnemyCell(Cell cell)
    {
        CmdClickEnemyCell(cell.Coords);
    }

    [Command]
    private void CmdClickEnemyCell(Vector2 coords)
    {
        GameManager.eCellState state = GameManager.GetEnemyCellState(playerID, coords);

        int x = (int)coords.x;
        int y = (int)coords.y;

        bool sunken = false;

        if (state == GameManager.eCellState.Water)
        {
            GameManager.PlayerFoundWater(playerID, coords);
            GameManager.NextTurn(playerID);
        }
        else
        {
            GameManager.SinkShip(playerID, coords);
            sunken = GameManager.HasBeenSunk(playerID, x, y);
        }
        RpcClickEnemyCell(x, y, state, sunken);
    }

    [ClientRpc]
    private void RpcClickEnemyCell(int x, int y, GameManager.eCellState state, bool sunken)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (state == GameManager.eCellState.Ship) // Sunken
        {
            if (sunken)
            {
                EnemyShipSunken(x, y);
            }
            else
            {
                EnemyShipTouched(x, y);
            }
        }
        else // Water
        {
            WaterFound(x, y);
            DisableGrid(enemyCells);
        }
    }

    private void EnemyShipTouched(int x, int y)
    {
        enemyCells[x, y].GetComponent<Image>().color = colorTouched;
        enemyCells[x, y].GetComponent<Button>().interactable = false;
    }

    private void EnemyShipSunken(int x, int y)
    {
        enemyCells[x, y].GetComponent<Image>().color = colorSunken;
        enemyCells[x, y].GetComponent<Button>().interactable = false;

        // Fill adjacent cells with water
        List<Vector2> list = new List<Vector2>();
        list = GetShipCells(ref list, x, y);
        foreach (Vector2 v in list)
        {
            for (int row = (int)v.x - 1; row <= (int)v.x + 1; row++)
            {
                for (int col = (int)v.y - 1; col <= (int)v.y + 1; col++)
                {
                    if ((row != (int)v.x || col != (int)v.y) && row >= 0 && col >= 0 && row < GameManager.N_ROWS && col < GameManager.N_COLS)
                    {
                        if (enemyCells[row, col].GetComponent<Image>().color == colorTouched)
                        {
                            EnemyShipSunken(row, col);
                        }
                        else if (enemyCells[row, col].GetComponent<Image>().color == interactableCellColor)
                        {
                            WaterFound(row, col);
                            GameManager.PlayerFoundWater(playerID, new Vector2(row, col));
                        }
                    }
                }
            }
        }
    }

    private List<Vector2> GetShipCells(ref List<Vector2> list, int x, int y)
    {
        list.Add(new Vector2(x, y));

        for (int row = x - 1; row <= x + 1; row++)
        {
            for (int col = y - 1; col <= y + 1; col++)
            {
                if ((row != x || col != y) && row >= 0 && col >= 0 && row < GameManager.N_ROWS && col < GameManager.N_COLS &&
                    enemyCells[row, col].GetComponent<Image>().color == colorTouched &&
                    !list.ContainsVector2(new Vector2(row, col)))
                {
                    foreach (Vector2 v in GetShipCells(ref list, row, col))
                    {
                        if (!list.ContainsVector2(v))
                        {
                            list.Add(v);
                        }
                    }
                }
            }
        }

        return list;
    }

    private void WaterFound(int x, int y)
    {
        enemyCells[x, y].GetComponent<Image>().color = colorWater;
        enemyCells[x, y].GetComponent<Button>().interactable = false;
    }

    [ClientRpc]
    public void RpcUpdateMyGrid(int x, int y, GameManager.eCellState state)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        switch (state)
        {
            case GameManager.eCellState.Water:
                myCells[x, y].GetComponent<Image>().color = colorWater;
                break;
            case GameManager.eCellState.TouchedShip:
                myCells[x, y].GetComponent<Image>().color = colorTouched;
                break;
            case GameManager.eCellState.SunkenShip:
                myCells[x, y].GetComponent<Image>().color = colorSunken;
                break;
        }
    }

    public void PlayerReady()
    {
        IsReady = true;
        DisableGrid(myCells);

        CmdPlayerReady();
    }

    private void DisableGrid(GameObject[,] grid)
    {
        for (int row = 0; row < GameManager.N_ROWS; row++)
        {
            for (int col = 0; col < GameManager.N_COLS; col++)
            {
                grid[row, col].GetComponent<Button>().interactable = false;
            }
        }
    }

    [Command]
    private void CmdPlayerReady()
    {
        GameManager.PlayerReady(playerID);
    }

    [ClientRpc]
    public void RpcStartGame()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        GenerateGrid(false);
        myGridObj.transform.position -= offset;
        enemyGridObj.transform.position += offset;
        RpcEndTurn();
    }

    [ClientRpc]
    public void RpcEndTurn()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        DisableGrid(enemyCells);

        textTurn.text = "Wait";
    }

    [ClientRpc]
    public void RpcStartTurn()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        for (int row = 0; row < GameManager.N_ROWS; row++)
        {
            for (int col = 0; col < GameManager.N_COLS; col++)
            {
                if (enemyCells[row, col].GetComponent<Image>().color == interactableCellColor)
                {
                    enemyCells[row, col].GetComponent<Button>().interactable = true;
                }
            }
        }

        textTurn.text = "Your turn";
    }

    public GameObject GetCell(int x, int y)
    {
        return myCells[x, y];
    }

    [ClientRpc]
    public void RpcGameOver(int loserID)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (playerID == loserID)
        {
            textTurn.text = "Defeat...";
        }
        else
        {
            textTurn.text = "VICTORY!";
        }

        playAgainObj.SetActive(true);
    }
}
