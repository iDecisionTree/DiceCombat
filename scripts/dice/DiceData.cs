namespace DiceCombat.scripts.dice;

public class DiceData
{
    public DiceType DiceType { get; set; }
    public int Num { get; set; }

    public DiceData(DiceType diceType, int num)
    {
        DiceType = diceType;
        Num = num;
    }
}