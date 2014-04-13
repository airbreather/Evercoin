namespace Evercoin
{
    public interface IChainValidator
    {
        ValidationResult ValidateBlock(IBlock block);

        ValidationResult ValidateTransaction(ITransaction transaction);
    }
}
