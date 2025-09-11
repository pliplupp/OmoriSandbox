using System;

public struct Stats
{
    public int HP;
    public int MaxHP;
    public int Juice;
    public int MaxJuice;
    public int ATK;
    public int DEF;
    public int SPD;
    public int LCK;
    public int HIT;

    public Stats(int hp = 0, int juice = 0, int atk = 0, int def = 0, int spd = 0, int lck = 0, int hit = 0)
    {
        HP = hp;
        MaxHP = hp;
        Juice = juice;
        MaxJuice = juice;
        ATK = atk;
        DEF = def;
        SPD = spd;
        LCK = lck;
        HIT = hit;
    }

    public static Stats operator +(Stats a, Stats b) {
        Stats result = new(a.HP + b.HP, a.Juice + b.Juice, a.ATK + b.ATK, a.DEF + b.DEF, a.SPD + b.SPD, a.LCK + b.LCK, a.HIT + b.HIT);
        result.MaxHP = a.HP + b.HP;
        result.MaxJuice = a.Juice + b.Juice;
        return result;
    }

    public int GetStat(StatType stat)
    {
        return stat switch
        {
            StatType.HP => HP,
            StatType.MaxHP => MaxHP,
            StatType.Juice => Juice,
            StatType.MaxJuice => MaxJuice,
            StatType.ATK => ATK,
            StatType.DEF => DEF,
            StatType.SPD => SPD,
            StatType.LCK => LCK,
            StatType.HIT => HIT,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
    }

    public void SetStat(StatType stat, int value)
    {
        switch (stat)
        {
            case StatType.HP: HP = value; break;
            case StatType.MaxHP: MaxHP = value; break;
            case StatType.Juice: Juice = value; break;
            case StatType.MaxJuice: MaxJuice = value; break;
            case StatType.ATK: ATK = value; break;
            case StatType.DEF: DEF = value; break;
            case StatType.SPD: SPD = value; break;
            case StatType.LCK: LCK = value; break;
            case StatType.HIT: HIT = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }
    }
}