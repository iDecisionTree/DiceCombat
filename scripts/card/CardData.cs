using Godot;
using DiceCombat.scripts.dice;

namespace DiceCombat.scripts.card;

[GlobalClass]
[Tool]
public partial class CardData : Resource
{
	// 基础信息
	[Export] public string CardId { get; set; } = "0000";
	[Export] public string CardName { get; set; } = "卡牌名称";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "卡牌描述";
	[Export] public Texture2D CardBackground { get; set; }
	[Export] public Texture2D CardAvatar { get; set; }
	[Export] public Texture2D InfoAvatar { get; set; }
	
	// 属性
	[Export] public int Attack { get; set; }
	[Export] public int Defense { get; set; }
	[Export] public int MaxHealth { get; set; }
	
	// 骰子池
	[Export] public DiceSet[] DiceGroups { get; set; }
}
