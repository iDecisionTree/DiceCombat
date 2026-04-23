using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

public sealed class NullCombatEffectDirector : ICombatEffectDirector
{
	public static readonly NullCombatEffectDirector Instance = new NullCombatEffectDirector();

	private NullCombatEffectDirector()
	{
	}

	public void ResetEffects()
	{
	}

	public float GetResolutionDuration()
	{
		return 0f;
	}

	public void OnAttackStarted(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void OnAttackImpact(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void OnDefenseStarted(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void OnDefenseImpact(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void PlayAttackEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void PlayDefenseEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void PlayDamageEffect(Card card, int damage)
	{
	}
}

