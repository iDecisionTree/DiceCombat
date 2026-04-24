using Godot;

namespace DiceCombat.scripts.card_skill;

[Tool]
public abstract partial class CardSkill : Resource
{
	public virtual int GetSelectionDamagePreviewBonus(DiceSelectionPreviewContext context)
	{
		return 0;
	}

	public virtual void OnAfterDiceSelected(DiceSelectionSkillContext context)
	{
	}

	public virtual void OnBeforeDamageResolved(DamageResolutionSkillContext context)
	{
	}

	public virtual void OnAfterDamageResolved(DamageResolutionSkillContext context)
	{
	}
}

