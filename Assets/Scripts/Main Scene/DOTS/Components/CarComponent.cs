﻿using Unity.Entities;
using System;
using UnityEngine;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

[Serializable]
[GenerateAuthoringComponent]
public struct CarComponent : IComponentData
{
    /// <summary>
    /// Indentification id that always stays on this car 
    /// </summary>
    public int ID;

    /// <summary>
    /// Car speed, subtracted from player's speed for reflecting current player speed
    /// </summary>
    public float Speed;

    /// <summary>
    /// Represents the current steering angle of the wheel, 0 means no rotation.
    /// </summary>
    public float SteeringIndex;
    
    public bool IsBraking;

    /// <summary>
    /// Data for the box collider surrounding this object. 
    /// Feels less accurate than <see cref="CapsuleColliderData"/> but this can be re-visted
    /// </summary>
    public Vector3 CubeColliderSize;

    /// <summary>
    /// Is disabled is for cars that are out of player's sight
    /// </summary>
    public bool IsDisabled;

    /// <summary>
    /// For cars that did hit other object on the road.
    /// </summary>
    public bool IsCollided;
    
    /// <summary>
    /// Data for the capsule collider surrounding this car.
    /// </summary>
    public CapsuleColliderData CapsuleColliderData;
    
    /// <summary>
    /// <see cref="ID"/> of <see cref="CarComponent"/> in close call
    /// </summary>
    public int CarInCloseCall;


    public MoveDirection CarDirection;
}