﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    private void Awake()
    {
        PlayerSpawnerSystem.AddSpawnPoint(transform);
    }

    private void OnDestroy()
    {
        PlayerSpawnerSystem.RemoveSpawnPoint(transform);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 1f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}
