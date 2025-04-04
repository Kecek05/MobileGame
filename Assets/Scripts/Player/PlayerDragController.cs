using Sortify;
using Unity.Netcode;
using UnityEngine;

public class PlayerDragController : DragAndShoot
{

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return;

        player.PlayerStateMachine.OnStateChanged += PlayerStateMachine_OnStateChanged;

    }

    private void PlayerStateMachine_OnStateChanged(IState state)
    {
        if(state == player.PlayerStateMachine.idleMyTurnState)
        {
            TurnOnDrag();
        } 
        else if (state == player.PlayerStateMachine.dragReleaseJump || state == player.PlayerStateMachine.dragReleaseItem || state == player.PlayerStateMachine.idleEnemyTurnState)
        {
            TurnOffDrag();
            ResetDrag();
        } else if (state == player.PlayerStateMachine.playerGameOverState)
        {
            TurnOffDrag();
        }
    }


    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        player.PlayerStateMachine.OnStateChanged -= PlayerStateMachine_OnStateChanged;
    }
}
