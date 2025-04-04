using QFSW.QC;
using Sortify;
using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class DebuggingTools : NetworkBehaviour
{

    [BetterHeader("References")]
    public TextMeshProUGUI debugGameStateText;
    public TextMeshProUGUI debugPlayableStateText;
    public TextMeshProUGUI debugPingText;

    //Ping
    private float lastPingTime;
    private float currentPing = 0f;

    private BaseGameStateManager stateManager;
    private BaseTurnManager turnManager;
    private BaseGameOverManager gameOverManager;

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            StartCoroutine(PingCoroutine());
        }

        stateManager = ServiceLocator.Get<BaseGameStateManager>();
        turnManager = ServiceLocator.Get<BaseTurnManager>();
        gameOverManager = ServiceLocator.Get<BaseGameOverManager>();
    }

    private IEnumerator PingCoroutine()
    {
        while (true)
        {
            lastPingTime = Time.time;
            RequestPingServerRpc();
            yield return new WaitForSeconds(1f); // Send ping every second
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPingServerRpc(ServerRpcParams rpcParams = default)
    {
        RespondPingClientRpc(rpcParams.Receive.SenderClientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RespondPingClientRpc(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            currentPing = (Time.time - lastPingTime) * 1000f; // Convert to milliseconds
            debugPingText.text = $"Ping: {currentPing:F1} ms";
        }
    }

    private void Update()
    {
        debugGameStateText.text = stateManager.CurrentGameState.Value.ToString();
        debugPlayableStateText.text = turnManager.CurrentPlayableState.Value.ToString();
    }



    [Command("shutDownDebug")]
    private void ShutdownDebug() //DEBUG
    {
        if (NetworkManager.Singleton.IsConnectedClient)
            NetworkManager.Singleton.Shutdown();
    }

    [Command("quitGame")]
    private void QuitGameDEBUG()
    {
        //Return to main menu
        if (NetworkManager.Singleton != null && HostSingleton.Instance != null && NetworkManager.Singleton.IsHost) //Server cant click buttons
        {
            HostSingleton.Instance.GameManager.ShutdownAsync();
        }

        if (ClientSingleton.Instance != null)
            ClientSingleton.Instance.GameManager.Disconnect();
    }

    [Command("listAllPlayerData")]
    private void ListAllPlayerData()
    {
        foreach (PlayerData playerData in NetworkServerProvider.Instance.CurrentNetworkServer.ServerAuthenticationService.ClientIdToPlayerData.Values)
        {
            Debug.Log($"PlayerUserData Name: {playerData.userData.userName} - Client Id: {playerData.clientId} - PlayableState: {playerData.playableState} - Pearls: {playerData.userData.UserPearls} - GameObject: {playerData.gameObject} - PlayerId: {playerData.userData.userAuthId}");
            Debug.Log($"Pearls To Win: {playerData.calculatedPearls.PearlsToWin} - Pearls To Lose: - {playerData.calculatedPearls.PearlsToLose}");
        }
    }


    [Command("setGameOver")]
    private void SetGameOver(PlayableState playableState)
    {
        SetGameOverRpc(playableState);
    }

    [Rpc(SendTo.Server)]
    private void SetGameOverRpc(PlayableState playableState)
    {
        gameOverManager.LoseGame(playableState);
    }

    [Rpc(SendTo.Server)]
    private void DebugOnServerRpc(string debugText)
    {
        Debug.Log(debugText);
    }

    [Command("serverDebugLog")]
    private void SendDebugLogToServer(string message)
    {
        DebugOnServerRpc(message);
    }

}
