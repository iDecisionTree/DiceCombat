using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

public interface ICombatEffectDirector
{
	void ResetEffects();
	float GetResolutionDuration();
	void OnAttackStarted(CombatTurn turn, Card sourceCard, Card targetCard, int damage);
	void OnAttackImpact(CombatTurn turn, Card sourceCard, Card targetCard, int damage);
	void OnDefenseStarted(CombatTurn turn, Card sourceCard, Card targetCard, int damage);
	void OnDefenseImpact(CombatTurn turn, Card sourceCard, Card targetCard, int damage);
	void PlayAttackEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage);
	void PlayDefenseEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage);
	void PlayDamageEffect(Card card, int damage);
}

