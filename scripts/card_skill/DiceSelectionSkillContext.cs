using System;
using System.Collections.Generic;
using DiceCombat.scripts.card;
using DiceCombat.scripts.dice;
using DiceCombat.scripts.state_machine;

namespace DiceCombat.scripts.card_skill;

public sealed class DiceSelectionSkillContext
{
	public DiceSelectionSkillContext(
		CombatTurn turn,
		Card ownerCard,
		Card otherCard,
		CardSelectionRole ownerRole,
		IEnumerable<DiceData> selectedDice,
		CardSkillRuntimeState ownerRuntimeState,
		CardSkillRuntimeState otherRuntimeState)
	{
		Turn = turn;
		OwnerCard = ownerCard;
		OtherCard = otherCard;
		OwnerRole = ownerRole;
		SelectedDice = selectedDice != null ? Array.AsReadOnly(new List<DiceData>(selectedDice).ToArray()) : Array.Empty<DiceData>();
		OwnerRuntimeState = ownerRuntimeState ?? new CardSkillRuntimeState();
		OtherRuntimeState = otherRuntimeState ?? new CardSkillRuntimeState();
	}

	public CombatTurn Turn { get; }
	public Card OwnerCard { get; }
	public Card OtherCard { get; }
	public CardSelectionRole OwnerRole { get; }
	public IReadOnlyList<DiceData> SelectedDice { get; }
	public CardSkillRuntimeState OwnerRuntimeState { get; }
	public CardSkillRuntimeState OtherRuntimeState { get; }

	public bool IsAttackSelection => OwnerRole == CardSelectionRole.Attack;
	public bool IsDefenseSelection => OwnerRole == CardSelectionRole.Defense;
	public int SelectedCount => SelectedDice.Count;
	public int TotalPoints
	{
		get
		{
			int total = 0;
			for (int i = 0; i < SelectedDice.Count; i++)
			{
				total += SelectedDice[i]?.Num ?? 0;
			}

			return total;
		}
	}

	public void AddPendingDamageBonus(int amount)
	{
		OwnerRuntimeState.AddPendingDamageBonus(amount);
	}

	public void AddPendingDamageReduction(int amount)
	{
		OwnerRuntimeState.AddPendingDamageReduction(amount);
	}
}

