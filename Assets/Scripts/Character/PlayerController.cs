using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

//Controls Player movement in gridspace
//Unlikely needs change

public class PlayerController : MonoBehaviour
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;

    public Vector2 input;

    private CharacterAnimator animator;
    private Character character;

    bool multiplayerWorld = false; 

    MqttClient client = new MqttClient("mqtt.eclipseprojects.io");  

    void Awake()
    {
        animator = GetComponent<CharacterAnimator>();
        character = GetComponent<Character>();

        string[] mqtt_topic = { "Team-2/Digimon/players/#" };
        byte[] mqtt_qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };

        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        client.Connect("");
        client.Subscribe(mqtt_topic, mqtt_qosLevels);
    }

    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        
    }

    public void HandleUpdate()
    {

        if (Input.GetKeyDown(KeyCode.U))
        {
            multiplayerWorld = true;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            multiplayerWorld = false;
        }

        if (multiplayerWorld)
        {
            client.Publish("Team-2/Digimon/players/player1/multi", System.Text.Encoding.UTF8.GetBytes("connected"));
            new WaitForSeconds(0.1f);
        }
        else
        {
            client.Publish("Team-2/Digimon/players/player1/multi", System.Text.Encoding.UTF8.GetBytes("disconnected"));
            new WaitForSeconds(0.1f);
        }

        if (!character.IsMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)) && multiplayerWorld)
            {
                client.Publish("Team-2/Digimon/players/player1/x_pos", System.Text.Encoding.UTF8.GetBytes(character.transform.position.x.ToString()));
                new WaitForSeconds(0.1f);
            }

            if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)) && multiplayerWorld)
            {
                client.Publish("Team-2/Digimon/players/player1/y_pos", System.Text.Encoding.UTF8.GetBytes(character.transform.position.y.ToString()));
                new WaitForSeconds(0.1f);
            }

            if (Input.GetKey(KeyCode.LeftArrow) && multiplayerWorld)
            {
                client.Publish("Team-2/Digimon/players/player1/looking", System.Text.Encoding.UTF8.GetBytes("left"));
                new WaitForSeconds(0.1f);
                //Debug.Log("transmitting left");
            }
            else if (Input.GetKey(KeyCode.RightArrow) && multiplayerWorld)
            {
                client.Publish("Team-2/Digimon/players/player1/looking", System.Text.Encoding.UTF8.GetBytes("right"));
                new WaitForSeconds(0.1f);
            }
            else if (Input.GetKey(KeyCode.UpArrow) && multiplayerWorld)
            {
                client.Publish("Team-2/Digimon/players/player1/looking", System.Text.Encoding.UTF8.GetBytes("up"));
                new WaitForSeconds(0.1f);
                //Debug.Log("transmitting up");
            }
            else if (Input.GetKey(KeyCode.DownArrow) && multiplayerWorld)
            {
                client.Publish("Team-2/Digimon/players/player1/looking", System.Text.Encoding.UTF8.GetBytes("down"));
                new WaitForSeconds(0.1f);
            }

            //If a player goes in a diagonal direction, they move at sqrt(2)*v
            //This prevents speed exploits
            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                StartCoroutine(character.Move(input, OnMoveOver));
            }
        }

        character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Interact();
        }
    }

    void Interact()
    {
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        //Debug.DrawLine(transform.position, interactPos, Color.green, 0.5f);

        var collider = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.i.InteractableLayer);
        if (collider != null)
        {
            collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    private void OnMoveOver()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position - new Vector3(0, character.OffsetY), 0.2f, GameLayers.i.TriggerableLayers);
        foreach (var collider in colliders)
        {
            var triggerable = collider.GetComponent<IPlayerTriggerable>();
            if(triggerable != null)
            {
                character.Animator.IsMoving = false;
                triggerable.OnPlayerTriggered(this);
                break;
            }
        }
    }

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }

    public Character Character => character;
}

//Reference: Pokemon in Unity Series on Youtube (Game Dev Experiments)