using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShipPlacement : MonoBehaviour
{
    [SerializeField]
    private Button[] shipsButtons;

    [SerializeField]
    private GameObject okButton;

    private bool selectedFlag;
    private GameObject selectedShip;

    private void Update()
    {
        if (!selectedFlag)
        {
            return;
        }
    }

    public bool GetSelectedFlag()
    {
        return selectedFlag;
    }

    public Ship GetShip()
    {
        return selectedShip.GetComponent<Ship>();
    }

    public void Select(GameObject ship)
    {
        selectedFlag = true;
        selectedShip = ship;
    }

    public Ship PutShip()
    {
        selectedFlag = false;
        selectedShip.GetComponent<Button>().interactable = false;

        if (AllShipsPositioned())
        {
            okButton.SetActive(true);
        }

        return selectedShip.GetComponent<Ship>();
    }

    private bool AllShipsPositioned()
    {
        foreach (Button button in shipsButtons)
        {
            if (button.interactable)
            {
                return false;
            }
        }

        return true;
    }

    public void RotateShip()
    {
        if (selectedShip == null)
        {
            return;
        }
        selectedShip.GetComponent<Ship>().Rotate();
    }
}
