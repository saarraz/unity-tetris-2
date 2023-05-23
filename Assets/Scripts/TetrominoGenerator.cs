using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrominoGenerator : MonoBehaviour
{
    public GameObject[] TetrominoPrefabs;


    public GameObject GetRandomTetrominoPrefab()
    {
        return TetrominoPrefabs[UnityEngine.Random.Range(0, TetrominoPrefabs.Length)];
    }


    public Tuple<Tetromino, GameObject> Generate(GameObject Prefab = null)
    {
        if (!Prefab)
        {
            Prefab = GetRandomTetrominoPrefab();
        }
        return Tuple.Create(Instantiate(Prefab, transform.position, Quaternion.identity).GetComponent<Tetromino>(), Prefab);
    }
}
