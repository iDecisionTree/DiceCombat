using Godot;
using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

public abstract partial class CombatEffectDirector : Node, ICombatEffectDirector
{
	public virtual void ResetEffects()
	{
	}

	public virtual float GetResolutionDuration()
	{
		return 0f;
	}

	public virtual void OnAttackStarted(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public virtual void OnAttackImpact(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public virtual void OnDefenseStarted(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public virtual void OnDefenseImpact(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public virtual void PlayAttackEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public virtual void PlayDefenseEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public virtual void PlayDamageEffect(Card card, int damage)
	{
	}
}

