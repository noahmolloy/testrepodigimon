using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Unlikely to need change

public class Condition
{
    public ConditionID Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string StartMessage { get; set; }

    public Func<Pokemon, bool> OnBeforeMove { get; set; }

    public Action<Pokemon> OnAfterTurn { get; set; }

    public Action<Pokemon> OnStart { get; set; }
}

//Reference: Pokemon in Unity Series on Youtube (Game Dev Experiments)