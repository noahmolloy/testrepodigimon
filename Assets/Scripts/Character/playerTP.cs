using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class playerTP : MonoBehaviour
{
    float x_pos;
    float y_pos;
    string looking;

    Character character;
    CharacterAnimator animator;

    bool connected = false;

    // Start is called before the first frame update
    void Start()
    {
        character = GetComponent<Character>();
        animator = GetComponent<CharacterAnimator>();
        string[] mqtt_topic = { "Team-2/Digimon/players/#" };
        byte[] mqtt_qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };

        MqttClient client = new MqttClient("mqtt.eclipseprojects.io");
        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        client.Connect("");
        client.Subscribe(mqtt_topic, mqtt_qosLevels);
        Debug.Log("mqtt connected");
    }

    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        if (e.Topic == "Team-2/Digimon/players/player1/multi")
        {
            if (System.Text.Encoding.UTF8.GetString(e.Message) == "connected")
            {
                connected = true;
            }
            else if (System.Text.Encoding.UTF8.GetString(e.Message) == "disconnected")
            {
                connected = false;
            }
        }

        if (connected)
        {
            if (e.Topic == "Team-2/Digimon/players/player2/x_pos")
            {
                x_pos = float.Parse(System.Text.Encoding.UTF8.GetString(e.Message));
            }
            if (e.Topic == "Team-2/Digimon/players/player2/y_pos")
            {
                y_pos = float.Parse(System.Text.Encoding.UTF8.GetString(e.Message));
            }
            if (e.Topic == "Team-2/Digimon/players/player2/looking")
            {
                looking = System.Text.Encoding.UTF8.GetString(e.Message);
            }
        }  
    }

    void Update()
    {
        new WaitForSeconds(0.1f);
        transform.position = new Vector3(x_pos, y_pos, 0);

        if (looking == "left")
        {
            animator.SetFacingDirection(FacingDirection.Left);
            //Debug.Log("facing left");
            looking = string.Empty;
        }
        else if(looking == "right")
        {
            animator.SetFacingDirection(FacingDirection.Right);
            //Debug.Log("facing right");
            looking = string.Empty;
        }
        else if(looking == "up")
        {
            animator.SetFacingDirection(FacingDirection.Up);
            //Debug.Log("facing up");
            looking = string.Empty;
        }
        else if(looking == "down")
        {
            animator.SetFacingDirection(FacingDirection.Down);
            //Debug.Log("facing down");
            looking = string.Empty;
        }
        //x_axis = 0;
        //y_axis = 0;
    }
}