using QFSW.QC;
using Sortify;
using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public static event Action<Player> OnPlayerSpawned;


    [BetterHeader("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerInventoryUI playerInventoryUI;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerDragController playerDragController;
    [SerializeField] private PlayerLauncher playerLauncher;
    [SerializeField] private Collider playerTouchColl;
    [SerializeField] private GameObject[] playerColliders;
    private PlayerStateMachine playerStateMachine;

    private NetworkVariable<PlayableState> thisPlayableState = new();

    private NetworkVariable<FixedString32Bytes> playerName = new();
    private NetworkVariable<int> playerPearls = new();


    //Publics
    public PlayerStateMachine PlayerStateMachine => playerStateMachine;
    public PlayerInventory PlayerInventory => playerInventory;
    public PlayerInventoryUI PlayerInventoryUI => playerInventoryUI;
    public PlayerHealth PlayerHealth => playerHealth;
    public PlayerDragController PlayerDragController => playerDragController;
    public PlayerLauncher PlayerLauncher => playerLauncher;

    public NetworkVariable<PlayableState> ThisPlayableState => thisPlayableState;

    public NetworkVariable<FixedString32Bytes> PlayerName => playerName;

    public NetworkVariable<int> PlayerPearls => playerPearls;

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            UserData userData = null;

            if(IsHost)
            {
                //Host Singleton
                userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);

            } else
            {
                //Server Singleton
                //userData = ServerSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            }

            playerName.Value = userData.userName;
            playerPearls.Value = userData.userPearls;

            OnPlayerSpawned?.Invoke(this);

        }


        gameObject.name = "Player " + playerName.Value;

        thisPlayableState.OnValueChanged += PlayableStateChanged;

        if(!IsHost) // host will add itself twice
            PlayableStateChanged(thisPlayableState.Value, thisPlayableState.Value);

        if (IsOwner)
        {

            GameFlowManager.OnMyTurnStarted += GameFlowManager_OnMyTurnStarted;

            GameFlowManager.OnMyTurnEnded += GameFlowManager_OnMyTurnEnded;

            GameFlowManager.OnMyTurnJumped += GameFlowManager_OnMyTurnJumped;

            playerStateMachine = new PlayerStateMachine(this);

            playerStateMachine.Initialize(playerStateMachine.idleEnemyTurnState);

        } else
        {
            //if not owner, turn off touch collider
            playerTouchColl.enabled = false;

        }

    }

    private void GameFlowManager_OnMyTurnJumped()
    {
        //DelayToChangeMyTurnJumped();
        playerStateMachine.TransitionTo(playerStateMachine.idleMyTurnState);
    }

    private async void DelayToChangeMyTurnJumped()
    {
        //this player jumped
        await Task.Delay(3000);
        playerStateMachine.TransitionTo(playerStateMachine.idleMyTurnState);
    }

    private void GameFlowManager_OnMyTurnEnded()
    {
        playerStateMachine.TransitionTo(playerStateMachine.myTurnEndedState);
    }

    private void GameFlowManager_OnMyTurnStarted()
    {
        // My Turn Started, I can play
        playerStateMachine.TransitionTo(playerStateMachine.myTurnStartedState);
        Debug.Log("I can play!");

    }

    //DEBUG
    [Command("player-passTurn", MonoTargetType.All)]
    private void PassTurn()
    {
        if(!IsOwner)
        {
            return;
        }

        playerStateMachine.TransitionTo(playerStateMachine.idleEnemyTurnState);
        GameFlowManager.Instance.PlayerPlayedRpc(GameFlowManager.Instance.LocalplayableState);

    }


    [Rpc(SendTo.Server)]
    public void SetThisPlayableStateRpc(PlayableState playableState)
    {
        // Cant be OnnetworkSpawn because it needs to be called by NetworkServer
        thisPlayableState.Value = playableState;

    }
    

    private void PlayableStateChanged(PlayableState previousValue, PlayableState newValue)
    {
        if (IsOwner)
        {
            GameFlowManager.Instance.SetLocalStates(thisPlayableState.Value); //pass to GameFlow to know when its local turn
        }

        if (thisPlayableState.Value == PlayableState.Player1Playing)
        {

            foreach(GameObject playerCollider in playerColliders)
            {
                playerCollider.layer = PlayersPublicInfoManager.PLAYER_1_LAYER;
            }
        }
        else
        {
            foreach (GameObject playerCollider in playerColliders)
            {
                playerCollider.layer = PlayersPublicInfoManager.PLAYER_2_LAYER;
            }
        }

        PlayersPublicInfoManager.Instance.AddPlayerToPlayersDictionary(thisPlayableState.Value, gameObject);
    }

    public override void OnNetworkDespawn()
    {
        thisPlayableState.OnValueChanged -= PlayableStateChanged;

        if (IsOwner)
        {
            GameFlowManager.OnMyTurnStarted -= GameFlowManager_OnMyTurnStarted;

            GameFlowManager.OnMyTurnEnded -= GameFlowManager_OnMyTurnEnded;

            GameFlowManager.OnMyTurnJumped -= GameFlowManager_OnMyTurnJumped;
        }
    }

}
