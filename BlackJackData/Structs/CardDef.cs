using System;
using BlackJackData.Enums;

namespace BlackJackData.Structs;

public struct CardDef : IEquatable<CardDef>
{
    public CardSuit Suit;
    public CardRank Rank;

    public CardDef(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public readonly byte ToPackedByte()
    {
        return (byte)(((int)Suit << 4) | ((int)Rank - 2));
    }

    public static CardDef FromPackedByte(byte packed)
    {
        if (packed > 51)
        {
            throw new ArgumentOutOfRangeException(nameof(packed), packed, "Packed card value must be in the range 0..51.");
        }

        var suit = (CardSuit)(packed >> 4);
        var rank = (CardRank)((packed & 0x0F) + 2);

        return new CardDef(suit, rank);
    }

    public static implicit operator byte(CardDef card)
    {
        return card.ToPackedByte();
    }

    public static implicit operator CardDef(byte packed)
    {
        return FromPackedByte(packed);
    }

    public static bool operator ==(CardDef left, CardDef right)
    {
        return left.Suit == right.Suit && left.Rank == right.Rank;
    }

    public static bool operator !=(CardDef left, CardDef right)
    {
        return !(left == right);
    }

    public static bool operator ==(CardDef left, byte right)
    {
        return left.ToPackedByte() == right;
    }

    public static bool operator !=(CardDef left, byte right)
    {
        return !(left == right);
    }

    public static bool operator ==(byte left, CardDef right)
    {
        return left == right.ToPackedByte();
    }

    public static bool operator !=(byte left, CardDef right)
    {
        return !(left == right);
    }

    public bool Equals(CardDef other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is CardDef other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Suit, Rank);
    }
}
