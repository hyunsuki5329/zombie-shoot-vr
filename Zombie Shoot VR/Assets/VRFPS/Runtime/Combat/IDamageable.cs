namespace VRFPS
{
    public interface IDamageable
    {
        bool ApplyDamage(int amount, int sourceId, double activeTime);
    }
}
