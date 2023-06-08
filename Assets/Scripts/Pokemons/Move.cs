using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Unlikely to need change

public class Move
{
    public MoveBase Base { get; set; }
    public int PP { get; set; }

    public Move(MoveBase pBase)
    {
        Base = pBase;
        PP = pBase.PP;
    }
}

//Reference: Pokemon in Unity Series on Youtube (Game Dev Experiments)