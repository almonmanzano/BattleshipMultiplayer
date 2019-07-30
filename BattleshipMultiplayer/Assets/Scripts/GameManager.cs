using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkBehaviour
{
    public const int N_ROWS = 10;
    public const int N_COLS = 10;

    private const int NUM_PLAYERS = 2;

    private static int numPlayersRegistered;

    private static Player player1, player2;
    private static Player[] players = new Player[NUM_PLAYERS];

    private static Dictionary<Vector2, eCellState>[] grids = new Dictionary<Vector2, eCellState>[NUM_PLAYERS];

    private static bool[] playersReady = new bool[NUM_PLAYERS];

    private static int[] numShips = new int[NUM_PLAYERS];

    public enum eCellState
    {
        Water,
        Ship,
        TouchedShip,
        SunkenShip
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log("On Start Server");

        for (int i = 0; i < NUM_PLAYERS; i++)
        {
            grids[i] = new Dictionary<Vector2, eCellState>();
            for (int row = 0; row < N_ROWS; row++)
            {
                for (int col = 0; col < N_COLS; col++)
                {
                    grids[i].Add(new Vector2(row, col), eCellState.Water);
                }
            }
        }
    }

    public static void RegisterPlayer(Player player)
    {
        players[numPlayersRegistered] = player;
        player.SetPlayerID(numPlayersRegistered);
        numPlayersRegistered++;
        player.transform.name = "Player" + (numPlayersRegistered);
        Debug.Log("Registered " + player.transform.name);
    }

    public static void PutShip(int id, Vector2 coords)
    {
        grids[id][coords] = eCellState.Ship;
        numShips[id]++;
    }

    public static void PlayerReady(int id)
    {
        playersReady[id] = true;

        for (int i = 0; i < playersReady.Length; i++)
        {
            if (playersReady[i] == false)
            {
                return;
            }
        }

        StartGame();
    }

    private static void StartGame()
    {
        Debug.Log("Start game");

        foreach (Player player in players)
        {
            player.RpcStartGame();
        }

        int turn = Random.Range(0, NUM_PLAYERS);
        Debug.Log("Player " + turn + "'s turn");
        players[turn].RpcStartTurn();
    }

    public static eCellState GetEnemyCellState(int playerID, Vector2 coords)
    {
        int enemyID = GetOtherPlayerID(playerID);

        return grids[enemyID][coords];
    }

    public static bool HasBeenSunk(int playerID, int x, int y)
    {
        int enemyID = GetOtherPlayerID(playerID);

        List<Vector2> list = new List<Vector2>();
        list = GetShipCells(enemyID, ref list, x, y);

        foreach (Vector2 v in list)
        {
            if ((v.x != x || v.y != y) && grids[enemyID][v] != eCellState.TouchedShip)
            {
                return false;
            }
        }

        foreach (Vector2 v in list)
        {
            UpdateCellState(enemyID, v, eCellState.SunkenShip);
        }

        return true;
    }

    public static void PlayerFoundWater(int playerID, Vector2 coords)
    {
        int enemyID = GetOtherPlayerID(playerID);

        UpdateCellState(enemyID, coords, eCellState.Water);
    }

    private static List<Vector2> GetShipCells(int id, ref List<Vector2> list, int x, int y)
    {
        if (grids[id][new Vector2(x, y)] != eCellState.Water)
        {
            list.Add(new Vector2(x, y));
        }

        for (int row = x - 1; row <= x + 1; row++)
        {
            for (int col = y - 1; col <= y + 1; col++)
            {
                if ((row != x || col != y) && row >= 0 && col >= 0 && row < N_ROWS && col < N_COLS &&
                    grids[id][new Vector2(row, col)] != eCellState.Water &&
                    !list.ContainsVector2(new Vector2(row, col)))
                {
                    foreach (Vector2 v in GetShipCells(id, ref list, row, col))
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

    private static int GetOtherPlayerID(int id)
    {
        return (id + 1) % NUM_PLAYERS;
    }

    public static void NextTurn(int id)
    {
        players[id].RpcEndTurn();
        int turn = GetOtherPlayerID(id);
        players[turn].RpcStartTurn();
        Debug.Log("Player " + turn + "'s turn");
    }

    public static void SinkShip(int playerID, Vector2 coords)
    {
        int enemyID = GetOtherPlayerID(playerID);

        UpdateCellState(enemyID, coords, eCellState.TouchedShip);

        numShips[enemyID]--;

        if (numShips[enemyID] == 0)
        {
            GameOver(enemyID);
        }
    }

    private static void UpdateCellState(int id, Vector2 coords, eCellState state)
    {
        grids[id][coords] = state;
        players[id].RpcUpdateMyGrid((int)coords.x, (int)coords.y, grids[id][coords]);
    }

    private static void GameOver(int id)
    {
        Debug.Log("Player " + id + " has lost");

        foreach (Player player in players)
        {
            player.RpcEndTurn();
            player.RpcGameOver(id);
        }
    }
}
