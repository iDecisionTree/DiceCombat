using System;
using System.Collections.Generic;
using DiceCombat.scripts.card;
using DiceCombat.scripts.dice;
using DiceCombat.scripts.state_machine;

namespace DiceCombat.scripts.card_skill;

public sealed class DamageResolutionSharedState
{
	public int Damage { get; set; }
	public int AppliedDamage { get; set; }
}

public sealed class DamageResolutionSkillContext
{
	private readonly DamageResolutionSharedState _sharedState;

	public DamageResolutionSkillContext(
		CombatTurn turn,
		Card ownerCard,
		Card otherCard,
		Card sourceCard,
		Card targetCard,
		IEnumerable<DiceData> sourceDice,
		IEnumerable<DiceData> targetDice,
		int attackValue,
		int defenseValue,
		int initialDamage,
		CardSkillRuntimeState ownerRuntimeState,
		CardSkillRuntimeState otherRuntimeState,
		DamageResolutionSharedState sharedState = null)
	{
		Turn = turn;
		OwnerCard = ownerCard;
		OtherCard = otherCard;
		SourceCard = sourceCard;
		TargetCard = targetCard;
		SourceDice = sourceDice != null ? Array.AsReadOnly(new List<DiceData>(sourceDice).ToArray()) : Array.Empty<DiceData>();
		TargetDice = targetDice != null ? Array.AsReadOnly(new List<DiceData>(targetDice).ToArray()) : Array.Empty<DiceData>();
		AttackValue = attackValue;
		DefenseValue = defenseValue;
		BaseDamage = Math.Max(initialDamage, 0);
		OwnerRuntimeState = ownerRuntimeState ?? new CardSkillRuntimeState();
		OtherRuntimeState = otherRuntimeState ?? new CardSkillRuntimeState();
		_sharedState = sharedState ?? new DamageResolutionSharedState { Damage = BaseDamage, AppliedDamage = 0 };
	}

	public CombatTurn Turn { get; }
	public Card OwnerCard { get; }
	public Card OtherCard { get; }
	public Card SourceCard { get; }
	public Card TargetCard { get; }
	public IReadOnlyList<DiceData> SourceDice { get; }
	public IReadOnlyList<DiceData> TargetDice { get; }
	public int AttackValue { get; }
	public int DefenseValue { get; }
	public int BaseDamage { get; }
	public CardSkillRuntimeState OwnerRuntimeState { get; }
	public CardSkillRuntimeState OtherRuntimeState { get; }

	public int Damage => _sharedState.Damage;
	public int AppliedDamage => _sharedState.AppliedDamage;
	public bool IsOwnerAttacker => OwnerCard == SourceCard;
	public bool IsOwnerDefender => OwnerCard == TargetCard;

	public DamageResolutionSkillContext CreateForOwner(Card ownerCard, Card otherCard, CardSkillRuntimeState ownerRuntimeState, CardSkillRuntimeState otherRuntimeState)
	{
		return new DamageResolutionSkillContext(
			Turn,
			ownerCard,
			otherCard,
			SourceCard,
			TargetCard,
			SourceDice,
			TargetDice,
			AttackValue,
			DefenseValue,
			BaseDamage,
			ownerRuntimeState,
			otherRuntimeState,
			_sharedState);
	}

	public void AddDamageBonus(int amount)
	{
		SetDamage(_sharedState.Damage + amount);
	}

	public void ReduceDamage(int amount)
	{
		SetDamage(_sharedState.Damage - amount);
	}

	public void SetDamage(int value)
	{
		_sharedState.Damage = Math.Max(value, 0);
	}

	public void SetAppliedDamage(int value)
	{
		_sharedState.AppliedDamage = Math.Max(value, 0);
	}
}


