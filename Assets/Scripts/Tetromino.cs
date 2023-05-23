#nullable enable

using System.Collections;
using UnityEngine;
using Pixelplacement;
using System;
using static Utils;

public class Tetromino : MonoBehaviour
{
    public GameController? Controller;
    public GameObject? Body;
    public MusicTimer? FallTimer;
    private ContactFilter2D CollisionFilter;

    public void Start()
    {
        Controller = FindObjectOfType<GameController>();
        CollisionFilter = new ContactFilter2D();
        CollisionFilter.SetLayerMask(new LayerMask() { value = LayerMask.GetMask("Default") });
        FallTimer = GameObject.Find("music").GetComponent<MusicTimer>();
    }

    // Delay from when the Tetromino touches the ground until the turn ends, even if the player keeps moving the block.
    public const float HardTerminateDelaySeconds = 3;

    // Tetromino normal fall rate.
    public const float FallRateSeconds = 1;

    // Tetromino accelerated fall rate (when pressing DOWN).
    public const float FastFallRateSeconds = .025f;

    // How often to consider prolonged presses of right and left as separate inputs.
    public const float InputRepeatRateSeconds = .1f;

    // X and Y size of each block.
    public const float BlockSize = 1;

    private bool _locked = false;
    private int _lockWaitCount = 0;
    private bool _fastFalling = false;
    private int _fastFallingCount = 0;
    private readonly Timer HardTerminateTimer = new Timer(HardTerminateDelaySeconds, false);
    private readonly Timer SoftTerminateTimer = new Timer(FallRateSeconds, false);
    private readonly Timer RepeatedInputTimer = new Timer(InputRepeatRateSeconds);

    public void Update()
    {
        // Move left
        bool RepeatedInputTimerTick = RepeatedInputTimer.OnUpdate() > 0;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StartCoroutine(MoveIfPossible(Vector2.left));
            RepeatedInputTimer.Reset();
        }
        else if (RepeatedInputTimerTick && Input.GetKey(KeyCode.LeftArrow))
        {
            StartCoroutine(MoveIfPossible(Vector2.left));
        }

        // Move right
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartCoroutine(MoveIfPossible(Vector2.right));
            RepeatedInputTimer.Reset();
        }
        else if (RepeatedInputTimerTick && Input.GetKey(KeyCode.RightArrow))
        {
            StartCoroutine(MoveIfPossible(Vector2.right));
        }

        // Downward acceleration
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _fastFallingCount = 1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            _fastFalling = true;
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            _fastFalling = false;
        }

        // Rotate
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(RotateIfPossible());
        }

        // Fall
        int FallTimerTicks = FallTimer!.Ticks;
        if (FallTimerTicks != 0)
        {
            StartCoroutine(MoveAtMost(
                direction: Vector2.down,
                atMostCount: _fastFalling ? 1 << _fastFallingCount : FallTimerTicks,
                amountMoved: new Promise<int>().Then(amountFell => {
                    if (amountFell != 0 && _fastFalling)
                    {
                        _fastFallingCount++;
                    }
                })
            ));
        }

        // Soft terminate - terminate if the Tetromino is touching something below and hasn't been moved for a certain
        // period. This allows players to still move the Tetromino after it has been touched down, but if they don't
        // want to - terminates the Tetromino.
        bool SoftTerminateTimerTick = SoftTerminateTimer.OnUpdate() > 0;
        if (SoftTerminateTimerTick)
        {
            StartCoroutine(Terminate());
        }

        // Hard terminate - terminate if the block has been touching something below for a certain period, regardless
        // of movement. This prevents players from moving the piece endlessly left and right across the floor.
        bool HardTerminateTimerTick = HardTerminateTimer.OnUpdate() > 0;
        if (HardTerminateTimerTick)
        {
            StartCoroutine(Terminate());
        }
    }

    // Check if the collider is not part of this Tetromino, meaning it is either a wall or an already laid-down Block.
    bool IsExternalCollider(Collider2D collider)
    {
        var potentialBlock = collider;
        var potentialBody = potentialBlock.transform.parent?.gameObject;
        var potentialTetromino = potentialBody?.transform?.parent?.gameObject;
        return potentialTetromino != gameObject;
    }

    private readonly RaycastHit2D[] _hits = new RaycastHit2D[3];

    bool CanMove(Vector2 displacement)
    {
        var magnitude = displacement.magnitude;
        foreach (var collider in Body!.GetComponentsInChildren<BoxCollider2D>())
        {
            int collisions = collider.Raycast(displacement, CollisionFilter, _hits, magnitude);
            for (int i = 0; i < collisions; ++i)
            {
                if (IsExternalCollider(_hits[i].collider))
                {
                    return false;
                }
            }
        }
        return true;
    }


    float DefaultSmoothMovementDuration()
    {
        return Math.Max(.001f, Math.Min(.1f, FallTimer!.TimeToNextBeat()));
    }


    public IEnumerator MoveIfPossible(Vector2 direction, Promise<bool>? moved = null)
    {
        var lockScope = new Promise<LockScope>();
        yield return AcquireLock(lockScope);
        using (lockScope.Value)
        {
            if (!CanMove(direction))
            {
                moved?.Resolve(false);
                yield break;
            }
            yield return Move(direction);
            moved?.Resolve(true);
        }
    }


    public IEnumerator MoveAtMost(Vector2 direction, int atMostCount, Promise<int>? amountMoved = null)
    {
        var lockScope = new Promise<LockScope>();
        yield return AcquireLock(lockScope);
        using (lockScope.Value)
        {
            int movement = atMostCount;
            Vector2 offset = default;
            while (movement != 0 && !CanMove(offset = direction * movement)) {
                movement--;
            }
            if (movement == 0)
            {
                amountMoved?.Resolve(movement);
                yield break;
            }
            yield return Move(offset);
            amountMoved?.Resolve(movement);
        }
    }

    public IEnumerator Move(Vector2 direction)
    {
        Debug.Assert(_locked);
        yield return SmoothMovement(new Vector3(direction.x, direction.y));
        yield return OnMoveFinished();
    }

    public class LockScope : System.IDisposable
    {
        public Tetromino _tetromino;

        public LockScope(Tetromino tetromino)
        {
            Debug.Assert(!tetromino._locked, "Double lock attempt");
            _tetromino = tetromino;
            _tetromino._locked = true;
            _tetromino.HardTerminateTimer.Pause();
            _tetromino.SoftTerminateTimer.Pause();
            _tetromino.FallTimer!.Pause();
            _tetromino.RepeatedInputTimer.Pause();
        }


        public void Dispose()
        {
            _tetromino._locked = false;
            _tetromino.HardTerminateTimer.Resume();
            _tetromino.SoftTerminateTimer.Resume();
            _tetromino.FallTimer!.Resume();
            _tetromino.RepeatedInputTimer.Resume();
        }
    }

    IEnumerator SmoothMovement(Vector3 offset, float? rotateAngle = null, float? duration = null)
    {
        Debug.Assert(_locked);
        duration ??= DefaultSmoothMovementDuration();
        var positionBefore = transform.position;
        var targetPosition = positionBefore + offset;
        var rotationBefore = Body!.transform.localRotation;
        yield return Utils.Animate(
            duration.Value,
            Tween.EaseOutBack,
            shouldCancel: () => _lockWaitCount > 0,
            t => transform.position = Vector3.Lerp(positionBefore, targetPosition, t),
            t =>
            {
                if (rotateAngle.HasValue)
                {
                    Body!.transform.localRotation = rotationBefore * Quaternion.Euler(0, 0, t * rotateAngle.Value);
                }
            }
        );
    }

    IEnumerator OnMoveFinished()
    {
        Debug.Assert(_locked);
        yield return new WaitForFixedUpdate();
        if (CanMove(Vector2.down))
        {
            HardTerminateTimer.Reset(stop: true);
            SoftTerminateTimer.Reset(stop: true);
            yield break;
        }
        // Hard terminate timer is only reset if it stops touching down (terminate even if player keeps moving).
        if (!HardTerminateTimer.Running)
        {
            HardTerminateTimer.Start();
        }
        // Soft timer is reset whenever the player moves (will only tick if player decides to stop moving).
        SoftTerminateTimer.Start();
    }

    // Terminate this tetromino, letting the GameController break it down and spawn another one.
    IEnumerator Terminate()
    {
        yield return FallTimer!.WaitUntilNextBeat();
        Controller!.OnTetrominoTermination(this);
    }

    IEnumerator Rotate(Vector3 displacement)
    {
        Debug.Assert(_locked);
        yield return SmoothMovement(displacement, rotateAngle: 90);
        yield return OnMoveFinished();
    }

    private readonly Vector3[] _rotationDisplacements = new Vector3[] {
        new Vector3( 0, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 2, 0),
        new Vector3( 1, 0, 0),
        new Vector3(-1, 0, 0),
        new Vector3(2, 0, 0),
        new Vector3(-2, 0, 0),
        new Vector3(3, 0, 0),
        new Vector3(-3, 0, 0)
    };

    private readonly Collider2D[] _colliders = new Collider2D[5];

    IEnumerator CanRotate(Promise<bool> canRotate, Promise<Vector3> displacement)
    {
        Debug.Assert(_locked);
        // Check if we can rotate by copying a transparent version of the body, rotating it and trying to find a short
        // displacement that makes it not overlap any other block or wall.
        var rotationChecker = Instantiate(Body, this.transform);
        foreach (SpriteRenderer sprite in rotationChecker!.GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.enabled = false;
        }
        try
        {
            rotationChecker.transform.Rotate(0, 0, 90);
            foreach (Vector3 possibleDisplacement in _rotationDisplacements)
            {
                if (possibleDisplacement.x != 0 || possibleDisplacement.y != 0)
                {
                    rotationChecker.transform.localPosition = possibleDisplacement;
                }
                yield return new WaitForFixedUpdate();
                bool overlap = false;
                foreach (var collider in rotationChecker.GetComponentsInChildren<BoxCollider2D>())
                {
                    int colliderCount = collider.OverlapCollider(CollisionFilter, _colliders);

                    for (int j = 0; j < colliderCount; ++j)
                    {
                        if (IsExternalCollider(_colliders[j]))
                        {
                            overlap = true;
                            break;
                        }
                    }
                }
                if (!overlap)
                {
                    canRotate.Resolve(true);
                    displacement.Resolve(possibleDisplacement);
                    yield break;
                }
            }
        }
        finally
        {
            DestroyImmediate(rotationChecker);
        }
        canRotate.Resolve(false);
    }

    IEnumerator AcquireLock(Promise<LockScope> lockScope)
    {
        _lockWaitCount++;
        while (_locked)
        {
            yield return new WaitForFixedUpdate();
        }
        _lockWaitCount--;
        lockScope.Resolve(new LockScope(this));
    }

    public IEnumerator RotateIfPossible(Promise<bool>? rotated = null)
    {
        var lockScope = new Promise<LockScope>();
        yield return AcquireLock(lockScope);
        using (lockScope.Value)
        {
            var canRotate = new Promise<bool>();
            var displacement = new Promise<Vector3>();
            yield return CanRotate(canRotate, displacement);
            if (!canRotate.Value)
            {
                rotated?.Resolve(false);
                yield break;
            }
            yield return Rotate(displacement.Value);
            rotated?.Resolve(true);
        }
    }
}
