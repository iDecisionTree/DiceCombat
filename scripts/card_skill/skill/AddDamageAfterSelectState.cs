using Godot;
using DiceCombat.scripts.card_skill;

namespace DiceCombat.scripts.card_skill.skill;

[GlobalClass]
public partial class AddDamageAfterSelectState : CardSkill
{
	[Export] public int DamageAddition { get; set; } = 5;

	public override int GetSelectionDamagePreviewBonus(DiceSelectionPreviewContext context)
	{
		return ShouldApplyToAttack(context.IsAttackSelection) ? DamageAddition : 0;
	}

	public override void OnBeforeDamageResolved(DamageResolutionSkillContext context)
	{
		if (!ShouldApplyToAttack(context.IsOwnerAttacker))
		{
			return;
		}

		context.AddDamageBonus(DamageAddition);
	}

	private bool ShouldApplyToAttack(bool isAttackRole)
	{
		if (!isAttackRole)
		{
			return false;
		}

		if (DamageAddition <= 0)
		{
			return false;
		}

		return true;
	}
}
