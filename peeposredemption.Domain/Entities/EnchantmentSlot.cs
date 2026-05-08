namespace peeposredemption.Domain.Entities;

/// <summary>
/// A single enchantment on an item. Items hold a JSON-serialized list of these.
/// Tier 1-5, Bonus is the stat bonus granted (+damage, +resistance, etc.).
/// Name is auto-generated e.g. "Blazing II", "Void-Touched V".
/// </summary>
public record EnchantmentSlot(Element Element, int Tier, int Bonus, string Name);
