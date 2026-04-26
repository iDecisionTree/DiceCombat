using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DiceCombat.scripts.card_skill;
using DiceCombat.scripts.dice;
using DiceCombat.scripts.rich_text_3d;

namespace DiceCombat.scripts.card;

[GlobalClass]
[Tool]
public partial class Card : Node3D
{
	[Export] public CardData CardData { get; set; }

	[Export] public int CurrentHealth { get; set; }
	[Export] public MeshInstance3D CardMesh { get; set; }

	[Export] public Node RichTextName { get; set; }
	[Export] public Node RichTextDescription { get; set; }
	[Export] public Node RichTextHealth { get; set; }
	[Export] public Node RichTextAttack { get; set; }
	[Export] public Node RichTextDefense { get; set; }
	[Export] public Node RichTextDice4 { get; set; }
	[Export] public Node RichTextDice6 { get; set; }
	[Export] public Node RichTextDice8 { get; set; }
	[Export] public Node RichTextDice12 { get; set; }

	private RichText3DWrapper _richTextName;
	private RichText3DWrapper _richTextDescription;
	private RichText3DWrapper _richTextHealth;
	private RichText3DWrapper _richTextAttack;
	private RichText3DWrapper _richTextDefense;
	private RichText3DWrapper _richTextDice4;
	private RichText3DWrapper _richTextDice6;
	private RichText3DWrapper _richTextDice8;
	private RichText3DWrapper _richTextDice12;

	private ShaderMaterial _material;
	private readonly CardSkillRuntimeState _skillRuntime = new();

	public CardSkillRuntimeState SkillRuntime => _skillRuntime;

	public override void _Ready()
	{
		CacheRichTextWrappers();
		ApplyCardMaterial();
		InitializeHealth();
		RefreshView();
	}

	public void RefreshView()
	{
		RefreshCoreText();
		RefreshDiceText();
	}

	public void UpdateText()
	{
		RefreshView();
	}

	public IReadOnlyList<CardSkill> GetSkills()
	{
		return CardData?.Skills?.Where(skill => skill != null).ToArray() ?? Array.Empty<CardSkill>();
	}

	public void ResetSkillRuntime()
	{
		_skillRuntime.Reset();
	}

	public int GetSelectionDamagePreviewBonus(DiceSelectionPreviewContext context)
	{
		int totalBonus = 0;
		foreach (CardSkill skill in GetSkills())
		{
			totalBonus += skill.GetSelectionDamagePreviewBonus(context);
		}

		return totalBonus;
	}

	public List<DiceData> RollAllDice()
	{
		List<DiceData> result = new List<DiceData>();
		if (CardData?.DiceGroups == null)
		{
			return result;
		}

		foreach (DiceSet group in CardData.DiceGroups)
		{
			if (group == null)
			{
				continue;
			}

			for (int i = 0; i < group.Count; i++)
			{
				result.Add(new DiceData(group.DiceType, 0));
			}
		}

		return result;
	}

	public int RollSingleDice(DiceType diceType)
	{
		int sides = diceType switch
		{
			DiceType.Dice4 => 4,
			DiceType.Dice6 => 6,
			DiceType.Dice8 => 8,
			DiceType.Dice12 => 12,
			_ => 4
		};

		return GD.RandRange(1, sides);
	}

	public void TakeDamage(int damage)
	{
		damage = Mathf.Max(damage, 0);
		CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
		RefreshView();
	}

	public void Heal(int amount)
	{
		amount = Mathf.Max(amount, 0);
		int maxHealth = Mathf.Max(CardData?.MaxHealth ?? CurrentHealth, CurrentHealth);
		CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
		RefreshView();
	}

	public void ApplyAfterDiceSelected(DiceSelectionSkillContext context)
	{
		foreach (CardSkill skill in GetSkills())
		{
			skill.OnAfterDiceSelected(context);
		}
	}

	public void ApplyBeforeDamageResolved(DamageResolutionSkillContext context)
	{
		foreach (CardSkill skill in GetSkills())
		{
			skill.OnBeforeDamageResolved(context);
		}
	}

	public void ApplyAfterDamageResolved(DamageResolutionSkillContext context)
	{
		foreach (CardSkill skill in GetSkills())
		{
			skill.OnAfterDamageResolved(context);
		}
	}


	public bool IsDead()
	{
		return CurrentHealth <= 0;
	}

	private void CacheRichTextWrappers()
	{
		_richTextName = new RichText3DWrapper(RichTextName);
		_richTextDescription = new RichText3DWrapper(RichTextDescription);
		_richTextHealth = new RichText3DWrapper(RichTextHealth);
		_richTextAttack = new RichText3DWrapper(RichTextAttack);
		_richTextDefense = new RichText3DWrapper(RichTextDefense);
		_richTextDice4 = new RichText3DWrapper(RichTextDice4);
		_richTextDice6 = new RichText3DWrapper(RichTextDice6);
		_richTextDice8 = new RichText3DWrapper(RichTextDice8);
		_richTextDice12 = new RichText3DWrapper(RichTextDice12);
	}

	private void ApplyCardMaterial()
	{
		if (CardMesh == null || CardMesh.MaterialOverride == null)
		{
			return;
		}

		_material = CardMesh.MaterialOverride.Duplicate() as ShaderMaterial;
		if (_material == null || CardData == null)
		{
			CardMesh.MaterialOverride = _material;
			return;
		}

		_material.SetShaderParameter("card_avatar", CardData.CardAvatar);
		_material.SetShaderParameter("card_background", CardData.CardBackground);
		CardMesh.MaterialOverride = _material;
	}

	private void InitializeHealth()
	{
		CurrentHealth = CardData != null ? CardData.MaxHealth : 0;
	}

	private void RefreshCoreText()
	{
		CardData cardData = CardData;
		int attack = cardData?.Attack ?? 0;
		int defense = cardData?.Defense ?? 0;

		_richTextName.Text = cardData?.CardName ?? Name;
		_richTextDescription.Text = cardData?.Description ?? string.Empty;
		_richTextHealth.Text = CurrentHealth.ToString();
		_richTextAttack.Text = attack.ToString();
		_richTextDefense.Text = defense.ToString();
	}

	private void RefreshDiceText()
	{
		_richTextDice4.Text = GetDiceCount(DiceType.Dice4).ToString();
		_richTextDice6.Text = GetDiceCount(DiceType.Dice6).ToString();
		_richTextDice8.Text = GetDiceCount(DiceType.Dice8).ToString();
		_richTextDice12.Text = GetDiceCount(DiceType.Dice12).ToString();
	}

	private int GetDiceCount(DiceType diceType)
	{
		return CardData?.DiceGroups?.FirstOrDefault(x => x != null && x.DiceType == diceType)?.Count ?? 0;
	}
}
