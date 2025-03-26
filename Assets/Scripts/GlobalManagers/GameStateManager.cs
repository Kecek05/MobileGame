using QFSW.QC;
using Sortify;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    public event Action OnGameOver;
    public event Action OnWin;
    public event Action OnLose;

    /// <summary>
    /// Called when any player lost connection in Host.
    /// </summary>
    public static event Action OnLostConnectionInHost;

    /// <summary>
    /// Called when the server should be closed.
    /// </summary>
    public static event Action OnCanCloseServer;

    [BetterHeader("Settings")]
    [Tooltip("in ms")][SerializeField] private int delayClosePlayersInfo = 3000;


    private NetworkVariable<GameState> gameState = new(GameState.WaitingForPlayers);
    private NetworkVariable<PlayableState> losedPlayer = new(PlayableState.None);

    private bool gameOver = false;
    private bool localWin = false;
    private int calculatedResults = 0; //Count the results pré calculated by clients
    private int changedResults = 0; // Count the results changed by clients
    //Publics

    public bool LocalWin => localWin;
    public NetworkVariable<PlayableState> LosedPlayer => losedPlayer;
    public bool GameOver => gameOver;
    public NetworkVariable<GameState> CurrentGameState => gameState;



    public override void OnNetworkSpawn()
    {
        losedPlayer.OnValueChanged += LosedPlayer_OnvalueChanged;
        gameState.OnValueChanged += GameState_OnValueChanged;

        if(IsClient)
        {
            CalculatePearlsManager.OnFinishedCalculateResults += CalculatePearlsManager_OnFinishedCalculateResults;
        }

        if (IsServer)
        {
            PlayerHealth.OnPlayerDie += LoseGame;
            PlayerSpawner.OnPlayerSpawned += PlayerSpawner_OnPlayerSpawned;
        }
    }

    private void CalculatePearlsManager_OnFinishedCalculateResults()
    {
        //Called when any client calculated the results
        CalculatedResultServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void CalculatedResultServerRpc()
    {
        calculatedResults++;

        Debug.Log($"Calculated Results: {calculatedResults}");
        if (calculatedResults == 2)
        {
            //both clients calculated the results, change state
            ChangeGameState(GameState.ShowingPlayersInfo);
        }
    }

    private void PlayerSpawner_OnPlayerSpawned(int playerCount)
    {
        if (playerCount == 2)
        {
            ChangeGameState(GameState.CalculatingResults); // All players Spawned, calculating Results
        }
    }

    private void GameState_OnValueChanged(GameState previousValue, GameState newValue)
    {
        //Only Server

        switch (newValue)
        {
            case GameState.WaitingForPlayers:
                break;
            case GameState.SpawningPlayers:
                //All players connected
                Debug.Log("Start Spawning Players");

                break;
            case GameState.CalculatingResults:
                if (IsClient)
                    CalculatePearlsManager.CalculatePossibleResults();
                break;
            case GameState.ShowingPlayersInfo:

                if(IsServer)
                {
                    GameFlowManager.Instance.RandomizePlayerItems();
                    ChangeGameState(GameState.GameStarted, delayClosePlayersInfo); //Show Players Info delay
                }
                break;
            case GameState.GameStarted:
                if(IsServer)
                    GameFlowManager.Instance.TurnManager.StartGame();
                break;
            case GameState.GameEnded:
                break;
        }

        Debug.Log($"Game State Changed to: {newValue}");
    }

    public void ConnectionLostHostAndClient()
    {
        OnLostConnectionInHost?.Invoke();
    }

    private void LosedPlayer_OnvalueChanged(PlayableState previousValue, PlayableState newValue)
    {
        OnGameOver?.Invoke();

        if(IsServer)
            ChangeGameState(GameState.GameEnded);
        
        if(IsClient)
        {
            ClientChangedResultsRpc();
            GameOverAsync();
        }

        gameOver = true;
    }

    [Rpc(SendTo.Server)]
    private void ClientChangedResultsRpc()
    {
        changedResults++;

        if(changedResults == 2)
        {
            //Both clients changed the results
            //Close Server
            TriggerCanCloseServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void TriggerCanCloseServerRpc()
    {
        OnCanCloseServer?.Invoke();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ClientRemaningWinRpc()
    {
        IwinGameOverAsync();
    }

    private async void IwinGameOverAsync()
    {
        //Change pearls, then win
        localWin = true;
        await CalculatePearlsManager.TriggerChangePearls();

        Debug.Log("IwinGameOverAsync Finished");
        TriggerCanCloseServerRpc();

        OnWin?.Invoke();

    }

    public async void GameOverAsync()
    {
        if (losedPlayer.Value == GameFlowManager.Instance.TurnManager.LocalPlayableState)
        {
            //Change pearls, then lose
            localWin = false;

            await CalculatePearlsManager.TriggerChangePearls();
            OnLose?.Invoke();
        }
        else if (losedPlayer.Value == PlayableState.None)
        {
            //Tie, lose
            //Change pearls, then lose
            localWin = false;

            await CalculatePearlsManager.TriggerChangePearls();
            OnLose?.Invoke();
        }
        else
        {
            //Change pearls, then win
            localWin = true;

            await CalculatePearlsManager.TriggerChangePearls();
            OnWin?.Invoke();
        }
    }

    /// <summary>
    /// Call this to know who loses and who wins. Server only.
    /// </summary>
    /// <param name="playerLosedPlayableState"> Playing State of the player who loses</param>
    public void LoseGame(PlayableState playerLosedPlayableState)
    {
        losedPlayer.Value = playerLosedPlayableState;
    }

    /// <summary>
    /// Call this to change the Game State, delay its optional.
    /// </summary>
    /// <param name="gameState"> Game State</param>
    /// <param name="delayToChange"> Delay in ms</param>
    [Command("gameStateManager-ChangeGameState")]
    public async void ChangeGameState(GameState gameState, int delayToChange = 0) //0ms default
    {
        if (gameOver) return;

        await Task.Delay(delayToChange);
        SetGameStateServerRpc(gameState);
    }

    [Rpc(SendTo.Server)]
    private void SetGameStateServerRpc(GameState newState)
    {
        gameState.Value = newState;
    }

    public override void OnNetworkDespawn()
    {

        losedPlayer.OnValueChanged -= LosedPlayer_OnvalueChanged;
        gameState.OnValueChanged -= GameState_OnValueChanged;

        if (IsServer)
        {
            PlayerHealth.OnPlayerDie -= LoseGame;
            PlayerSpawner.OnPlayerSpawned -= PlayerSpawner_OnPlayerSpawned;
        }
    }

}


/// <summary>
/// Related to game flow, use PlayableState to player relayed states.
/// </summary>
public enum GameState
{
    None,
    WaitingForPlayers, //Waiting for players to connect
    SpawningPlayers, //Spawning Players
    CalculatingResults, //Calculating Results
    ShowingPlayersInfo, // All players connected, and Spawned, Showing Players Info
    GameStarted, //Game Started
    GameEnded, //Game Over
}
