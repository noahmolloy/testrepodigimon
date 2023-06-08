using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Unlikely to be changed

[System.Serializable]

public class Dialog
{
    [SerializeField] List<string> lines;
    public List<string> Lines
    {
        get { return lines; }
    }
 }

//Reference: Pokemon in Unity Series on Youtube (Game Dev Experiments)