using UnityEngine;

public class GameRandom : MonoBehaviour
{

    // TODO: прикруить ввод сида через интерфейс

    public string seed;

    public void Init()
    {
        SetSeed(seed);

        int numericSeed = seed.GetHashCode(); // Переводим в числовой тип

        Random.InitState(numericSeed);

        Debug.Log("Random is alive!");
    }

    public void SetSeed(string seedFromPlr = "Hello, World") // Устанавливаем сид, в будущем здесь будет либо случайная строка, либо то, что напишет игрок
    {
        seed = seedFromPlr;
    }

    public int GetRandomInt(int a, int b)
    {
        return Random.Range(a, b);
    }
}
