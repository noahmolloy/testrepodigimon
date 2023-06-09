using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { FreeRoam, Battle, Dialog, Cutscene, Paused }



public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;
    GameState state;
    GameState stateBeforePause;

    public static GameController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ConditionsDB.Init(); //initializes the conditions and status effects data base
    }

    private void Start()
    {
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

    public void PauseGame(bool pause)
    {
        if(pause)
        {
            stateBeforePause = state;
            state = GameState.Paused;
        }
        else
        {
            state = stateBeforePause;
        }
    }

    //begins a battle
    public void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true); //sets active camera to the arena view
        worldCamera.gameObject.SetActive(false); //deactivates overworld camera view

        var playerParty = playerController.GetComponent<PokemonParty>(); //assigns pokemon to the player's party from player data
        var wildPokemon = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildPokemon(); //populates long grass with wild pokemon to fight

        var wildPokemonCopy = new Pokemon(wildPokemon.Base, wildPokemon.Level); //prevents issue where you'd assign the pokemon from the wild 


        battleSystem.StartBattle(playerParty, wildPokemonCopy);
    }

    TrainerController trainer;

    public void StartTrainerBattle(TrainerController trainer)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true); //sets active camera to the arena view
        worldCamera.gameObject.SetActive(false); //deactivates overworld camera view

        this.trainer = trainer;
        var playerParty = playerController.GetComponent<PokemonParty>();
        var trainerParty = trainer.GetComponent<PokemonParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }

    public void OnEnterTrainersView(TrainerController trainer)
    {
        state = GameState.Cutscene;
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }

    void EndBattle(bool won)
    {
        if(trainer != null && won == true)
        {
            trainer.BattleLost();
            trainer = null;
        }

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

//Reference: Pokemon in Unity Series on Youtube (Game Dev Experiments)