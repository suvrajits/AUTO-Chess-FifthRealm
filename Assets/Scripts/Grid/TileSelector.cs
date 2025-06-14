using UnityEngine;

public class TileSelector : MonoBehaviour
{
    public LayerMask tileLayer;

    public bool GetTileUnderCursor(out GridTile tile)
    {
        tile = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
        {
            tile = hit.collider.GetComponent<GridTile>();
            return tile != null;
        }

        return false;
    }
}
