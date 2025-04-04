using UnityEngine;


public class ServiceLocatorBootstrap : MonoBehaviour
{

    private void Awake()
    {
        //Register clients
        BaseTurnManager turnManager = gameObject.AddComponent<TurnManager>();
        BaseTimerManager timerManager = gameObject.AddComponent<TimerManager>();
        BaseGameTimerManager gameTimerManager = gameObject.AddComponent<GameTimerManager>();
        BasePlayersPublicInfoManager playersPublicInfoManager = gameObject.AddComponent<PlayersPublicInfoManager>();
        BaseItemActivableManager itemActivableManager = gameObject.AddComponent<ItemActivableManager>();
        BaseGameStateManager gameStateManager = gameObject.AddComponent<GameStateManager>();
        BaseGameOverManager gameOverManager = gameObject.AddComponent<GameOverManager>();
        BasePearlsManager pearlsManager = gameObject.AddComponent<PearlsManager>();

        ServiceLocator.Register(turnManager);
        ServiceLocator.Register(timerManager);
        ServiceLocator.Register(gameTimerManager);
        ServiceLocator.Register(playersPublicInfoManager);
        ServiceLocator.Register(itemActivableManager);
        ServiceLocator.Register(gameStateManager);
        ServiceLocator.Register(gameOverManager);
        ServiceLocator.Register(pearlsManager);
    }
}
