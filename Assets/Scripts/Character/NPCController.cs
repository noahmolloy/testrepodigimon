using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Unlikely needs change

public class NPCController : MonoBehaviour, Interactable
{
    [SerializeField] Dialog dialog;

    public void Interact()
    {
        Debug.Log("Interacting with an NPC");
        StartCoroutine(DialogManager.Instance.ShowDialog(dialog));
    }
}
