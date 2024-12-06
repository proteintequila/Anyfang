using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int width = 7;
    public int height = 7;
    public float timeLimitInSeconds = 60f;
    public GameObject[] fruitPrefabs;
    public float dropSpeed = 5f;
    public int scorePerMatch = 100;

    public GameObject[,] allFruits;
    public float remainingTime;
    public bool isGameActive = true;
    public int currentScore = 0;

    public GameObject selectedFruit = null;
    public Vector2Int selectedPosition;

    private Vector2 centerOffset;

    public float matchDisplayDuration = 0.8f;
    public Color matchHighlightColor = Color.red;

    private bool isProcessingMatches = false;

    void Start()
    {
        allFruits = new GameObject[width, height];
        remainingTime = timeLimitInSeconds;

        centerOffset = new Vector2(
            -(width - 1) / 2f,
            -(height - 1) / 2f
        );

        InitializeBoard();
    }

    private void InitializeBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateFruitWithoutMatch(x, y);
            }
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0)
        {
            GameOver();
            return;
        }

        if (isProcessingMatches) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 adjustedPos = mousePos - centerOffset;
            Vector2Int gridPosition = Vector2Int.RoundToInt(adjustedPos);

            int arrayX = gridPosition.x;
            int arrayY = gridPosition.y;

            if (IsValidPosition(arrayX, arrayY))
            {
                HandleFruitSelection(arrayX, arrayY);
            }
        }
    }

    private void HandleFruitSelection(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        GameObject clickedFruit = allFruits[x, y];

        if (selectedFruit == null)
        {
            selectedFruit = clickedFruit;
            selectedPosition = new Vector2Int(x, y);
            selectedFruit.GetComponent<FruitSelectionEffect>()?.Select();
        }
        else
        {
            Vector2Int newPosition = new Vector2Int(x, y);
            if (IsAdjacent(selectedPosition, newPosition))
            {
                SwapFruits(selectedPosition, newPosition);
            }
            else
            {
                selectedFruit.GetComponent<FruitSelectionEffect>()?.Deselect();
                selectedFruit = clickedFruit;
                selectedPosition = new Vector2Int(x, y);
                selectedFruit.GetComponent<FruitSelectionEffect>()?.Select();
            }
        }
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y) == 1;
    }

    private void SwapFruits(Vector2Int pos1, Vector2Int pos2)
    {
        if (!IsValidPosition(pos1.x, pos1.y) || !IsValidPosition(pos2.x, pos2.y)) return;
        if (isProcessingMatches) return;

        GameObject temp = allFruits[pos1.x, pos1.y];
        allFruits[pos1.x, pos1.y] = allFruits[pos2.x, pos2.y];
        allFruits[pos2.x, pos2.y] = temp;

        UpdateFruitPosition(pos1.x, pos1.y);
        UpdateFruitPosition(pos2.x, pos2.y);

        bool hasMatch1 = CheckMatch(pos1.x, pos1.y);
        bool hasMatch2 = CheckMatch(pos2.x, pos2.y);

        if (!hasMatch1 && !hasMatch2)
        {
            StartCoroutine(SwapBackAfterDelay(pos1, pos2));
            selectedFruit.GetComponent<FruitSelectionEffect>()?.Deselect();
            selectedFruit = null;
        }
        else
        {
            selectedFruit?.GetComponent<FruitSelectionEffect>()?.Deselect();
            selectedFruit = null;
        }
    }

    private IEnumerator SwapBackAfterDelay(Vector2Int pos1, Vector2Int pos2)
    {
        yield return new WaitForSeconds(0.5f);

        GameObject temp = allFruits[pos1.x, pos1.y];
        allFruits[pos1.x, pos1.y] = allFruits[pos2.x, pos2.y];
        allFruits[pos2.x, pos2.y] = temp;

        UpdateFruitPosition(pos1.x, pos1.y);
        UpdateFruitPosition(pos2.x, pos2.y);
    }

    private void CreateFruitWithoutMatch(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        List<int> availableTypes = new List<int>();
        for (int i = 0; i < fruitPrefabs.Length; i++)
        {
            availableTypes.Add(i);
        }

        if (x >= 2 && allFruits[x - 1, y] != null && allFruits[x - 2, y] != null)
        {
            int leftType = allFruits[x - 1, y].GetComponent<FruitType>().type;
            int leftType2 = allFruits[x - 2, y].GetComponent<FruitType>().type;
            if (leftType == leftType2)
            {
                availableTypes.Remove(leftType);
            }
        }

        if (y >= 2 && allFruits[x, y - 1] != null && allFruits[x, y - 2] != null)
        {
            int bottomType = allFruits[x, y - 1].GetComponent<FruitType>().type;
            int bottomType2 = allFruits[x, y - 2].GetComponent<FruitType>().type;
            if (bottomType == bottomType2)
            {
                availableTypes.Remove(bottomType);
            }
        }

        int randomIndex = availableTypes[Random.Range(0, availableTypes.Count)];
        GameObject newFruit = Instantiate(fruitPrefabs[randomIndex], GridToWorldPosition(x, y), Quaternion.identity);
        newFruit.transform.parent = transform;

        FruitType fruitType = newFruit.GetComponent<FruitType>();
        if (fruitType == null)
        {
            fruitType = newFruit.AddComponent<FruitType>();
        }

        fruitType.type = randomIndex;
        fruitType.x = x;
        fruitType.y = y;

        allFruits[x, y] = newFruit;
    }

    private void CreateFruit(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        int randomIndex = Random.Range(0, fruitPrefabs.Length);
        GameObject newFruit = Instantiate(fruitPrefabs[randomIndex], GridToWorldPosition(x, y), Quaternion.identity);
        newFruit.transform.parent = transform;

        FruitType fruitType = newFruit.GetComponent<FruitType>();
        if (fruitType == null)
        {
            fruitType = newFruit.AddComponent<FruitType>();
        }

        fruitType.type = randomIndex;
        fruitType.x = x;
        fruitType.y = y;

        allFruits[x, y] = newFruit;
    }

    private Vector3 GridToWorldPosition(int x, int y)
    {
        return new Vector3(x + centerOffset.x, y + centerOffset.y, 0);
    }

    private void UpdateFruitPosition(int x, int y)
    {
        if (allFruits[x, y] == null) return;

        GameObject fruit = allFruits[x, y];
        fruit.transform.position = GridToWorldPosition(x, y);

        FruitType ft = fruit.GetComponent<FruitType>();
        ft.x = x;
        ft.y = y;
    }

    private bool CheckMatch(int x, int y)
    {
        if (allFruits[x, y] == null) return false;

        List<GameObject> horizontalMatches = FindHorizontalMatches(x, y);
        List<GameObject> verticalMatches = FindVerticalMatches(x, y);

        bool hasMatch = (horizontalMatches.Count >= 3 || verticalMatches.Count >= 3);

        if (hasMatch)
        {
            List<GameObject> allMatches = new List<GameObject>();
            allMatches.AddRange(horizontalMatches);
            allMatches.AddRange(verticalMatches);
            HandleMatches(allMatches);
        }

        return hasMatch;
    }

    private List<GameObject> FindHorizontalMatches(int x, int y)
    {
        List<GameObject> matches = new List<GameObject>();
        GameObject startFruit = allFruits[x, y];
        if (startFruit == null) return matches;

        int fruitType = startFruit.GetComponent<FruitType>().type;
        List<GameObject> horizontalLine = new List<GameObject>();
        horizontalLine.Add(startFruit);

        // 왼쪽
        for (int i = x - 1; i >= 0; i--)
        {
            if (allFruits[i, y] != null && allFruits[i, y].GetComponent<FruitType>().type == fruitType)
                horizontalLine.Add(allFruits[i, y]);
            else
                break;
        }

        // 오른쪽
        for (int i = x + 1; i < width; i++)
        {
            if (allFruits[i, y] != null && allFruits[i, y].GetComponent<FruitType>().type == fruitType)
                horizontalLine.Add(allFruits[i, y]);
            else
                break;
        }

        if (horizontalLine.Count >= 3)
        {
            HashSet<GameObject> allConnected = new HashSet<GameObject>();
            foreach (var f in horizontalLine)
            {
                FruitType ft = f.GetComponent<FruitType>();
                FindConnectedFruits(ft.x, ft.y, fruitType, allConnected);
            }
            return new List<GameObject>(allConnected);
        }

        return new List<GameObject>();
    }

    private List<GameObject> FindVerticalMatches(int x, int y)
    {
        List<GameObject> matches = new List<GameObject>();
        GameObject startFruit = allFruits[x, y];
        if (startFruit == null) return matches;

        int fruitType = startFruit.GetComponent<FruitType>().type;
        List<GameObject> verticalLine = new List<GameObject>();
        verticalLine.Add(startFruit);

        // 아래
        for (int i = y - 1; i >= 0; i--)
        {
            if (allFruits[x, i] != null && allFruits[x, i].GetComponent<FruitType>().type == fruitType)
                verticalLine.Add(allFruits[x, i]);
            else
                break;
        }

        // 위
        for (int i = y + 1; i < height; i++)
        {
            if (allFruits[x, i] != null && allFruits[x, i].GetComponent<FruitType>().type == fruitType)
                verticalLine.Add(allFruits[x, i]);
            else
                break;
        }

        if (verticalLine.Count >= 3)
        {
            HashSet<GameObject> allConnected = new HashSet<GameObject>();
            foreach (var f in verticalLine)
            {
                FruitType ft = f.GetComponent<FruitType>();
                FindConnectedFruits(ft.x, ft.y, fruitType, allConnected);
            }
            return new List<GameObject>(allConnected);
        }

        return new List<GameObject>();
    }

    private void FindConnectedFruits(int x, int y, int targetType, HashSet<GameObject> matches)
    {
        if (!IsValidPosition(x, y)) return;
        if (allFruits[x, y] == null) return;

        FruitType ft = allFruits[x, y].GetComponent<FruitType>();
        if (ft.type != targetType) return;

        if (matches.Contains(allFruits[x, y])) return;

        matches.Add(allFruits[x, y]);

        FindConnectedFruits(x + 1, y, targetType, matches);
        FindConnectedFruits(x - 1, y, targetType, matches);
        FindConnectedFruits(x, y + 1, targetType, matches);
        FindConnectedFruits(x, y - 1, targetType, matches);
    }

    private void HandleMatches(List<GameObject> matches)
    {
        HashSet<GameObject> uniqueMatches = new HashSet<GameObject>(matches);
        StartCoroutine(HandleMatchesWithEffect(uniqueMatches));
    }

    private IEnumerator HandleMatchesWithEffect(HashSet<GameObject> matches)
    {
        isProcessingMatches = true; 

        foreach (GameObject match in matches)
        {
            if (match != null)
            {
                if (match == selectedFruit)
                {
                    selectedFruit.GetComponent<FruitSelectionEffect>()?.Deselect();
                    selectedFruit = null;
                }
                else
                {
                    match.GetComponent<FruitSelectionEffect>()?.Select();
                }
            }
        }

        yield return new WaitForSeconds(matchDisplayDuration);

        int vanishCount = matches.Count;

        foreach (GameObject match in matches)
        {
            if (match == null)
            {
                vanishCount--;
                continue;
            }

            FruitType ft = match.GetComponent<FruitType>();
            if (!IsValidPosition(ft.x, ft.y))
            {
                vanishCount--;
                continue;
            }

            LeanTween.scale(match, Vector3.zero, 0.3f).setEase(LeanTweenType.easeInBack);
            LeanTween.alpha(match, 0f, 0.3f).setOnComplete(() =>
            {
                allFruits[ft.x, ft.y] = null;
                Destroy(match);
                currentScore += scorePerMatch;
                vanishCount--;
                if (vanishCount == 0)
                {
                    StartCoroutine(DropFruits());
                }
            });
        }

        if (vanishCount == 0)
        {
            StartCoroutine(DropFruits());
        }
    }

    private IEnumerator DropFruits()
    {
        yield return new WaitForSeconds(0.3f);

        for (int x = 0; x < width; x++)
        {
            bool needCheck = true;
            while (needCheck)
            {
                needCheck = false;
                for (int y = 0; y < height - 1; y++)
                {
                    if (allFruits[x, y] == null)
                    {
                        for (int yAbove = y + 1; yAbove < height; yAbove++)
                        {
                            if (allFruits[x, yAbove] != null)
                            {
                                allFruits[x, y] = allFruits[x, yAbove];
                                allFruits[x, yAbove] = null;
                                UpdateFruitPosition(x, y);
                                needCheck = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allFruits[x, y] == null)
                {
                    CreateFruit(x, y);
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        bool newMatches = CheckAllMatches();

        if (!newMatches)
        {
            if (!HasPossibleMoves())
            {
                ShuffleBoard();

                if (!HasPossibleMoves())
                {
                    ShuffleBoard();
                }
            }
            isProcessingMatches = false;
        }
    }

    private bool CheckAllMatches()
    {
        bool foundMatch = false;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (CheckMatch(x, y))
                    foundMatch = true;
            }
        }
        return foundMatch;
    }

    private void GameOver()
    {
        isGameActive = false;
        Debug.Log($"Game Over! Final Score: {currentScore}");
    }

    private bool HasPossibleMoves()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 오른쪽 스왑 시도
                if (x < width - 1)
                {
                    if (WouldSwapCreateMatch(x, y, x + 1, y))
                        return true;
                }
                // 위쪽 스왑 시도
                if (y < height - 1)
                {
                    if (WouldSwapCreateMatch(x, y, x, y + 1))
                        return true;
                }
            }
        }
        return false;
    }

    private bool WouldSwapCreateMatch(int x1, int y1, int x2, int y2)
    {
        // 임시 스왑
        GameObject temp = allFruits[x1, y1];
        allFruits[x1, y1] = allFruits[x2, y2];
        allFruits[x2, y2] = temp;

        // 스왑 후 매칭 검사 (단순히 라인오브3 체크)
        bool result = LineOfThreeAt(x1, y1) || LineOfThreeAt(x2, y2);

        // 원복
        temp = allFruits[x1, y1];
        allFruits[x1, y1] = allFruits[x2, y2];
        allFruits[x2, y2] = temp;

        return result;
    }

    private bool LineOfThreeAt(int x, int y)
    {
        // 해당 위치를 중심으로 가로/세로 3개 이상 일치하는지 검사
        if (allFruits[x, y] == null) return false;
        FruitType ft = allFruits[x, y].GetComponent<FruitType>();
        int type = ft.type;

        // 가로 검사
        int horizCount = 1;
        // 왼쪽
        for (int i = x - 1; i >= 0; i--)
        {
            if (allFruits[i, y] != null && allFruits[i, y].GetComponent<FruitType>().type == type) horizCount++;
            else break;
        }
        // 오른쪽
        for (int i = x + 1; i < width; i++)
        {
            if (allFruits[i, y] != null && allFruits[i, y].GetComponent<FruitType>().type == type) horizCount++;
            else break;
        }
        if (horizCount >= 3) return true;

        // 세로 검사
        int vertCount = 1;
        // 아래
        for (int j = y - 1; j >= 0; j--)
        {
            if (allFruits[x, j] != null && allFruits[x, j].GetComponent<FruitType>().type == type) vertCount++;
            else break;
        }
        // 위
        for (int j = y + 1; j < height; j++)
        {
            if (allFruits[x, j] != null && allFruits[x, j].GetComponent<FruitType>().type == type) vertCount++;
            else break;
        }
        if (vertCount >= 3) return true;

        return false;
    }

    private void ShuffleBoard()
    {
        List<GameObject> fruitsList = new List<GameObject>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                fruitsList.Add(allFruits[x, y]);
            }
        }

        for (int i = 0; i < fruitsList.Count; i++)
        {
            GameObject temp = fruitsList[i];
            int rand = Random.Range(i, fruitsList.Count);
            fruitsList[i] = fruitsList[rand];
            fruitsList[rand] = temp;
        }

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                allFruits[x, y] = fruitsList[index];
                UpdateFruitPosition(x, y);
                index++;
            }
        }
    }
}
