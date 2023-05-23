#nullable enable

using Pixelplacement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class GameController : MonoBehaviour
{
    public TetrominoGenerator? Generator;
    public RowDetector[]? RowDetectors;
    public GameObject? SavedTetrominoDisplay;
    public GameObject? NextTetrominoPreviewDisplay;
    public GameObject? JumpOutAnchor;
    public MusicTempoController? MusicTempoController;
    public MusicTimer? MusicTimer;

    public Tetromino? CurrentTetromino { get; private set; }
    public GameObject? CurrentTetrominoPrefab { get; private set; }
    public GameObject? NextTetrominoPrefab { get; private set; }
    private GameObject? _savedTetromino;
    private GameObject? _displayedSavedTetromino;
    public GameObject? SavedTetrominoPrefab {
        get => _savedTetromino;
        private set
        {
            _savedTetromino = value;
            if (value)
            {
                _displayedSavedTetromino = ShowTetrominoInDisplay(value!, SavedTetrominoDisplay!);
            }
        }
    }

    private bool _tetrominoSavedSinceLastSpawn = false;

    public void Start()
    {
        RowDetectors = GetComponentsInChildren<RowDetector>();
        StartCoroutine(StartGame());
    }
    public IEnumerator StartGame()
    {
        yield return MusicTimer!.WaitUntilNextBeat();
        yield return GenerateNextTetromino(waitForAnimation: true);
        yield return StartNextTurn();
    }

    public IEnumerator StartNextTurn()
    {
        Debug.Assert(NextTetrominoPrefab, "StartNextTurn() called with no next tetromino.");
        Debug.Assert(MusicTimer!.IsOnBeat());

        float jumpInDuration = MusicTimer!.TimeToNearestBeat(35 / 60f);

        GameObject nextTetromino = NextTetrominoPreviewDisplay!.transform.GetChild(0).gameObject;
        GameObject nextTetrominoPrefab = NextTetrominoPrefab!;
        Animation spawnAnimation = nextTetromino.GetComponent<Animation>();

        // Slide next tetromino in and jump the old one onto the board simultaneausly.
        yield return GenerateNextTetromino(slideInDuration: jumpInDuration);
        yield return spawnAnimation.PlayAndWaitForEvent("JumpIn", eventIndex: 0, durationUntilEvent: jumpInDuration);
        yield return MusicTimer!.WaitUntilNextBeat();

        Destroy(nextTetromino);
        Tetromino spawned = Spawn(nextTetrominoPrefab);
        // Wait for spawned tetromino to initialize.
        yield return null;
        // We just waited until the beat, so the Tetromino should move now instead of waiting for another beat.
        yield return spawned.MoveIfPossible(Vector2.down);
    }

    public IEnumerator GenerateNextTetromino(bool waitForAnimation = false, float? slideInDuration = null)
    {
        NextTetrominoPrefab = Generator!.GetRandomTetrominoPrefab();
        GameObject displayed = ShowTetrominoInDisplay(NextTetrominoPrefab, NextTetrominoPreviewDisplay!, destroyOld: false);
        Animation animation = displayed.GetComponent<Animation>();
        yield return animation.PlayAndWaitForStart("SlideFromOffscreen", duration: slideInDuration ?? MusicTimer!.TimeToNearestBeat(5 / 6f));
        if (waitForAnimation)
        {
            yield return animation.WhilePlaying();
        }
    }

    GameObject ShowTetrominoInDisplay(GameObject tetrominoPrefab, GameObject display, bool destroyOld = true)
    {
        if (destroyOld)
        {
            var children = (from Transform child in display.transform select child.gameObject).ToArray();
            foreach (var child in children)
            {
                Destroy(child);
            }
        }
        return Instantiate(tetrominoPrefab.GetComponent<Tetromino>().Body, display.transform)!;
    }


    public Tetromino Spawn(GameObject? Prefab = null)
    {
        if (CurrentTetrominoPrefab)
        {
            CancelTurn();
        }
        (CurrentTetromino, CurrentTetrominoPrefab) = Generator!.Generate(Prefab);
        _tetrominoSavedSinceLastSpawn = false;
        return CurrentTetromino;
    }

    public void CancelTurn()
    {
        if (!CurrentTetromino)
        {
            return;
        }
        Destroy(CurrentTetromino!.gameObject);
        CurrentTetrominoPrefab = null;
        CurrentTetromino = null;
    }

    public void DecomposeTetromino(Tetromino tetromino)
    {
        Transform[] blocks = (from Transform block in tetromino.Body!.transform select block).ToArray();
        foreach (Transform block in blocks)
        {
            block.SetParent(this.transform);
        }
        Destroy(tetromino.gameObject);
    }

    public IEnumerable<GameObject> AllBlocks()
    {
        return (from Transform child in transform where child.tag == "Block" select child.gameObject);
    }

    public void OnTetrominoTermination(Tetromino tetromino)
    {
        StartCoroutine(ProcessTetrominoTermination(tetromino));
    }

    private IEnumerator ProcessTetrominoTermination(Tetromino tetromino)
    { 
        DecomposeTetromino(tetromino);
        CurrentTetromino = null;
        CurrentTetrominoPrefab = null;
        yield return new WaitForFixedUpdate();
        var destroyedRows = new List<Tuple<RowDetector, IEnumerable<GameObject>>> ();
        foreach (RowDetector rowDetector in RowDetectors!)
        {
            var blocks = rowDetector.DetectRow();
            if (blocks == null)
            {
                continue;
            }
            destroyedRows.Add(Tuple.Create(rowDetector, blocks));
        }
        foreach (var row in destroyedRows)
        {
            foreach (GameObject block in row.Item2)
            {
                Destroy(block);
            }
        }
        foreach (GameObject block in AllBlocks())
        {
            int rowsDestroyedBelow = (from row in destroyedRows where row.Item1.transform.position.y < block.transform.position.y select row).Count();
            if (rowsDestroyedBelow != 0)
            {
                block.transform.position -= new Vector3(0, rowsDestroyedBelow * Tetromino.BlockSize);
            }
        }
        if (destroyedRows.Count() > 0)
        {
            MusicTempoController!.UpTempo();
        }
        yield return StartCoroutine(StartNextTurn());
        yield break;
    }

    private IEnumerator AnimateTetrominoSave(GameObject newSaved, GameObject? oldSaved = null)
    {
        yield return MusicTimer!.WaitUntilNextBeat();
        GameObject newSavedBody = newSaved.transform.GetChild(0).gameObject;
        var fromPosition = newSaved.transform.position;
        var toPosition = JumpOutAnchor!.transform.position;
        var fromRotation = newSaved.transform.rotation;
        var toRotation = JumpOutAnchor!.transform.rotation;
        var xCurve = Tween.EaseIn;
        var yCurve = Tween.EaseOut;
        var rotationCurve = Tween.EaseOut;
        float jumpOutDuration = MusicTimer!.TimeToNearestBeat(.7f);
        Debug.Log(jumpOutDuration);
        yield return Utils.Animate(
            duration: jumpOutDuration * (.2f / .7f),
            t => newSaved.transform.position = new Vector3(
                Mathf.Lerp(fromPosition.x, toPosition.x, xCurve.Evaluate(t)),
                Mathf.Lerp(fromPosition.y, toPosition.y, yCurve.Evaluate(t))
            ),
            t => newSavedBody.transform.rotation = Quaternion.Lerp(fromRotation, toRotation, rotationCurve.Evaluate(t))
        );
        newSaved.transform.SetParent(SavedTetrominoDisplay!.transform);
        newSaved.transform.localPosition = Vector3.zero;
        newSavedBody.transform.rotation = Quaternion.identity;
        Animation newSavedAnimation = newSavedBody.GetComponent<Animation>();
        yield return newSavedAnimation.PlayUntilEnd("JumpOut", duration: jumpOutDuration * (.5f / .7f));
        yield return newSavedAnimation.PlayAndWaitForStart("SlideToSaved", duration: MusicTimer!.TimeToNearestBeat(.5f));
        if (oldSaved)
        {
            yield return oldSaved!.GetComponent<Animation>().PlayAndWaitForEvent("JumpInFromSaved", eventIndex: 0, durationUntilEvent: MusicTimer!.TimeToNearestBeat(34 / 60f));
        }
    }


    private IEnumerator SaveTetromino(Action? then = null)
    {
        if (!CurrentTetrominoPrefab) {
            Debug.LogWarning("CurrentTetrominoPrefab was null when trying to save tetromino");
            yield break;
        }
        GameObject newSaved = CurrentTetrominoPrefab!;
        CurrentTetromino!.GetComponent<Tetromino>().enabled = false;
        yield return AnimateTetrominoSave(CurrentTetromino!.gameObject, _displayedSavedTetromino);
        if (!SavedTetrominoPrefab)
        {
            yield return StartNextTurn();
        } else
        {
            Tetromino spawned = Spawn(SavedTetrominoPrefab);
            // Wait for Start() to be called on Tetromino
            yield return null;
            // We just waited until the beat, so the Tetromino should move now instead of waiting for another beat.
            yield return spawned.MoveIfPossible(Vector2.down);
        }
        SavedTetrominoPrefab = newSaved;
        then?.Invoke();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!_tetrominoSavedSinceLastSpawn)
            {
                StartCoroutine(SaveTetromino(then: () => _tetrominoSavedSinceLastSpawn = true));
            } else
            {
                Debug.Log("Skipping save since already saved since last spawn");
            }
        }
    }
}
