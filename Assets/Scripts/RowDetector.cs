using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class RowDetector : MonoBehaviour
{
    private Collider2D[] _colliders;
    public int Size { get; private set; }

    void Start()
    {
        Size = (int)(GetComponent<BoxCollider2D>().size.x);
        _colliders = new Collider2D[Size];
    }

    public IEnumerable<GameObject>? DetectRow()
    {
        var res = GetComponent<BoxCollider2D>().OverlapCollider(new ContactFilter2D(), _colliders);
        if (res != Size)
        {
            return null;
        }
        return from collider in _colliders select collider.gameObject;
    }
}
