using UnityEngine;


public class GenerateTiles : MonoBehaviour
{
    [SerializeField] GameObject[] tilesPrefabs;



    public void PlaceTiles(int[,] cells)
    { 
        // Смещение, чтобы центр поля был в (0,0)
        Vector2 offset = new Vector2(
            -(cells.GetLength(0) - 1) * 1 * 0.5f,
            -(cells.GetLength(1) - 1) * 1 * 0.5f
        );
        for (int i = 0; i < cells.GetLength(0); i++)
        {
            for (int j = 0; j < cells.GetLength(1); j++)
            {
                Instantiate(tilesPrefabs[cells[i, j]], new Vector2(i, j) + offset, Quaternion.identity);
            }
        }
    }
}
