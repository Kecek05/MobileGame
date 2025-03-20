using Sortify;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class ApplicationController : MonoBehaviour //Responsable of launching the game in the correct mode
{

    [BetterHeader("Singletons")]
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
    [SerializeField] private ServerSingleton serverPrefab;

    [BetterHeader("Settings", 12)]
    [SerializeField] private NetworkObject playerPrefab;

    [SerializeField] private bool isServerDebug = false;

    private ApplicationData appData;

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

        if(isServerDebug)
        {
            await LaunchInMode(true);
            return;
        }

        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if(isDedicatedServer)
        {
            //Dedicated Server Code
            appData = new();

            ServerSingleton serverSingleton = Instantiate(serverPrefab);

            await serverSingleton.CreateServer(playerPrefab);

            serverSingleton.GameManager.StartGameServerAsync();

        } else
        {
            //Host and Client Code

            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost(playerPrefab);

            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            clientSingleton.CreateClient();


            //Go to main menu
            Loader.Load(Loader.Scene.AuthBootstrap);

        }
    }

}
