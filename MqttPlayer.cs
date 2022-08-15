using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class MqttPlayer : MonoBehaviour
{
    public MqttManager _mqttManager;
    public GameObject _playerPrefabs;
    private GameObject _player;
    private GameObject _joinPlayer;
    private bool isCreate = false;
    private PlayerInputAction _playerInputAction;

    private PlayerData player_Pub_json;
    private PlayerData player_Sub_json;

    private string player_prePubMessage;

    [SerializeField] private int playerSpeed;

    public class PlayerData
    {
        public int playerID;
        public float[] playerPosition;
        public float[] playerRotation;

    }

    private void Awake()
    {
        _playerInputAction = new PlayerInputAction();
    }

    private void OnEnable()
    {
        _playerInputAction.Enable();
    }

    private void OnDisable()
    {
        _playerInputAction.Disable();
    }

    private void Start()
    {
        _mqttManager = this.gameObject.GetComponent<MqttManager>();

        player_Pub_json = new PlayerData();
        player_Pub_json.playerID = 0;
        player_Pub_json.playerPosition = new float[3];
        player_Pub_json.playerPosition[0] = 0;
        player_Pub_json.playerPosition[1] = 0;
        player_Pub_json.playerPosition[2] = 0;
        player_Pub_json.playerRotation = new float[4];
        player_Pub_json.playerRotation[0] = 0;
        player_Pub_json.playerRotation[1] = 0;
        player_Pub_json.playerRotation[2] = 0;
        player_Pub_json.playerRotation[3] = 0;
        player_prePubMessage = JsonUtility.ToJson(player_Pub_json);

        player_Sub_json = new PlayerData();
        player_Sub_json.playerID = 0;
        player_Sub_json.playerPosition = new float[3];
        player_Sub_json.playerPosition[0] = 0;
        player_Sub_json.playerPosition[1] = 0;
        player_Sub_json.playerPosition[2] = 0;
        player_Sub_json.playerRotation = new float[4];
        player_Sub_json.playerRotation[0] = 0;
        player_Sub_json.playerRotation[1] = 0;
        player_Sub_json.playerRotation[2] = 0;
        player_Sub_json.playerRotation[3] = 0;

        SelectPlayer();
    }

    private void FixedUpdate()
    {
        if (_player != null)
        {
            CreateJoinPlayer();
            MovePlayer();
            RotatePlayer();
            PlayerUpdate();
            JoinPlayerUpdate();

        }
        
    }

    void PlayerUpdate()
    {
        player_Pub_json.playerPosition[0] = _player.transform.position.x;
        player_Pub_json.playerPosition[1] = _player.transform.position.y;
        player_Pub_json.playerPosition[2] = _player.transform.position.z;

        player_Pub_json.playerRotation[0] = _player.transform.rotation.x;
        player_Pub_json.playerRotation[1] = _player.transform.rotation.y;
        player_Pub_json.playerRotation[2] = _player.transform.rotation.z;
        player_Pub_json.playerRotation[3] = _player.transform.rotation.w;

        _mqttManager._pubMessage = JsonUtility.ToJson(player_Pub_json);

        if (player_prePubMessage != _mqttManager._pubMessage)
        {    
            _mqttManager.client_MqttMsgPublishSent();

            player_prePubMessage = _mqttManager._pubMessage;
        }
        
    }

    void JoinPlayerUpdate()
    {
        if (_joinPlayer != null)
        {
            player_Sub_json = JsonUtility.FromJson<PlayerData>(_mqttManager._subMessage);

            // Position
            Vector3 position = new Vector3();
            position.x = player_Sub_json.playerPosition[0];
            position.y = player_Sub_json.playerPosition[1];
            position.z = player_Sub_json.playerPosition[2];
            _joinPlayer.GetComponent<Transform>().position = position;

            // Rotation
            Quaternion rotation = new Quaternion();
            rotation.x = player_Pub_json.playerRotation[0];
            rotation.y = player_Pub_json.playerRotation[1];
            rotation.z = player_Pub_json.playerRotation[2];
            rotation.w = player_Pub_json.playerRotation[3];

            _joinPlayer.GetComponent<Transform>().rotation = rotation;
        }
        
    }

    void CreatePlayer()
    {
        _player = Instantiate(_playerPrefabs, new Vector3(0,1,0), Quaternion.identity);
    }

    void CreateJoinPlayer()
    {
        if ((_mqttManager._isP1Join && isCreate == false) || (_mqttManager._isP2Join && isCreate == false))
        {
            player_Sub_json = JsonUtility.FromJson<PlayerData>(_mqttManager._subMessage);
            Vector3 position = new Vector3();
            position.x = player_Sub_json.playerPosition[0];
            position.y = player_Sub_json.playerPosition[1];
            position.z = player_Sub_json.playerPosition[2];

            _joinPlayer = Instantiate(_playerPrefabs, position , Quaternion.identity);
            _joinPlayer.GetComponentInChildren<TextMeshPro>().text = "Player " + player_Sub_json.playerID.ToString();

            isCreate = true;
        }
    }

    public void SelectPlayer()
    {
        Time.timeScale = 0f;
    }

    public void SelectPlayer1()
    {
        player_Pub_json.playerID = 1;
        CreatePlayer();
        PlayerSelected();
        _player.transform.position = new Vector3(5, 1, 0);

        _mqttManager._subTopic = _mqttManager._p1SubTopic;
        _mqttManager.SubscribeToTopic();

        _mqttManager._pubTopic = _mqttManager._p1PubTopic;
        _mqttManager._allowPublish = true;

    }

    public void SelectPlayer2()
    {
        player_Pub_json.playerID = 2;
        CreatePlayer();
        PlayerSelected();
        _player.transform.position = new Vector3(-5,1,0);

        _mqttManager._subTopic = _mqttManager._p2SubTopic;
        _mqttManager.SubscribeToTopic();

        _mqttManager._pubTopic = _mqttManager._p2PubTopic;
        _mqttManager._allowPublish = true;

    }

    public void PlayerSelected()
    {
        _player.GetComponentInChildren<TextMeshPro>().text = "Player " + player_Pub_json.playerID.ToString();

        GameObject[] playerSelectButton = GameObject.FindGameObjectsWithTag("SelectPlayer");
        foreach (GameObject selectplayer in playerSelectButton)
        {
            Destroy(selectplayer);
        }

        Time.timeScale = 1f;
    }

    // Player Control Input
    void MovePlayer()
    {
        Vector2 moveInput = _playerInputAction.Movement.Move.ReadValue<Vector2>();
        Vector3 moveInput3 = new Vector3(moveInput.x, 0, moveInput.y);

        _player.GetComponent<Rigidbody>().velocity = moveInput3 * playerSpeed;
    }

    void RotatePlayer()
    {
        Vector2 rotateInput = _playerInputAction.Movement.Rotate.ReadValue<Vector2>();
        Vector3 rotateInput3 = new Vector3(0, rotateInput.x, 0);

        _player.GetComponent<Rigidbody>().angularVelocity = rotateInput3 * 2;
    }
}
