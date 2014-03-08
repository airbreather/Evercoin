namespace Evercoin
{
    public interface ISignatureCheckerFactory
    {
        ISignatureChecker CreateSignatureChecker(ITransaction transaction, int outputIndex);
    }
}
