using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

public sealed class NullCombatCameraDirector : ICombatCameraDirector
{
	public static readonly NullCombatCameraDirector Instance = new NullCombatCameraDirector();

	private NullCombatCameraDirector()
	{
	}

	public void ResetCamera()
	{
	}

	public void OnBattleStarted()
	{
	}

	public void OnTurnChanged(CombatTurn turn, int roundCount)
	{
	}

	public void OnDamageResolved(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void OnBattleEnded(CombatState finalState)
	{
	}
}

