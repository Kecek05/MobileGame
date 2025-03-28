using QFSW.QC;
using Sortify;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class GameStateManager : BaseGameStateManager
{

    //public override void OnNetworkSpawn()
    //{
    //    losedPlayer.OnValueChanged += LosedPlayer_OnvalueChanged;
    //    gameState.OnValueChanged += GameState_OnValueChanged;

    //    if (IsServer)
    //    {
    //        PlayerHealth.OnPlayerDie += LoseGame;
    //        PlayerSpawner.OnPlayerSpawned += PlayerSpawner_OnPlayerSpawned;

    //        GameTimerManager.OnGameTimerEnd += GameTimerManager_OnGameTimerEnd;
    //    }
    //}

    //public override void OnNetworkDespawn()
    //{

    //    losedPlayer.OnValueChanged -= LosedPlayer_OnvalueChanged;
    //    gameState.OnValueChanged -= GameState_OnValueChanged;

    //    if (IsServer)
    //    {
    //        PlayerHealth.OnPlayerDie -= LoseGame;
    //        PlayerSpawner.OnPlayerSpawned -= PlayerSpawner_OnPlayerSpawned;

    //        GameTimerManager.OnGameTimerEnd -= GameTimerManager_OnGameTimerEnd;
    //    }
    //}


    public override void OnNetworkSpawn()
    {
        throw new NotImplementedException();
    }

    public override async void ChangeGameState(GameState gameState, int delayToChange = 0)
    {

        //if (gameOver) return;

        await Task.Delay(delayToChange);
        SetGameStateServerRpc(gameState);

    }

    public override void ConnectionLostHostAndClient()
    {
        TriggerOnLostConnectionInHost();
    }

    protected override void HandleOnGameStateValueChanged(GameState previousValue, GameState newValue)
    {
        switch (newValue)
        {
            case GameState.WaitingForPlayers:
                break;
            case GameState.SpawningPlayers:
                //All players connected
                Debug.Log("Start Spawning Players");

                break;
            case GameState.CalculatingResults:
                if (IsServer && !IsHost) //DS
                {
                    //REFACTOR
                    CalculatePearlsManager.CalculatePossibleResults(NetworkServerProvider.Instance.CurrentNetworkServer.ServerAuthenticationService.PlayerDatas[0], NetworkServerProvider.Instance.CurrentNetworkServer.ServerAuthenticationService.PlayerDatas[1]);
                    ChangeGameState(GameState.ShowingPlayersInfo);
                }
                else if (IsHost)
                {
                    ChangeGameState(GameState.ShowingPlayersInfo);
                }
                break;
            case GameState.ShowingPlayersInfo:
                if (IsServer)
                {
                    GameManager.Instance.RandomizePlayerItems();
                    ChangeGameState(GameState.GameStarted, delayClosePlayersInfo); //Show Players Info delay
                }
                break;
            case GameState.GameStarted:
                if (IsServer)
                {
                    //START GAME TIMER 
                    GameManager.Instance.TurnManager.StartGame();
                }
                break;
            case GameState.GameEnded:
                //If is DS, change players save
                if (IsServer)
                    CheckForTheWinner();
                //Show UI for clients
                break;
        }

        TriggerOnGameStateChange();
        Debug.Log($"Game State Changed to: {newValue}");
    }

    protected override void HandleOnGameTimerEnd()
    {
        Debug.Log("Game Timer Ended");
        ChangeGameState(GameState.GameEnded);
    }

    protected override void HandleOnPlayerSpawned(int playerCount)
    {
        if (playerCount == 2)
        {
            ChangeGameState(GameState.CalculatingResults); // All players Spawned, calculating Results
        }
    }

    [Rpc(SendTo.Server)]
    protected override void SetGameStateServerRpc(GameState newState)
    {
        gameState.Value = newState;
    }

    public override void OnNetworkDespawn()
    {
        throw new NotImplementedException();
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
