using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameRandom rnd;
    [SerializeField] GenerateWorld generateWorld;
    [SerializeField] GenerateTiles generateTiles;

    private int[,] worldArray;

    private void Awake()
    {
        rnd.Init();
        worldArray = generateWorld.Init(rnd);
        generateTiles.PlaceTiles(worldArray);

    }
}
