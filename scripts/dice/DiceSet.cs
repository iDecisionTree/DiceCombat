using Godot;
using DiceCombat.scripts;
using DiceCombat.scripts.dice;

namespace DiceCombat.scripts.dice;

[GlobalClass]
[Tool]
public partial class DiceSet : Resource
{
    [Export] public DiceType DiceType { get; set; } = DiceType.Dice4;
    [Export] public int Count { get; set; } = 1;
}
