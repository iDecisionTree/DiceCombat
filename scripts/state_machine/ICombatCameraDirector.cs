using DiceCombat.scripts.card;
namespace DiceCombat.scripts.state_machine;

public interface ICombatCameraDirector
{
	void ResetCamera();
	void OnBattleStarted();
	void OnTurnChanged(CombatTurn turn, int roundCount);
	void OnDamageResolved(CombatTurn turn, Card sourceCard, Card targetCard, int damage);
	void OnBattleEnded(CombatState finalState);
}

