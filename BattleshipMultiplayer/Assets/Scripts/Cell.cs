using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    public Vector2 Coords { get; set; }

    private Color temporalColor = Color.green;

    private Player player;

    private void Start()
    {
        player = GetComponentInParent<Player>();
    }

    private void OnMouseEnter()
    {
        if (player.IsReady || !GetComponentInParent<ShipPlacement>().GetSelectedFlag())
        {
            return;
        }

        CheckIfShipCanBePlaced();
    }

    private void OnMouseExit()
    {
        if (player.IsReady || !GetComponentInParent<ShipPlacement>().GetSelectedFlag())
        {
            return;
        }

        if (GetComponent<Image>().color == temporalColor)
        {
            Ship ship = GetComponentInParent<ShipPlacement>().GetShip();
            if (ship.horizontal)
            {
                for (int i = 0; i < ship.numCells; i++)
                {
                    GameObject cellObj = player.GetCell((int)Coords.x, (int)Coords.y + i);
                    cellObj.GetComponent<Image>().color = Player.interactableCellColor;
                }
            }
            else
            {
                for (int i = 0; i < ship.numCells; i++)
                {
                    GameObject cellObj = player.GetCell((int)Coords.x + i, (int)Coords.y);
                    cellObj.GetComponent<Image>().color = Player.interactableCellColor;
                }
            }
        }
    }

    public bool CheckIfShipCanBePlaced()
    {
        Ship ship = GetComponentInParent<ShipPlacement>().GetShip();

        if (ship.horizontal) // Horizontal
        {
            if (Coords.y + ship.numCells > GameManager.N_COLS)
            {
                return false;
            }

            for (int i = 0; i < ship.numCells; i++)
            {
                GameObject cellObj = player.GetCell((int)Coords.x, (int)Coords.y + i);
                if (!cellObj.GetComponent<Button>().interactable)
                {
                    return false;
                }
            }

            // It can be placed
            for (int i = 0; i < ship.numCells; i++)
            {
                GameObject cellObj = player.GetCell((int)Coords.x, (int)Coords.y + i);
                cellObj.GetComponent<Image>().color = temporalColor;
            }
        }
        else // Vertical
        {
            if (Coords.x + ship.numCells > GameManager.N_ROWS)
            {
                return false;
            }

            for (int i = 0; i < ship.numCells; i++)
            {
                GameObject cellObj = player.GetCell((int)Coords.x + i, (int)Coords.y);
                if (!cellObj.GetComponent<Button>().interactable)
                {
                    return false;
                }
            }

            // It can be placed
            for (int i = 0; i < ship.numCells; i++)
            {
                GameObject cellObj = player.GetCell((int)Coords.x + i, (int)Coords.y);
                cellObj.GetComponent<Image>().color = temporalColor;
            }
        }

        return true;
    }
}
