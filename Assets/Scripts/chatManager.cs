using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


public class chatManager : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    List<Message> messagelist = new List<Message>();

    public GameObject chatpanel, textobject;

    MqttClient client = new MqttClient("mqtt.eclipseprojects.io");

    public string p1_string;
    public string p2_string;

    void Start()
    {
        string[] mqtt_topic = { "Team-2/Digimon/players/#" };
        byte[] mqtt_qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };

        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        client.Connect("");
        client.Subscribe(mqtt_topic, mqtt_qosLevels);
        Debug.Log("mqtt connected");
    }
    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        if (e.Topic == "Team-2/Digimon/players/player1/chat")
        {
            //sendtochat("Player 1: " + System.Text.Encoding.UTF8.GetString(e.Message));
            p1_string = System.Text.Encoding.UTF8.GetString(e.Message);
            //Debug.Log("Player 1: " + System.Text.Encoding.UTF8.GetString(e.Message));
        }
        else if (e.Topic == "Team-2/Digimon/players/player2/chat")
        {
            //sendtochat("Player 2: " + System.Text.Encoding.UTF8.GetString(e.Message));
            //Debug.Log("Player 2: " + System.Text.Encoding.UTF8.GetString(e.Message));
            p2_string = System.Text.Encoding.UTF8.GetString(e.Message);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            sendtochat("YEEEEET");
            Debug.Log("YEEET");
        }

        if (p1_string != "")
        {
            sendtochat("Player 1: " + p1_string);
            p1_string = "";
        }
        else if (p2_string != "")
        {
            sendtochat("Player 2: " + p2_string);
            p2_string = "";
        }


    }

    public void sendtochat (string text)
    {
        if(messagelist.Count > 20)
        {
            Destroy(messagelist[0].textobject.gameObject);
            messagelist.Remove(messagelist[0]);
        }
        Message newmessage = new Message();


        newmessage.text = text;
        GameObject newtext = Instantiate(textobject, chatpanel.transform);
        newmessage.textobject = newtext.GetComponent<Text>();
        newmessage.textobject.text = newmessage.text;

        messagelist.Add(newmessage);




    }



}


[System.Serializable]
public class Message
{
    public string text;
    public Text textobject;
}
