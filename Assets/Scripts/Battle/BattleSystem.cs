using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using DG.Tweening;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEditor.Scripting.Python;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, BattleOver }
public enum BattleAction { Move, SwitchPokemon, UseItem, Run }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject pokeballSprite;
    //test

    public event Action<bool> OnBattleOver;

    private GrammarRecognizer grammarRecognizer;

    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;
    bool aboutToUseChoice = true;

    /// <summary>
    /// mqtt stuff
    private string recog = String.Empty;
    private string visual = String.Empty;
    /// </summary>

    PokemonParty playerParty;
    PokemonParty trainerParty;
    Pokemon wildPokemon;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    int escapeAttempts;

    void Start()
    {
        grammarRecognizer = new GrammarRecognizer(Application.streamingAssetsPath + "/grammar.xml");
        grammarRecognizer.OnPhraseRecognized += RecognizedSpeech;
        grammarRecognizer.Start();
        Debug.Log("started speech");

        ////

        string[] mqtt_topic = { "Team-2/Digimon/recog", "Team-2/Digimon/visual" };
        byte[] mqtt_qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };

        MqttClient client = new MqttClient("mqtt.eclipseprojects.io");
        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        client.Connect("");
        client.Subscribe(mqtt_topic, mqtt_qosLevels);

        ///
        PythonRunner.RunFile($"{Application.dataPath}/visual_recog_mqtt_out.py");
    }

    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        if (e.Topic == "Team-2/Digimon/recog")
        {
            recog = System.Text.Encoding.UTF8.GetString(e.Message);
            Debug.Log(recog);
        }
        else if (e.Topic == "Team-2/Digimon/visual")
        {
            visual = System.Text.Encoding.UTF8.GetString(e.Message);
            Debug.Log(visual);
        }
    }

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon)
    {
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;
        player = playerParty.GetComponent<PlayerController>();

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;
        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        //clears HUDs
        playerUnit.Clear();
        enemyUnit.Clear();

        if(!isTrainerBattle)
        {
            //Wild Pokemon Battle 
            playerUnit.Setup(playerParty.GetHealthyPokemon());
            enemyUnit.Setup(wildPokemon);

            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);

            yield return StartCoroutine(dialogBox.TypeDialog($"A wild {enemyUnit.Pokemon.Base.Name} appeared."));
        }
        else
        {
            //Trainer Battle

            //Show trainer sprites
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);
            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogBox.TypeDialog($"{trainer.Name} wants to battle!");

            //Send out trainer pokemon
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyPokemon = trainerParty.GetHealthyPokemon();
            enemyUnit.Setup(enemyPokemon);
            yield return dialogBox.TypeDialog($"{trainer.Name} sends out {enemyPokemon.Base.Name}!");

            //send out player pokemon
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerPokemon = playerParty.GetHealthyPokemon();
            playerUnit.Setup(playerPokemon);
            yield return dialogBox.TypeDialog($"Go {playerPokemon.Base.Name}!");
            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);

        }
        escapeAttempts = 0;

        partyScreen.Init();
        ActionSelection();
    }

    /*
    void ChooseFirstTurn()
    {
        if (playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed)
            ActionSelection();
        else
            StartCoroutine(EnemyMove());
    }
    */

    void BattleOver(bool won) //likely wont be changed
    {
        state = BattleState.BattleOver;
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
        PhraseRecognitionSystem.Shutdown();

        //temporary fix for bug where encountering a wild pokemon after trainer battle
        if (isTrainerBattle)
        {
            isTrainerBattle = false;
        }
    }

    void ActionSelection() //likely wont be changed
    {
        state = BattleState.ActionSelection;
        dialogBox.SetDialog("Choose an action");
        dialogBox.EnableActionSelector(true);
    }

    void OpenPartyScreen() //likely wont be changed
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Pokemons);
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection() //likely wont be changed
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);

    }

    IEnumerator AboutToUse(Pokemon newPokemon)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use {newPokemon.Base.Name}. Do you want to switch pokemon?");

        state = BattleState.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;
        if (playerAction == BattleAction.Move) //checks to see who goes first
        {
            playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentMove];
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            //check who goes first
            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            bool playerGoesFirst = true; //default condition
            if (enemyMovePriority > playerMovePriority)
                playerGoesFirst = false;
            else if (enemyMovePriority == playerMovePriority)
                playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondPokemon = secondUnit.Pokemon;

            //first turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver)
            {
                PhraseRecognitionSystem.Shutdown();
                yield break;
            }
                

            if (secondPokemon.HP > 0) //applies effects and statuses at the end of combat
            {
                //second turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver)
                {
                    PhraseRecognitionSystem.Shutdown();
                    yield break;
                }
            }

        }
        else
        {
            if (playerAction == BattleAction.SwitchPokemon) //switch pokemon from party
            {
                var selectedPokemon = playerParty.Pokemons[currentMember];
                state = BattleState.Busy;
                yield return SwitchPokemon(selectedPokemon);
            }

            else if (playerAction == BattleAction.UseItem)
            {
                dialogBox.EnableActionSelector(false);
                yield return ThrowPokeball();
            }

            else if(playerAction == BattleAction.Run)
            {
                yield return TryToEscape();
            }

            //enemy turn
            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver)
            {
                PhraseRecognitionSystem.Shutdown();
                yield break;
            }
        }

        if (state != BattleState.BattleOver)
            ActionSelection();
    }

    /*
    IEnumerator PlayerMove()
    {
        state = BattleState.RunningTurn;

        var move = playerUnit.Pokemon.Moves[currentMove];
        yield return RunMove(playerUnit, enemyUnit, move);

        //If the battle state was not changed by RunMove, then go to next step.
        if (state == BattleState.RunningTurn)
            StartCoroutine(EnemyMove());
    }
    */

    IEnumerator EnemyMove() //picks random move the enemy can perform
    {
        state = BattleState.RunningTurn;

        var move = enemyUnit.Pokemon.GetRandomMove();
        yield return RunMove(enemyUnit, playerUnit, move);

        //If the battle state was not changed by RunMove, then go to next step.
        //For edge cases when a pokemon dies due to DoT
        if (state == BattleState.RunningTurn)
            ActionSelection();
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move) //Unlikely needs change
    {
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove(); //applies status check
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon)) //accuracy check
        {
            //attacking animations
            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);
            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status) //applies status effects on pokemon's stats
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0) //checks to see if volatile statuses apply
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                }
            }

            if (targetUnit.Pokemon.HP <= 0) //death animation
            {
                yield return dialogBox.TypeDialog($"{targetUnit.Pokemon.Base.Name} fainted");
                targetUnit.PlayFaintAnimation();
                yield return new WaitForSeconds(2f);

                CheckForBattleOver(targetUnit);
            }
        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed!");
        }


    }

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget) //unlikely to be changed
    {
        //Stat Boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
            {
                source.ApplyBoosts(effects.Boosts);
            }
            else
                target.ApplyBoosts(effects.Boosts);
        }

        //Status Effects
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        //Volatile Status Effects
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit) //unlikely to be changed
    {
        if (state == BattleState.BattleOver)
        {
            PhraseRecognitionSystem.Shutdown();
            yield break;
        }
        //pauses the game logic if u go into like ur bag or party screen
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        //Calculate DoT damage effects
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.UpdateHP();
        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} Fainted!");
            sourceUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target) //likely does not need change
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccuracy = move.Base.Accuracy;
        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
        {
            moveAccuracy *= boostValues[accuracy];
        }
        else
        {
            moveAccuracy /= boostValues[-accuracy];
        }

        if (evasion > 0)
        {
            moveAccuracy /= boostValues[evasion];
        }
        else
        {
            moveAccuracy += boostValues[-evasion];
        }

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Pokemon pokemon) //likely does not need change
    {
        while (pokemon.StatusChanges.Count > 0)
        {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextPokemon = playerParty.GetHealthyPokemon();
            if (nextPokemon != null)
                OpenPartyScreen();
            else
                BattleOver(false);
        }
        else
        {
            if (!isTrainerBattle)
            {
                BattleOver(true);
            }
            else
            {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if (nextPokemon != null)
                {
                    //Send next pokemon
                    StartCoroutine(AboutToUse(nextPokemon));
                }
                else
                    BattleOver(true);
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)  //likely does not need change
    {
        if (damageDetails.Critical > 1f)
        {
            yield return dialogBox.TypeDialog("A Critical Hit!");
        }
        if (damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogBox.TypeDialog("It's super effective!");
        }
        else if (damageDetails.TypeEffectiveness < 1f)
        {
            yield return dialogBox.TypeDialog("It's not very effective...");
        }
    }

    public void HandleUpdate() //will likely not need change
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if(state == BattleState.AboutToUse)
        {
            HandleAboutToUse();
        }
        //Throw pokeball override
        if(Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(ThrowPokeball());
        }
    }

    void HandleActionSelection() //likely wont need change
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogBox.UpdateActionSelection(currentAction);

        if (recog == "1" && state == BattleState.ActionSelection)
        {
            //MoveSelection();
            StartCoroutine(RunTurns(BattleAction.UseItem));
            recog = String.Empty;
        }

        //Replace this section for speech recognition
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                //Fight
                MoveSelection();
            }
            else if (currentAction == 1)
            {
                //Bag
                StartCoroutine(RunTurns(BattleAction.UseItem));
            }
            else if (currentAction == 2)
            {
                //Pokemon
                prevState = state;
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                //Run
                StartCoroutine(RunTurns(BattleAction.Run));
            }
        }
    }

    void HandleMoveSelection() //integrate here
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMove;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMove;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMove -= 2;

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove, playerUnit.Pokemon.Moves[currentMove]);

        // speech recognition moved to handler RecognizedSpeech()

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var move = playerUnit.Pokemon.Moves[currentMove];
            if (move.PP <= 0) return;

            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        }
        else if (Input.GetKeyDown(KeyCode.X) && state == BattleState.MoveSelection)
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection() //likley wont need change
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMember;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMember;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMember += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMember -= 2;

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Pokemons.Count - 1);

        partyScreen.UpdateMemberSelection(currentMember);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var selectedMember = playerParty.Pokemons[currentMember];
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted Pokemon!");
                return;
            }
            if (selectedMember == playerUnit.Pokemon)
            {
                partyScreen.SetMessageText("Already selected!");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.ActionSelection)
            {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            }
            else
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchPokemon(selectedMember));
            }


        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            if(playerUnit.Pokemon.HP <= 0)
            {
                partyScreen.SetMessageText("You have to choose a pokemon to continue!");
                return;
            }
            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.AboutToUse)
            {
                prevState = null;
                StartCoroutine(SendNextTrainerPokemon());
            }
                
            else
                ActionSelection();
        }
    }

    void HandleAboutToUse()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            aboutToUseChoice = !aboutToUseChoice; //toggles between boolean states

        dialogBox.UpdateChoiceBox(aboutToUseChoice);

        if(Input.GetKeyDown(KeyCode.Z))
        {
            dialogBox.EnableChoiceBox(false);
            if(aboutToUseChoice == true)
            {
                //Yes option
                prevState = BattleState.AboutToUse;
                OpenPartyScreen();
            }
            else
            {
                //No option
                StartCoroutine(SendNextTrainerPokemon());
            }
        }
        else if(Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    IEnumerator SwitchPokemon(Pokemon newPokemon) //integration here
    {
        if (playerUnit.Pokemon.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Pokemon.Base.Name}!");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.Setup(newPokemon); //here
        dialogBox.SetMoveNames(newPokemon.Moves);
        yield return dialogBox.TypeDialog($"Go {newPokemon.Base.Name}!");

        if (prevState == null)
            state = BattleState.RunningTurn;
        else if (prevState == BattleState.AboutToUse)
        {
            prevState = null;
            StartCoroutine(SendNextTrainerPokemon());
        }

    }

    void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.Log(speech.text);
        Debug.Log("handler called");

        string pokemon = playerUnit.Pokemon.Base.Name;
        string pattern1 = @"\b" + pokemon + @"\b";
        Debug.Log(pokemon);

        /*
        bool action_f = Regex.IsMatch(speech.text, "fight", RegexOptions.IgnoreCase);
        bool action_b = Regex.IsMatch(speech.text, "bag", RegexOptions.IgnoreCase);
        bool action_p = Regex.IsMatch(speech.text, "party", RegexOptions.IgnoreCase);
        bool action_r = Regex.IsMatch(speech.text, "run", RegexOptions.IgnoreCase);
        */

        /*if (action_f && state == BattleState.ActionSelection)
        {
            MoveSelection();
        }
        if (action_b && state == BattleState.ActionSelection)
        {

        }
        */
        for (int i = 0; i < playerUnit.Pokemon.Moves.Count; i++)
        {
            string moves = playerUnit.Pokemon.Moves[i].Base.Name;
            Debug.Log(moves);
            string pattern2 = @"\b" + moves + @"\b";

            bool m = Regex.IsMatch(speech.text, pattern1, RegexOptions.IgnoreCase);
            bool n = Regex.IsMatch(speech.text, pattern2, RegexOptions.IgnoreCase);
            currentMove = i;
            dialogBox.UpdateMoveSelection(currentMove, playerUnit.Pokemon.Moves[currentMove]);

            if (m && n && state == BattleState.MoveSelection)
            {
                Debug.Log("valid move");
                var pmove = playerUnit.Pokemon.Moves[currentMove];
                if (pmove.PP <= 0) return;

                dialogBox.EnableMoveSelector(false);
                dialogBox.EnableDialogText(true);
                StartCoroutine(RunTurns(BattleAction.Move));
            }
        }
    }

    IEnumerator SendNextTrainerPokemon()
    {
        state = BattleState.Busy;

        var nextPokemon = trainerParty.GetHealthyPokemon();
        enemyUnit.Setup(nextPokemon);

        yield return dialogBox.TypeDialog($"{trainer.Name} sends out {nextPokemon.Base.Name}!");

        state = BattleState.RunningTurn;
    }

    IEnumerator ThrowPokeball()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't steal a trainer's pokemon!");
            state = BattleState.RunningTurn;
            yield break;
        }

        yield return dialogBox.TypeDialog($"{player.Name} used POKEBALL!");

        var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position - new Vector3(2,0), Quaternion.identity);
        var pokeball = pokeballObj.GetComponent<SpriteRenderer>();
        //animate
        yield return pokeball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return pokeball.transform.DOMoveY(enemyUnit.transform.position.y - 2.8f, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchPokemon(enemyUnit.Pokemon);

        for (int i = 0; i < Mathf.Min(shakeCount, 3); i++)
        {
            yield return new WaitForSeconds(0.5f);
            pokeball.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }

        if(shakeCount == 4)
        {
            //pokemon caught
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} was caught!");
            yield return pokeball.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} has been added!");

            Destroy(pokeball);
            BattleOver(true);
        }
        else
        {
            //pokemon broke out
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();

            if (shakeCount < 2)
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} broke free!");
            else
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} was almost caught!");
            Destroy(pokeball);
            state = BattleState.RunningTurn;
        }
    }

    int TryToCatchPokemon(Pokemon pokemon)
    {
        float a = (3 * pokemon.MaxHp - 2 * pokemon.HP) * pokemon.Base.CatchRate * ConditionsDB.GetStatusBonus(pokemon.Status) / (3 * pokemon.MaxHp);

        if (a >= 255)
            return 4; //4 == be caught

        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));
        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
                break;
            ++shakeCount;
        }

        return shakeCount;
    }
    
    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;

        if(isTrainerBattle) //dont allow player to escape from trainers
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++escapeAttempts;

        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogBox.TypeDialog($"Ran away safely!");
            BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if(UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.TypeDialog($"Ran away safely!");
                BattleOver(true);
            }
            else
            {
                yield return dialogBox.TypeDialog($"Can't escape!");
                state = BattleState.RunningTurn;
            }
        }

    }
}
