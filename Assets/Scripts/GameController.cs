using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { FreeRoam, Battle, Dialog }



public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;
    GameState state;

    private void Awake()
    {
        ConditionsDB.Init(); //initializes the conditions and status effects data base
    }

    private void Start()
    {
        playerController.OnEncountered += StartBattle;
        battleSystem.OnBattleOver += EndBattle;

        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.Dialog;
        };
        DialogManager.Instance.OnCloseDialog += () =>
        {
            if(state == GameState.Dialog)
                state = GameState.FreeRoam;
        };
    }

    //begins a battle
    void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true); //sets active camera to the arena view
        worldCamera.gameObject.SetActive(false); //deactivates overworld camera view

        var playerParty = playerController.GetComponent<PokemonParty>(); //assigns pokemon to the player's party from player data
        var wildPokemon = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildPokemon(); //populates long grass with wild pokemon to fight
        battleSystem.StartBattle(playerParty, wildPokemon);
    }

    void EndBattle(bool won)
    {
        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false); //deactivates battle camera
        worldCamera.gameObject.SetActive(true); //sets active camera to overworld 
    }

    //changes the gamestate to reflect whatever state the user is in
    private void Update()
    {
        if(state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();
        }
        else if(state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
        else if(state == GameState.Dialog)
        {
            DialogManager.Instance.HandleUpdate();
        }
    }
}
