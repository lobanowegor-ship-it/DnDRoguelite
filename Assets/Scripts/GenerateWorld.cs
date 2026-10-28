
using System;
using System.Collections.Generic;

using System.Text;

using UnityEngine;


public class GenerateWorld : MonoBehaviour
{
    // Описание генерации игрового мира для забега:
    // На вход этого файла получаем количество заполненных клеток
    // Создаём двумерный массив, который будет нашим игровым полем
    // Заполняем массив либо 0, либо 1 где 0 - пустая клетка, 1 - заполненная клетка

    [SerializeField] int width; // Ширина игрового поля
    [SerializeField] int height; // Высота игрового поля

    [SerializeField] int eventsCount; // Количество боев, магазинов и текстовых событий
    public int battleCounts = 7; // Количество боев
    public int campCounts = 3; // Количество лагерей
    public int shopCounts = 2; // Количество магазинов

    private int[,] cells;

    public int[,] Init(GameRandom rnd)
    {
        cells = new int[width, height];

        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                cells[i, j] = 0;

        GenerateArray(cells, eventsCount, rnd); // Заполняем массив единицами, которые потом поменяются на id событий

        Print2DArray(cells);
        Debug.Log("World Generated");

        GenerateEvents(cells, battleCounts, campCounts, shopCounts, rnd); // Присваиваем событиям id, чтобы потом их можно было различать
        MirrorHorizontal(cells);

        return cells;
    }

    public void GenerateArray(int[,] cells, int eventsCount, GameRandom rnd)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        // Минимальное расстояние между единицами (в клетках).
        // minDist = 2 означает, что вокруг единицы не может быть других единиц
        // в радиусе 1 (все 8 соседей). Если нужно только 4 соседа — измените цикл ниже.
        int minDist = 2;

        // Список доступных клеток
        var available = new List<(int i, int j)>();
        int[,] indexInList = new int[width, height];

        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                indexInList[i, j] = available.Count;
                available.Add((i, j));
            }

        int placed = 0;
        while (placed < eventsCount && available.Count > 0)
        {
            // Выбираем случайную доступную клетку
            int idx = rnd.GetRandomInt(0, available.Count);
            (int i, int j) = available[idx];

            // Удаляем её из списка (swap-remove)
            int lastIdx = available.Count - 1;
            available[idx] = available[lastIdx];
            indexInList[available[idx].i, available[idx].j] = idx;
            available.RemoveAt(lastIdx);
            indexInList[i, j] = -1; // помечаем как недоступную

            // Ставим единицу
            cells[i, j] = 1;
            placed++;

            // Запрещаем размещение в окрестности
            for (int di = -minDist + 1; di <= minDist - 1; di++)
            {
                for (int dj = -minDist + 1; dj <= minDist - 1; dj++)
                {
                    if (di == 0 && dj == 0) continue;

                    int ni = i + di;
                    int nj = j + dj;
                    if (ni < 0 || ni >= width || nj < 0 || nj >= height) continue;

                    int neighborIdx = indexInList[ni, nj];
                    if (neighborIdx >= 0) // если ещё доступна
                    {
                        // swap-remove
                        int last = available.Count - 1;
                        available[neighborIdx] = available[last];
                        indexInList[available[neighborIdx].i, available[neighborIdx].j] = neighborIdx;
                        available.RemoveAt(last);
                        indexInList[ni, nj] = -1;
                    }
                }
            }
        }

    }

    public void GenerateEvents(int[,] cells, int battleCounts, int campCounts, int shopCounts, GameRandom rnd)
    {
        // 1. Клетки-кандидаты
        List<(int i, int j)> pool = new List<(int i, int j)>();
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                if (cells[i, j] == 1)
                    pool.Add((i, j));

        if (pool.Count == 0) return;

        // 2. Прогресс клетки (0 — старт, 1 — конец). Если старт не на j = 0 — поменяй.
        float Progress((int i, int j) c) => height > 1 ? (float)c.j / (height - 1) : 0f;

        // 3. Кривые веса. Ненулевые на всём диапазоне — событие может выпасть где угодно.
        float BattleWeight(float p) => Mathf.Lerp(0.90f, 0.25f, Mathf.Clamp01(p));
        float ShopWeight(float p) => Mathf.Lerp(0.20f, 0.70f, Mathf.Clamp01(p));
        float CampWeight(float p) => Mathf.Lerp(0.15f, 0.65f, Mathf.Clamp01(p));

        // 4. Режем карту на полосы прогресса. 4..12 полос — разумный компромисс.
        int bands = Mathf.Clamp(pool.Count / 4, 4, 12);

        var bandCells = new List<(int i, int j)>[bands];
        for (int b = 0; b < bands; b++) bandCells[b] = new List<(int, int)>();
        foreach (var c in pool)
        {
            int b = Mathf.Min(bands - 1, (int)(Progress(c) * bands));
            bandCells[b].Add(c);
        }

        // 5. Веса полос (вес кривой в середине полосы × число клеток в полосе).
        float[] wBattle = new float[bands];
        float[] wShop = new float[bands];
        float[] wCamp = new float[bands];
        for (int b = 0; b < bands; b++)
        {
            float p = (b + 0.5f) / bands;
            int n = bandCells[b].Count;
            wBattle[b] = BattleWeight(p) * n;
            wShop[b] = ShopWeight(p) * n;
            wCamp[b] = CampWeight(p) * n;
        }

        // 6. Квоты на каждую полосу (с округлением, сохраняющим сумму).
        int[] qBattle = AllocateCounts(wBattle, battleCounts);
        int[] qShop = AllocateCounts(wShop, shopCounts);
        int[] qCamp = AllocateCounts(wCamp, campCounts);

        // 7. Раскладываем события внутри каждой полосы в случайном порядке.
        for (int b = 0; b < bands; b++)
        {
            var list = bandCells[b];
            Shuffle(list, rnd);

            int idx = 0;
            for (int k = 0; k < qBattle[b] && idx < list.Count; k++, idx++)
                cells[list[idx].i, list[idx].j] = 1;
            for (int k = 0; k < qShop[b] && idx < list.Count; k++, idx++)
                cells[list[idx].i, list[idx].j] = 2;
            for (int k = 0; k < qCamp[b] && idx < list.Count; k++, idx++)
                cells[list[idx].i, list[idx].j] = 3;
        }
    }

    // Распределяет total единиц по полосам пропорционально weights,
    // с округлением по методу наибольших остатков (сумма точно равна total).
    private static int[] AllocateCounts(float[] weights, int total)
    {
        int n = weights.Length;
        int[] q = new int[n];
        float sum = 0f;
        for (int i = 0; i < n; i++) sum += weights[i];
        if (sum <= 0f || total <= 0) return q;

        float[] frac = new float[n];
        int assigned = 0;
        for (int i = 0; i < n; i++)
        {
            float exact = weights[i] / sum * total;
            q[i] = Mathf.FloorToInt(exact);
            frac[i] = exact - q[i];
            assigned += q[i];
        }

        int remaining = total - assigned;
        int[] idx = new int[n];
        for (int i = 0; i < n; i++) idx[i] = i;
        System.Array.Sort(idx, (a, b) => frac[b].CompareTo(frac[a]));
        for (int i = 0; i < remaining && i < n; i++) q[idx[i]]++;

        return q;
    }

    private static void Shuffle<T>(List<T> list, GameRandom rnd)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.GetRandomInt(0, i + 1); // [0, i]
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void Print2DArray(int[,] matrix)
    {
        StringBuilder sb = new StringBuilder();
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                // Формат {0, 4} задает ширину ячейки в 4 символа для выравнивания
                sb.Append(string.Format("{0, 4} ", matrix[i, j]));
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }
    public static void MirrorHorizontal(int[,] cells)
    {
        int rows = cells.GetLength(0);
        int cols = cells.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols / 2; j++)
            {
                int tmp = cells[i, j];
                cells[i, j] = cells[i, cols - 1 - j];
                cells[i, cols - 1 - j] = tmp;
            }
        }
    }
}

