using DiceCombat.scripts.card;
using Godot;

namespace DiceCombat.scripts.state_machine;

public abstract partial class CombatCameraDirector : Node, ICombatCameraDirector
{
	public virtual void ResetCamera()
	{
	}

	public virtual void OnBattleStarted()
	{
	}

	public virtual void OnTurnChanged(CombatTurn turn, int roundCount)
	{
	}

	public virtual void OnDamageResolved(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public virtual void OnBattleEnded(CombatState finalState)
	{
	}
}

